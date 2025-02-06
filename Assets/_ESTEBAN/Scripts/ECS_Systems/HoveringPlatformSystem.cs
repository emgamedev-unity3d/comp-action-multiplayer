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
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<HoveringEntityComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRateConfig))
                tickRateConfig.ResolveDefaults();

            var elapsedTime =
                SystemAPI.GetSingleton<NetworkTime>().ServerTick.TickIndexForValidTick / (float)tickRateConfig.SimulationTickRate;

            foreach (var (transform, hoveringWallComponent)
                in SystemAPI.Query<
                    RefRW<LocalTransform>, HoveringEntityComponent>()
                        .WithAll<Simulate>())// <-- built in component all entities have (online or not),
                                             //     used for updating on predicted entities
            {
                // move platform up and down (testing with a simple sine function)
                var originalPosition = hoveringWallComponent.originalPosition;
                originalPosition.y +=
                    (hoveringWallComponent.sinHeight *
                        (float)math.sin(elapsedTime * hoveringWallComponent.sinLenght));

                transform.ValueRW.Position = originalPosition;
            }
        }
    }
}
