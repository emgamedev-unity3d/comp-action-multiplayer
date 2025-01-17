using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(ProjectileVisualsUpdateGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct LaserShotVisualsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<LaserShotVisuals, LocalTransform, PostTransformMatrix, VfxAttributeSettings, RaycastVisualProjectile>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            LaserShotVisualsJob job = new LaserShotVisualsJob
            {
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                Ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW
                    .CreateCommandBuffer(state.WorldUnmanaged),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct LaserShotVisualsJob : IJobEntity
        {
            public float ElapsedTime;
            public EntityCommandBuffer Ecb;

            void Execute(Entity entity, ref LaserShotVisuals shotVisuals, ref LocalTransform localTransform,
                ref PostTransformMatrix postTransformMatrix, ref VfxAttributeSettings attributeSettings, in RaycastVisualProjectile raycastVisualProjectile)
            {
                if (!shotVisuals.HasInitialized)
                {
                    shotVisuals.StartTime = ElapsedTime;

                    // Scale
                    shotVisuals.StartingScale = new float3(shotVisuals.Width, shotVisuals.Width,
                        raycastVisualProjectile.GetLengthOfTrajectory());

                    // Hit Vfx
                    if (raycastVisualProjectile.DidHit == 1)
                    {
                        Entity spawnVfxHitLaserRequestEntity = Ecb.CreateEntity();
                        Ecb.AddComponent(spawnVfxHitLaserRequestEntity,
                            new VfxHitRequest()
                        {
                            VfxHitType = VfxType.Laser,
                            LowCount = attributeSettings.LowVfxSpawnCount,
                            MidCount = attributeSettings.MidVfxSpawnCount,
                            HighCount = attributeSettings.HighVfxSpawnCount,
                            Position = raycastVisualProjectile.EndPoint,
                            HitNormal = raycastVisualProjectile.HitNormal,
                        });
                    }

                    shotVisuals.HasInitialized = true;
                }

                if (shotVisuals.LifeTime > 0f)
                {
                    var timeRatio = (ElapsedTime - shotVisuals.StartTime) / shotVisuals.LifeTime;
                    var clampedTimeRatio = math.clamp(timeRatio, 0f, 1f);
                    var invTimeRatio = 1f - clampedTimeRatio;

                    if (timeRatio >= 1f)
                        Ecb.DestroyEntity(entity);

                    postTransformMatrix.Value = float4x4.Scale(new float3(shotVisuals.StartingScale.x * invTimeRatio,
                        shotVisuals.StartingScale.y * invTimeRatio, shotVisuals.StartingScale.z));
                }
                else
                {
                    Ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
