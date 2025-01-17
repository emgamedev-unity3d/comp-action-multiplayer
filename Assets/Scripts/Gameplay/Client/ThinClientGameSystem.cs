using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    namespace GameManagement
    {
        /// <summary>
        /// Automatically connect thin clients to the Server World the player is connected to.
        /// </summary>
        [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
        public partial struct ThinClientGameSystem : ISystem
        {
            bool m_HasAttemptedToConnectAtLeastOnce;

            public void OnCreate(ref SystemState state)
            {
                state.RequireForUpdate<GameResources>();
            }

            public void OnUpdate(ref SystemState state)
            {
                // In the editor, only connect the first time as users should be able to
                // use the netcode window to change connection status.
                if (Application.isEditor && m_HasAttemptedToConnectAtLeastOnce) return;

                // No main client world to follow, so cannot connect.
                var clientWorld = ClientServerBootstrap.ClientWorld;
                if (clientWorld == null || !clientWorld.IsCreated) return;

                // Wait until after the main client is loading in.
                if (ConnectionSettings.Instance.GameConnectionState != GameConnectionState.Connected) return;

                // No need to call connect if already got a connection.
                if (SystemAPI.HasSingleton<NetworkStreamConnection>())
                {
                    m_HasAttemptedToConnectAtLeastOnce = true;
                    return;
                }

                // Connect:
                ref var networkStreamDriver = ref SystemAPI.GetSingletonRW<NetworkStreamDriver>().ValueRW;
                networkStreamDriver.Connect(state.EntityManager, ConnectionSettings.Instance.ConnectionEndpoint);
            }
        }
    }
}
