using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [BurstCompile]
    [UpdateInGroup(typeof(ProjectilePredictionUpdateGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PlasmaShotVisualSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PlasmaShotVisual, PrefabProjectile>().WithDisabled<DelayedDespawn>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ProjectileBulletSimulationJob simulationJob = new ProjectileBulletSimulationJob
            {
                HealthLookup = SystemAPI.GetComponentLookup<Health>(),
                DelayedDespawnLookup = SystemAPI.GetComponentLookup<DelayedDespawn>(),
            };
            state.Dependency = simulationJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        [WithDisabled(typeof(DelayedDespawn))]
        public partial struct ProjectileBulletSimulationJob : IJobEntity
        {
            public ComponentLookup<Health> HealthLookup;
            public ComponentLookup<DelayedDespawn> DelayedDespawnLookup;

            void Execute(Entity bulletEntity, ref PlasmaShotVisual bullet, in PrefabProjectile projectile)
            {
                // Hit processing
                if (bullet.HasProcessedHitSimulation == 0 && projectile.HasHit == 1)
                {
                    // Direct hit damage
                    if (HealthLookup.TryGetComponent(projectile.HitEntity, out Health health))
                    {
                        health.CurrentHealth -= bullet.Damage;
                        HealthLookup[projectile.HitEntity] = health;
                    }

                    // Activate delayed despawn
                    if (projectile.HitEntity != Entity.Null)
                    {
                        DelayedDespawnLookup.SetComponentEnabled(bulletEntity, true);
                    }

                    bullet.HasProcessedHitSimulation = 1;
                }
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(ProjectileVisualsUpdateGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlasmaShotVisualVfxSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<PlasmaShotVisual, LocalTransform, VfxAttributeSettings, PrefabProjectile>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PlasmaShotVisualVfxJob job = new PlasmaShotVisualVfxJob
            {
                Ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW
                    .CreateCommandBuffer(state.WorldUnmanaged),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct PlasmaShotVisualVfxJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;

            void Execute(in LocalTransform transform, ref PlasmaShotVisual bullet,
                ref VfxAttributeSettings vfxAttributeSettings, in PrefabProjectile projectile)
            {
                if (bullet.HasProcessedHitVfx == 0 && projectile.HasHit == 1)
                {
                    Entity spawnVfxHitProjectileBulletRequestEntity = Ecb.CreateEntity();
                    Ecb.AddComponent(spawnVfxHitProjectileBulletRequestEntity,
                        new VfxHitRequest()
                    {
                        VfxHitType = VfxType.Plasma,
                        LowCount = vfxAttributeSettings.LowVfxSpawnCount,
                        MidCount = vfxAttributeSettings.MidVfxSpawnCount,
                        HighCount = vfxAttributeSettings.HighVfxSpawnCount,
                        Position = transform.Position,
                        HitNormal = projectile.HitNormal,
                    });

                    bullet.HasProcessedHitVfx = 1;
                }
            }
        }
    }
}
