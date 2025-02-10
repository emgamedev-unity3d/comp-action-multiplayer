using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// System to send a request from the Client World to request the server
    /// to enable "Party Mode" for all clients
    /// </summary>
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    partial struct PartyBalloonSpawnerClientSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PartyBalloonSpawnerComponent>();
            state.RequireForUpdate<NetworkStreamInGame>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var defaultActionsMap = GameInput.Actions.Gameplay;

            // If client presses the "party" key, send the request
            if (!defaultActionsMap.RequestTemporaryBalloonParty.WasPressedThisFrame())
                return;

            UnityEngine.Debug.Log("Party button pressed!");

            // create the entity which has the RPC to be sent to the server
            state.EntityManager.CreateEntity(
                ComponentType.ReadOnly<ClientRequestPartyBalloonsRpc>(),
                ComponentType.ReadWrite<SendRpcCommandRequest>());
        }
    }
}
