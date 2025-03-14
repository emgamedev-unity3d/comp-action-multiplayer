using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// System to handle input for the thin client
    /// </summary>
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct AddThinClientInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
            state.RequireForUpdate<GameResources>();
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<CommandTarget>(out var commandTargetRw))
                return;

            // Ensure AI has input entity:
            if (commandTargetRw.ValueRO.targetEntity == Entity.Null ||
                !state.EntityManager.HasComponent<FirstPersonPlayerCommands>(
                    commandTargetRw.ValueRO.targetEntity))
            {
                var inputEntity = state.EntityManager.CreateEntity();
                commandTargetRw.ValueRW.targetEntity = inputEntity;

                var connectionId = SystemAPI.GetSingleton<NetworkId>().Value;

                state.EntityManager.SetName(
                    inputEntity,
                    $"{nameof(AddThinClientInputSystem)}-RandInput");

                state.EntityManager.AddComponentData(
                    inputEntity,
                    new GhostOwner { NetworkId = connectionId });

                state.EntityManager.AddComponent<FirstPersonPlayerCommands>(inputEntity);
                state.EntityManager.AddComponent<
                    InputBufferData<FirstPersonPlayerCommands>>(inputEntity);
            }
        }
    }

    [UpdateAfter(typeof(AddThinClientInputSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ThinClientRandomInput : ISystem
    {
        private NativeReference<Random> m_random;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<NetworkTime>();

            m_random = new NativeReference<Random>(
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory)
            {
                Value = Random.CreateFromIndex((uint)Stopwatch.GetTimestamp())
            };
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonRW<CommandTarget>(out var commandTargetRw))
                return;

            // Recalculate AI action every x ticks:
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.ServerTick.IsValid)
                return;

            // Apply the current input values
            var rand = m_random.Value;
            var fpsPlayerCommands =
                state.EntityManager.GetComponentData<FirstPersonPlayerCommands>(
                commandTargetRw.ValueRW.targetEntity);


            if (networkTime.ServerTick.TickIndexForValidTick % 120 == 0)
            {
                if (rand.NextFloat(0f, 1f) >= 0.6f)
                    fpsPlayerCommands.ShootPressed.Set();
            }

            if (networkTime.ServerTick.TickIndexForValidTick % 100 == 0)
            {
                fpsPlayerCommands.MoveInput.x = rand.NextInt(-1, 2);
                fpsPlayerCommands.MoveInput.y = rand.NextInt(-1, 2);

                m_random.Value = rand;
                state.EntityManager.SetComponentData(
                    commandTargetRw.ValueRW.targetEntity,
                    fpsPlayerCommands);

                UnityEngine.Debug.Log(
                    $"CubeInput now is: Move Input = {fpsPlayerCommands.MoveInput}");
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            m_random.Dispose();
        }
    }
}
