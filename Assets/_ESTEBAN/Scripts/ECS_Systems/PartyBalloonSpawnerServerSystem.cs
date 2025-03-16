using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

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
            state.RequireForUpdate<
                BeginSimulationEntityCommandBufferSystem.Singleton>();
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
                         ReceiveRpcCommandRequest>()
                            .WithEntityAccess())
            {
                var ecbSingleton =
                    SystemAPI.GetSingletonRW<
                        BeginSimulationEntityCommandBufferSystem.Singleton>();

                var ecb = ecbSingleton.ValueRW.CreateCommandBuffer(
                    state.WorldUnmanaged);

                // destroy the RPC request entity, we no longer need it
                ecb.DestroyEntity(entity);
                Debug.Log("Party request received!");

                partyBalloonSpawner.ValueRW.isInPartyMode = true;


                // Spawn the balloons!
                for(int i = 0; i < 10; ++i)
                {
                    SpawnNewPartyBalloon(
                        ecb,
                        partyBalloonSpawner.ValueRO.partyBalloonPrefab,
                        new float3(
                            Random.Range(-5f, 5f),
                            5.4f,
                            Random.Range(-10f, 24f)));
                }
            }
        }

        private void SpawnNewPartyBalloon(
            EntityCommandBuffer ecb,
            Entity partyBalloonPrefab,
            float3 position)
        {
            var newPartyBalloon = ecb.Instantiate(partyBalloonPrefab);
            ecb.SetName(newPartyBalloon, "Party-Balloon");

            Debug.Log($"party balloon position is: {position}");

            var hoveringComp = new HoveringEntityComponent()
            {
                originalPosition = position,
                sinHeight = Random.Range(.8f, 1.2f),
                sinLenght = Random.Range(.6f, 1.1f)
            };

            ecb.AddComponent(newPartyBalloon, hoveringComp);

            var newPartyBalloonTransform =
                LocalTransform.FromPosition(position);

            ecb.SetComponent(newPartyBalloon, newPartyBalloonTransform);
        }
    }
}
