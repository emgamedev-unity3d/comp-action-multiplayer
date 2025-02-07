using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Collections;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    partial struct PartyBalloonDeathSystem : ISystem
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
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (health, partyBalloonComponent, entity)
                in SystemAPI.Query<
                    RefRO<Health>, PartyBalloonComponent>()
                        .WithAll<Simulate>()
                        .WithEntityAccess())// <-- built in component all entities have (online or not),
                                             //     used for updating on predicted entities
            {
                if (!health.ValueRO.IsDead())
                    continue;

                // Entity (ex.party balloon) is dead, tag it for destruction
                entityCommandBuffer.AddComponent<DestroyEntityComponent>(entity);
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }
    }
}
