using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Multiplayer;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// This struct contains the necessary information from the <see cref="NetworkConfiguration"/> instance
    /// that are required to create the right <see cref="NetworkStreamDriver"/> on world creation.
    /// </summary>
    struct EntityNetworkConfiguration
    {
        public NetworkRole Role;
        public NetworkType Type;
        public RelayServerData RelayClientData;
        public RelayServerData RelayServerData;
    }

    /// <summary>
    /// This class creates the required <see cref="NetworkStreamDriver"/> on world creation for the Server and Client world.
    /// </summary>
    /// <remarks>
    /// It is using the <see cref="NetworkConfiguration"/> information when used from a Session within <see cref="GameManager.StartGameAsync"/>.
    /// When starting from <see cref="GameBootstrap"/>, it will instead default drivers setup to <see cref="NetworkType.Direct"/>.
    /// </remarks>
    class EntityDriverConstructor : INetworkStreamDriverConstructor
    {
        EntityNetworkConfiguration m_Configuration;

        public EntityDriverConstructor(NetworkConfiguration configuration)
        {
            m_Configuration = new EntityNetworkConfiguration
            {
                Role = configuration.Role,
                Type = configuration.Type,
                RelayClientData = configuration.RelayClientData,
                RelayServerData = configuration.RelayServerData,
            };
        }

        public EntityDriverConstructor(NetworkRole role)
        {
            m_Configuration = new EntityNetworkConfiguration
            {
                Role = role,
                Type = NetworkType.Direct,
            };
        }

        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var networkSettings = GameNetworkSettings(
                sendQueueCapacity: 64, // Client only needs small buffers.
                receiveQueueCapacity: 64);
#if UNITY_EDITOR || NETCODE_DEBUG
            if (NetworkSimulatorSettings.Enabled)
            {
                NetworkSimulatorSettings.SetSimulatorSettings(ref networkSettings);
            }
#endif

            if (m_Configuration.Type == NetworkType.Relay)
            {
                networkSettings.WithRelayParameters(ref m_Configuration.RelayClientData);
                DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, networkSettings);
            }
            else
            {
                DefaultDriverBuilder.RegisterClientDriver(world, ref driverStore, netDebug, networkSettings);
            }
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            const int expectedMaxPlayerCount = 1000;
            var networkSettings = GameNetworkSettings(
                sendQueueCapacity: expectedMaxPlayerCount * 4,
                receiveQueueCapacity: expectedMaxPlayerCount * 16 // This is how many incoming messages per player
                // the server can cache.
            );

            // Ipc driver is not needed unless we are self-connecting.
            if(m_Configuration.Role == NetworkRole.Host)
                DefaultDriverBuilder.RegisterServerIpcDriver(world, ref driverStore, netDebug, networkSettings);

            if (m_Configuration.Type == NetworkType.Relay)
            {
                networkSettings.WithRelayParameters(ref m_Configuration.RelayServerData);
            }
            DefaultDriverBuilder.RegisterServerUdpDriver(world, ref driverStore, netDebug, networkSettings);
#else
            throw new NotSupportedException(
                "Creating a server driver for a WebGL build is not supported. You can't listen on a WebSocket in the browser." +
                " WebGL builds should be ideally client-only (has UNITY_CLIENT define) and in case a Client/Server build is made, only client worlds should be created.");
#endif
        }

        static NetworkSettings GameNetworkSettings(int sendQueueCapacity, int receiveQueueCapacity)
        {
            var networkSettings = new NetworkSettings(Allocator.Temp);
            networkSettings.WithNetworkConfigParameters(
                connectTimeoutMS: 500,
                maxConnectAttempts: 8, // Don't spend 30s trying to connect, spend 4s.
                disconnectTimeoutMS: 3_500, // Timeouts should be relatively fast too, as it's pretty obvious when you've lost connection.
                reconnectionTimeoutMS: 1_500, // For mobile, reconnect timeouts should be aggressively low.
                sendQueueCapacity: sendQueueCapacity,
                receiveQueueCapacity: receiveQueueCapacity
            );
            return networkSettings;
        }
    }
}
