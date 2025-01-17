using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(WeaponVisualsUpdateGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct MachineGunBarrelVisualsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            MachineGunVisualsJob job = new MachineGunVisualsJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct MachineGunVisualsJob : IJobEntity
        {
            public float DeltaTime;
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            void Execute(ref MachineGunBarrelVisual barrelVisual, in BaseWeapon baseWeapon)
            {
                if (LocalTransformLookup.TryGetComponent(barrelVisual.BarrelEntity, out LocalTransform localTransform))
                {
                    if (baseWeapon.LastVisualTotalShotsCount < baseWeapon.TotalShotsCount)
                    {
                        barrelVisual.CurrentSpinVelocity = barrelVisual.SpinVelocity;
                    }
                    else
                    {
                        barrelVisual.CurrentSpinVelocity -= barrelVisual.SpinVelocityDecay * DeltaTime;
                        barrelVisual.CurrentSpinVelocity = math.clamp(barrelVisual.CurrentSpinVelocity, 0f, float.MaxValue);
                    }

                    localTransform.Rotation = math.mul(localTransform.Rotation, quaternion.Euler(0f, 0f, barrelVisual.CurrentSpinVelocity * DeltaTime));
                    LocalTransformLookup[barrelVisual.BarrelEntity] = localTransform;
                }
            }
        }
    }
}
