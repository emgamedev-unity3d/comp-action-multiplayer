using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [BurstCompile]
    [UpdateInGroup(typeof(ProjectilePredictionUpdateGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public partial struct RocketSimulationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RocketSimulationJob simulationJob = new RocketSimulationJob
            {
                IsServer = state.WorldUnmanaged.IsServer(),
                HealthLookup = SystemAPI.GetComponentLookup<Health>(),
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
                DelayedDespawnLookup = SystemAPI.GetComponentLookup<DelayedDespawn>(),
            };
            state.Dependency = simulationJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        [WithDisabled(typeof(DelayedDespawn))]
        public partial struct RocketSimulationJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public bool IsServer;
            public ComponentLookup<Health> HealthLookup;

            [ReadOnly]
            public CollisionWorld CollisionWorld;

            public ComponentLookup<DelayedDespawn> DelayedDespawnLookup;

            [NativeDisableContainerSafetyRestriction]
            NativeList<DistanceHit> m_Hits;

            void Execute(Entity entity, ref RocketShotVisual rocketShotVisual, ref LocalTransform localTransform,
                in PrefabProjectile projectile)
            {
                if (IsServer)
                {
                    // Hit processing
                    if (rocketShotVisual.HasProcessedHitSimulation == 0 && projectile.HasHit == 1)
                    {
                        // Direct hit damage
                        if (HealthLookup.TryGetComponent(projectile.HitEntity, out Health health))
                        {
                            health.CurrentHealth -= rocketShotVisual.DirectHitDamage;
                            HealthLookup[projectile.HitEntity] = health;
                        }

                        // Area damage
                        m_Hits.Clear();
                        if (CollisionWorld.OverlapSphere(localTransform.Position, rocketShotVisual.DamageRadius, ref m_Hits,
                                CollisionFilter.Default))
                        {
                            for (var i = 0; i < m_Hits.Length; i++)
                            {
                                var hit = m_Hits[i];
                                if (HealthLookup.TryGetComponent(hit.Entity, out Health health2))
                                {
                                    var damageWithFalloff = rocketShotVisual.MaxRadiusDamage *
                                                            (1f - math.saturate(hit.Distance / rocketShotVisual.DamageRadius));
                                    health2.CurrentHealth -= damageWithFalloff;
                                    HealthLookup[hit.Entity] = health2;
                                }
                            }
                        }

                        // Activate delayed despawn
                        if (projectile.HitEntity != Entity.Null)
                            DelayedDespawnLookup.SetComponentEnabled(entity, true);

                        rocketShotVisual.HasProcessedHitSimulation = 1;
                    }
                }
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                if (!m_Hits.IsCreated)
                {
                    m_Hits = new NativeList<DistanceHit>(64, Allocator.Temp);
                }

                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(ProjectileVisualsUpdateGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct RocketVfxSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, RocketShotVisual, VfxAttributeSettings, PrefabProjectile>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RocketVfxJob job = new RocketVfxJob
            {
                Ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW
                    .CreateCommandBuffer(state.WorldUnmanaged),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct RocketVfxJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;

            void Execute(in LocalTransform transform, ref RocketShotVisual rocketShotVisual,
                ref VfxAttributeSettings vfxAttributeSettings, in PrefabProjectile projectile)
            {
                if (rocketShotVisual.HasProcessedHitVfx == 0 && projectile.HasHit == 1)
                {
                    Entity spawnVfxHitRocketRequestEntity = Ecb.CreateEntity();
                    Ecb.AddComponent(spawnVfxHitRocketRequestEntity,
                        new VfxHitRequest()
                        {
                            VfxHitType = VfxType.Rocket,
                            LowCount = vfxAttributeSettings.LowVfxSpawnCount,
                            MidCount = vfxAttributeSettings.MidVfxSpawnCount,
                            HighCount = vfxAttributeSettings.HighVfxSpawnCount,
                            Position = transform.Position,
                            HitNormal = projectile.HitNormal,
                            HitRadius = rocketShotVisual.DamageRadius,
                        });

                    rocketShotVisual.HasProcessedHitVfx = 1;
                }
            }
        }
    }
}
