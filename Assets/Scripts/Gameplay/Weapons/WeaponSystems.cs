using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public partial class WeaponPredictionUpdateGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class WeaponVisualsUpdateGroup : ComponentSystemGroup
    {
    }

    [BurstCompile]
    [UpdateInGroup(typeof(WeaponPredictionUpdateGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public partial struct WeaponsSimulationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<PhysicsWorldHistorySingleton>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            GameResources gameResources = SystemAPI.GetSingleton<GameResources>();
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

            BaseWeaponSimulationJob baseWeaponSimulationJob = new BaseWeaponSimulationJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
            };
            state.Dependency = baseWeaponSimulationJob.ScheduleParallel(state.Dependency);

            if (networkTime.IsFirstTimeFullyPredictingTick)
            {
                RaycastWeaponSimulationJob raycastWeaponSimulationJob = new RaycastWeaponSimulationJob
                {
                    IsServer = state.WorldUnmanaged.IsServer(),
                    OldestAllowedVfxRequestsTick = math.max(0,
                        networkTime.ServerTick.TickIndexForValidTick - gameResources.PolledEventsTicks),
                    NetworkTime = networkTime,
                    PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                    PhysicsWorldHistory = SystemAPI.GetSingleton<PhysicsWorldHistorySingleton>(),
                    RaycastProjectileLookup = SystemAPI.GetComponentLookup<RaycastProjectile>(true),
                    HealthLookup = SystemAPI.GetComponentLookup<Health>(),
                };
                state.Dependency = raycastWeaponSimulationJob.Schedule(state.Dependency);

                PrefabWeaponSimulationJob prefabWeaponSimulationJob = new PrefabWeaponSimulationJob
                {
                    IsServer = state.WorldUnmanaged.IsServer(),
                    Ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                        .CreateCommandBuffer(state.WorldUnmanaged),
                    PrefabProjectileLookup = SystemAPI.GetComponentLookup<PrefabProjectile>(true),
                };
                state.Dependency = prefabWeaponSimulationJob.Schedule(state.Dependency);
            }
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct BaseWeaponSimulationJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;

            void Execute(
                ref BaseWeapon baseWeapon,
                ref WeaponControl weaponControl,
                ref DynamicBuffer<WeaponProjectileEvent> projectileEvents,
                in WeaponShotSimulationOriginOverride shotSimulationOriginOverride)
            {
                projectileEvents.Clear();

                var prevTotalShotsCount = baseWeapon.TotalShotsCount;

                // Detect starting to fire.
                if (weaponControl.ShootPressed)
                {
                    baseWeapon.IsFiring = true;
                }
                // Handle firing.
                if (baseWeapon.FiringRate > 0f)
                {
                    var delayBetweenShots = 1f / baseWeapon.FiringRate;

                    // Clamp shot timer in order to shoot at most the maximum amount of shots that can be shot in one
                    // frame based on the firing rate.
                    // This also prevents needlessly dirtying the timer ghostfield (saves bandwidth).
                    var maxUsefulShotTimer = delayBetweenShots + DeltaTime;
                    if (baseWeapon.ShotTimer < maxUsefulShotTimer)
                    {
                        baseWeapon.ShotTimer += DeltaTime;
                    }
                    // This loop is done to allow firing rates that would trigger more than one shot per tick.
                    while (baseWeapon.IsFiring && baseWeapon.ShotTimer > delayBetweenShots)
                    {
                        baseWeapon.TotalShotsCount++;

                        // Consume shoot time.
                        baseWeapon.ShotTimer -= delayBetweenShots;

                        // Stop firing after initial shot for non-auto fire.
                        if (!baseWeapon.Automatic)
                        {
                            baseWeapon.IsFiring = false;
                        }
                    }
                }

                // Detect stopping fire.
                if (!baseWeapon.Automatic || weaponControl.ShootReleased)
                {
                    baseWeapon.IsFiring = false;
                }

                var shotsToFire = baseWeapon.TotalShotsCount - prevTotalShotsCount;
                if (shotsToFire > 0)
                {
                    // Find the world transform of the shot start point.
                    RigidTransform shotSimulationOrigin = WeaponUtilities.GetShotSimulationOrigin(
                        baseWeapon.ShotOrigin,
                        in shotSimulationOriginOverride,
                        ref LocalTransformLookup,
                        ref ParentLookup,
                        ref PostTransformMatrixLookup);
                    TransformHelpers.ComputeWorldTransformMatrix(baseWeapon.ShotOrigin, out float4x4 shotVisualsOrigin,
                        ref LocalTransformLookup, ref ParentLookup, ref PostTransformMatrixLookup);

                    for (var i = 0; i < shotsToFire; i++)
                    {
                        for (var j = 0; j < baseWeapon.ProjectilesPerShot; j++)
                        {
                            baseWeapon.TotalProjectilesCount++;

                            Random deterministicRandom = Random.CreateFromIndex(baseWeapon.TotalProjectilesCount);
                            quaternion shotRotationWithSpread =
                                WeaponUtilities.CalculateSpreadRotation(shotSimulationOrigin.rot, baseWeapon.SpreadRadians,
                                    ref deterministicRandom);

                            projectileEvents.Add(new WeaponProjectileEvent
                            {
                                Id = baseWeapon.TotalProjectilesCount,
                                SimulationPosition = shotSimulationOrigin.pos,
                                SimulationDirection = math.mul(shotRotationWithSpread, math.forward()),
                                VisualPosition = shotVisualsOrigin.Translation(),
                            });
                        }
                    }
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct RaycastWeaponSimulationJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public bool IsServer;
            public NetworkTime NetworkTime;
            public uint OldestAllowedVfxRequestsTick;
            [ReadOnly] public PhysicsWorld PhysicsWorld;
            [ReadOnly] public PhysicsWorldHistorySingleton PhysicsWorldHistory;
            [ReadOnly] public ComponentLookup<RaycastProjectile> RaycastProjectileLookup;
            public ComponentLookup<Health> HealthLookup;

            [NativeDisableContainerSafetyRestriction]
            NativeList<RaycastHit> m_Hits;

            void Execute(
                in RaycastWeapon raycastWeapon,
                in DynamicBuffer<WeaponProjectileEvent> projectileEvents,
                ref DynamicBuffer<RaycastWeaponVisualProjectileEvent> visualProjectileEvents,
                in CommandDataInterpolationDelay interpolationDelay,
                in DynamicBuffer<WeaponShotIgnoredEntity> ignoredEntities)
            {
                if (RaycastProjectileLookup.TryGetComponent(raycastWeapon.ProjectilePrefab,
                        out RaycastProjectile raycastProjectile))
                {
                    PhysicsWorldHistory.GetCollisionWorldFromTick(NetworkTime.ServerTick, interpolationDelay.Delay,
                        ref PhysicsWorld, out CollisionWorld collisionWorld, out var expectedTick, out var returnedTick);

                    // Validation of Lag Compensation:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if(IsServer && !projectileEvents.IsEmpty)
                    {
                        // Sanity check: This guard ensures `GetCollisionWorldFromTick` is actually compensating for the latency.
                        if (interpolationDelay.Delay <= 0)
                            Debug.LogError($"Lag Compensation failed to read a valid `CommandDataInterpolationDelay` value!");

                        // This debug guard logs a verbose warning, denoting that a player on a ping higher than ~110ms
                        // cannot be FULLY lag compensated, due to the limited size of the history buffer.
                        // I.e. The player will have to lead their target by `expectedTick.TicksSince(returnedTick)` ticks.
                        // TODO - Increase the history size from 16 -> 32 via the `NetCodePhysicsConfig` authoring once it becomes available.
                        // Follow https://discussions.unity.com/t/lag-compensation-and-historic-collision-worlds-size-limitation/914746
                        if (expectedTick != returnedTick)
                            Debug.LogWarning(
                                $"Lag Compensation history buffer too small to resolve interpolationDelay of -{interpolationDelay.Delay} ticks; serverTick:{NetworkTime.ServerTick.ToFixedString()}, lastStored:{PhysicsWorldHistory.LatestStoredTick.ToFixedString()}, wanted:{expectedTick.ToFixedString()}, returned:{returnedTick.ToFixedString()}!");
                    }
#endif

                    // Handle each shot
                    for (var i = 0; i < projectileEvents.Length; i++)
                    {
                        WeaponProjectileEvent projectileEvent = projectileEvents[i];

                        WeaponUtilities.CalculateIndividualRaycastShot(
                            projectileEvent.SimulationPosition,
                            projectileEvent.SimulationDirection,
                            raycastProjectile.Range,
                            in collisionWorld,
                            ref m_Hits,
                            in ignoredEntities,
                            out var hitFound,
                            out float3 hitNormal,
                            out Entity hitEntity,
                            out float3 shotEndPoint);

#if UNITY_EDITOR
                        //Debug.Log($"[ProjectileEvent:{i}] {(IsServer ? "<color=red>SERVER</color>" : "<color=cyan>CLIENT</color>")} on ServerTick:{NetworkTime.ServerTick.ToFixedString()}, interpolationDelay:{interpolationDelay.Delay}, returnedTick:{returnedTick.ToFixedString()} = {(hitFound && HealthLookup.HasComponent(hitEntity) ? $"<color=green>HIT {hitEntity.ToFixedString()} FOR {raycastProjectile.Damage} DAMAGE</color>" : "<color=red>MISSED</color>")}, shotEndPoint:{shotEndPoint}!");
#endif

                        // Visual events.
                        if (raycastWeapon.VisualsSyncMode == RaycastWeaponVisualsSyncMode.Precise)
                        {
                            visualProjectileEvents.Add(new RaycastWeaponVisualProjectileEvent
                            {
                                Tick = NetworkTime.ServerTick.TickIndexForValidTick,
                                DidHit = hitFound ? (byte)1 : (byte)0,
                                EndPoint = shotEndPoint,
                                HitNormal = hitNormal,
                            });

                            // Clear Vfx requests that are too old (if server or owner as owners don't receive syncs from server for this buffer type).
                            for (var e = visualProjectileEvents.Length - 1; e >= 0; e--)
                            {
                                RaycastWeaponVisualProjectileEvent tmpRequest = visualProjectileEvents[e];
                                if (tmpRequest.Tick < OldestAllowedVfxRequestsTick)
                                {
                                    visualProjectileEvents.RemoveAtSwapBack(e);
                                }
                            }
                        }

                        // Apply Damage.
                        if (IsServer && hitFound)
                        {
                            if (HealthLookup.TryGetComponent(hitEntity, out Health health))
                            {
                                health.CurrentHealth -= raycastProjectile.Damage;
                                HealthLookup[hitEntity] = health;
                            }
                        }
                    }
                }
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (!m_Hits.IsCreated)
                {
                    m_Hits = new NativeList<RaycastHit>(128, Allocator.Temp);
                }

                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask,
                bool chunkWasExecuted)
            {
            }
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct PrefabWeaponSimulationJob : IJobEntity
        {
            public bool IsServer;
            public EntityCommandBuffer Ecb;
            [ReadOnly] public ComponentLookup<PrefabProjectile> PrefabProjectileLookup;

            void Execute(
                Entity entity,
                in PrefabWeapon prefabWeapon,
                in DynamicBuffer<WeaponProjectileEvent> projectileEvents,
                in LocalTransform localTransform,
                in DynamicBuffer<WeaponShotIgnoredEntity> ignoredEntities,
                in GhostOwner ghostOwner)
            {
                if (PrefabProjectileLookup.TryGetComponent(prefabWeapon.ProjectilePrefab,
                        out PrefabProjectile prefabProjectile))
                {
                    for (var i = 0; i < projectileEvents.Length; i++)
                    {
                        WeaponProjectileEvent projectileEvent = projectileEvents[i];

                        // Projectile spawn.
                        Entity spawnedProjectile = Ecb.Instantiate(prefabWeapon.ProjectilePrefab);
                        Ecb.SetComponent(spawnedProjectile, LocalTransform.FromPositionRotation(
                            projectileEvent.SimulationPosition,
                            quaternion.LookRotationSafe(projectileEvent.SimulationDirection,
                                math.mul(localTransform.Rotation, math.up()))));
                        Ecb.SetComponent(spawnedProjectile,
                            new ProjectileSpawnId { WeaponEntity = entity, SpawnId = projectileEvent.Id });
                        for (var k = 0; k < ignoredEntities.Length; k++)
                        {
                            Ecb.AppendToBuffer(spawnedProjectile, ignoredEntities[k]);
                        }

                        // Set projectile data.
                        {
                            prefabProjectile.Velocity = prefabProjectile.Speed * projectileEvent.SimulationDirection;
                            prefabProjectile.VisualOffset =
                                projectileEvent.VisualPosition - projectileEvent.SimulationPosition;
                            Ecb.SetComponent(spawnedProjectile, prefabProjectile);
                        }
                        // Set owner.
                        if (IsServer)
                        {
                            Ecb.SetComponent(spawnedProjectile, new GhostOwner { NetworkId = ghostOwner.NetworkId });
                        }
                    }
                }
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(WeaponVisualsUpdateGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct InitializeWeaponLastShotVisualsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var initializeWeaponLastShotVisualsJob = new InitializeWeaponLastShotVisualsJob();
            state.Dependency = initializeWeaponLastShotVisualsJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct InitializeWeaponLastShotVisualsJob : IJobEntity
        {
            void Execute(ref BaseWeapon baseWeapon)
            {
                // This prevents false visual feedbacks when a ghost is re-spawned die to relevancy.
                if (baseWeapon.LastVisualTotalShotsCountInitialized == 0)
                {
                    baseWeapon.LastVisualTotalShotsCount = baseWeapon.TotalShotsCount;
                    baseWeapon.LastVisualTotalShotsCountInitialized = 1;
                }
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(WeaponVisualsUpdateGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct FinalizeWeaponLastShotVisualsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var finalizeWeaponLastShotVisualsJob = new FinalizeWeaponLastShotVisualsJob();
            state.Dependency = finalizeWeaponLastShotVisualsJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct FinalizeWeaponLastShotVisualsJob : IJobEntity
        {
            void Execute(ref BaseWeapon baseWeapon)
            {
                baseWeapon.LastVisualTotalShotsCount = baseWeapon.TotalShotsCount;
            }
        }
    }


    [BurstCompile]
    [UpdateInGroup(typeof(WeaponVisualsUpdateGroup))]
    [UpdateBefore(typeof(CharacterWeaponVisualFeedbackSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct BaseWeaponShotVisualsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            BaseWeaponShotVisualsJob baseWeaponShotVisualsJob = new BaseWeaponShotVisualsJob
            {
                CharacterWeaponVisualFeedbackLookup = SystemAPI.GetComponentLookup<CharacterWeaponVisualFeedback>(),
            };
            state.Dependency = baseWeaponShotVisualsJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct BaseWeaponShotVisualsJob : IJobEntity
        {
            public ComponentLookup<CharacterWeaponVisualFeedback> CharacterWeaponVisualFeedbackLookup;

            void Execute(
                ref WeaponVisualFeedback weaponFeedback,
                ref BaseWeapon baseWeapon,
                ref WeaponOwner weaponOwner)
            {
                // Recoil.
                if (CharacterWeaponVisualFeedbackLookup.TryGetComponent(weaponOwner.Entity,
                        out CharacterWeaponVisualFeedback characterFeedback))
                {
                    for (var i = baseWeapon.LastVisualTotalShotsCount; i < baseWeapon.TotalShotsCount; i++)
                    {
                        characterFeedback.CurrentRecoil += weaponFeedback.RecoilStrength;
                        characterFeedback.TargetRecoilFovKick += weaponFeedback.RecoilFovKick;
                    }

                    CharacterWeaponVisualFeedbackLookup[weaponOwner.Entity] = characterFeedback;
                }
            }
        }
    }
}
