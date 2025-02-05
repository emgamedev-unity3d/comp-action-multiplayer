using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    //                    ^ a specific group for predicted entities
    public partial struct HoveringPlatformSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<HoveringWallComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;

            foreach (var (transform, hoveringWallComponent)
                in SystemAPI.Query<
                    RefRW<LocalTransform>, HoveringWallComponent>()
                        .WithAll<Simulate>())// <-- built in component all entities have (online or not),
                                             //     used for updating on predicted entities
            {
                // move platform up and down (testing with a simple sine function)
                var originalPosition = hoveringWallComponent.originalPosition;
                originalPosition.y += (2f * (float)math.sin(elapsedTime * 0.5f));

                transform.ValueRW.Position = originalPosition;
            }
        }
    }
}
