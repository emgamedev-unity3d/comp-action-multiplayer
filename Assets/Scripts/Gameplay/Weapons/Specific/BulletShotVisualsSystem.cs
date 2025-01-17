using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(ProjectileVisualsUpdateGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct BulletShotVisualsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<BulletShotVisuals, VfxAttributeSettings, RaycastVisualProjectile>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            BulletShotVisualsJob job = new BulletShotVisualsJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW
                    .CreateCommandBuffer(state.WorldUnmanaged),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct BulletShotVisualsJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer Ecb;

            void Execute(Entity entity, ref BulletShotVisuals shotVisuals, ref LocalTransform localTransform,
                ref PostTransformMatrix postTransformMatrix, ref VfxAttributeSettings attributeSettings, in RaycastVisualProjectile raycastVisualProjectile)
            {
                if (!shotVisuals.IsInitialized)
                {
                    // Hit Vfx
                    if (raycastVisualProjectile.DidHit == 1)
                    {
                        Entity spawnVfxHitBulletRequestEntity = Ecb.CreateEntity();
                        Ecb.AddComponent(spawnVfxHitBulletRequestEntity,
                            new VfxHitRequest()
                         {
                             VfxHitType = attributeSettings.BulletType == BulletType.Shotgun? VfxType.ShotgunBullet : VfxType.MachineGunBullet,
                             LowCount = attributeSettings.LowVfxSpawnCount,
                             MidCount = attributeSettings.MidVfxSpawnCount,
                             HighCount = attributeSettings.HighVfxSpawnCount,
                             Position = raycastVisualProjectile.EndPoint,
                             HitNormal = raycastVisualProjectile.HitNormal,
                         });
                    }
                    shotVisuals.IsInitialized = true;
                }

                // Speed
                float3 movedDistance =
                    math.mul(localTransform.Rotation, math.forward()) * shotVisuals.Speed * DeltaTime;
                localTransform.Position += movedDistance;
                shotVisuals.DistanceTraveled += math.length(movedDistance);

                // Stretch
                var zScale = math.clamp(shotVisuals.Speed * shotVisuals.StretchFromSpeed, 0f,
                    math.min(shotVisuals.DistanceTraveled, shotVisuals.MaxStretch));

                // On reached hit
                if (shotVisuals.DistanceTraveled >= raycastVisualProjectile.GetLengthOfTrajectory())
                {
                    // Clamp position to max distance.
                    var preClampDistFromOrigin = math.length(localTransform.Position - raycastVisualProjectile.StartPoint);
                    localTransform.Position = raycastVisualProjectile.EndPoint;

                    // Adjust scale stretch for clamped position.
                    zScale *= math.length(localTransform.Position - raycastVisualProjectile.StartPoint) /
                              preClampDistFromOrigin;

                    Ecb.DestroyEntity(entity);
                }

                postTransformMatrix.Value = float4x4.Scale(1f, 1f, zScale);
            }
        }
    }
}
