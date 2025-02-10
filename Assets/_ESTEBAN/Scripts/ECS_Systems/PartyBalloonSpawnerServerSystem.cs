using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{

    /// <summary>
    /// System to receive the request from the Client World to
    /// enable "Party Mode" for all clients
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    partial struct PartyBalloonSpawnerServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PartyBalloonSpawnerComponent>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // works for presentation purposes, because there's only one party
            // spawner
            var partyBalloonSpawner =
                SystemAPI.GetSingletonRW<PartyBalloonSpawnerComponent>();

            // if already in party mode, no need to process request
            if (partyBalloonSpawner.ValueRO.isInPartyMode)
                return;

            // Process join requests
            foreach (var (partyBalloonsRequest, rpcReceive, entity) in
                     SystemAPI.Query<
                         ClientRequestPartyBalloonsRpc,
                         ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                var ecbSingleton =
                    SystemAPI.GetSingletonRW<
                        BeginSimulationEntityCommandBufferSystem.Singleton>();

                var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

                // destroy the RPC request entity, we no longer need it
                ecb.DestroyEntity(entity);
                Debug.Log("Party request received!");

                partyBalloonSpawner.ValueRW.isInPartyMode = true;

                // Spawn the balloons!
                SpawnNewPartyBalloon(
                    ref state,
                    ecb,
                    partyBalloonSpawner.ValueRO.partyBalloonPrefab,
                    new float3(-1.79f, 1.38f, 22.68f));

                SpawnNewPartyBalloon(
                    ref state,
                    ecb,
                    partyBalloonSpawner.ValueRO.partyBalloonPrefab,
                    new float3(-1.55000019f, 5.38f, 15.68f));
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        private void SpawnNewPartyBalloon(
            ref SystemState state,
            EntityCommandBuffer ecb,
            Entity partyBalloonPrefab,
            float3 position)
        {
            var newPartyBalloon = ecb.Instantiate(partyBalloonPrefab);
            ecb.SetName(newPartyBalloon, "PartyBalloon!");

            var hoveringComp = new HoveringEntityComponent()
            {
                originalPosition = position,
                sinHeight = 1f,
                sinLenght = 0.8f
            };

            ecb.AddComponent(newPartyBalloon, hoveringComp);

            var newPartyBalloonTransform =
                LocalTransform.FromPosition(position);

            Debug.Log($"new party balloon transform {newPartyBalloonTransform}");

            ecb.SetComponent(newPartyBalloon, newPartyBalloonTransform);
        }
    }
}
