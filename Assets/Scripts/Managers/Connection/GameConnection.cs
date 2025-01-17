using System.Threading;
using System.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// This extension methods help with some specificities of the <see cref="ISession"/> interface.
    /// </summary>
    public static class SessionExtension
    {
        /// <summary>
        /// This method is used to know if the current <see cref="ISession"/> is a pure server and not a client.
        /// </summary>
        /// <returns>True if the session is only a server and does not have a client, false otherwise.</returns>
        public static bool IsServer(this ISession session)
        {
            return session.IsHost && session.CurrentPlayer?.Id == null;
        }
    }

    /// <summary>
    /// This class is a wrapper around <see cref="MultiplayerService.Instance"/> session API.
    /// </summary>
    /// <remarks>
    /// The purpose of this class is to encapsulate the need for a custom <see cref="INetworkHandler"/> that we are using
    /// to retrieve the Listen and Connect <see cref="NetworkEndpoint"/> while connecting through the <see cref="ISession"/> API.
    /// This is done in order to control the Worlds creation and Connection in <see cref="GameManager.StartGameAsync"/>.
    /// </remarks>
    public class GameConnection
    {
        public ISession Session { get; private set; }
        public NetworkEndpoint ListenEndpoint { get; private set; }
        public NetworkEndpoint ConnectEndpoint { get; private set; }
        public NetworkType SessionConnectionType { get; private set; }

        public static async Task<GameConnection> JoinGameAsync()
        {
            var gameConnection = new GameConnection();
            await StartServicesAsync();

            var networkHandler = new EntityNetworkHandler();
            JoinSessionOptions options = new JoinSessionOptions();
            options.WithNetworkHandler(networkHandler);
            gameConnection.Session = await MultiplayerService.Instance.JoinSessionByCodeAsync(ConnectionSettings.Instance.SessionCode, options);
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;
            return gameConnection;
        }

        public static async Task<GameConnection> JoinOrCreateMatchmakerGameAsync(CancellationToken cancellationToken)
        {
            var gameConnection = new GameConnection();
            await StartServicesAsync();

            ConnectionSettings.Instance.GameConnectionState = GameConnectionState.Matchmaking;
            var options = CreateSessionOptions(GameManager.Instance.CurrentSessionSettings.ConnectionTypeRequested, ConnectionSettings.Instance.IPAddress, ConnectionSettings.Instance.Port);
            var networkHandler = new EntityNetworkHandler();
            options.WithNetworkHandler(networkHandler);
            MatchmakerOptions match = new MatchmakerOptions
            {
                QueueName = GameManager.Instance.CurrentSessionSettings.MatchmakerTypeRequested == MatchmakerType.Dgs
                    ? "multiplayer-n4e-dgs"
                    : "multiplayer-n4e-p2p",
            };
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.LookingForMatch);
            gameConnection.Session = await MultiplayerService.Instance.MatchmakeSessionAsync(match, options, cancellationToken);
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;

            return gameConnection;
        }

        public static async Task<GameConnection> CreateGameAsync()
        {
            var gameConnection = new GameConnection();
            await StartServicesAsync();

            var options = CreateSessionOptions(GameManager.Instance.CurrentSessionSettings.ConnectionTypeRequested, ConnectionSettings.Instance.IPAddress, ConnectionSettings.Instance.Port);
            var networkHandler = new EntityNetworkHandler();
            options.WithNetworkHandler(networkHandler);
            gameConnection.Session = await MultiplayerService.Instance.CreateSessionAsync(options);
            gameConnection.ConnectEndpoint = await networkHandler.ConnectEndpoint;
            gameConnection.ListenEndpoint = await networkHandler.ListenEndpoint;
            gameConnection.SessionConnectionType = await networkHandler.SessionConnectionType;

            return gameConnection;
        }

        static async Task StartServicesAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsAuthorized)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        static SessionOptions CreateSessionOptions(ConnectionType connectionType, string address, string port)
        {
            SessionOptions options = new SessionOptions { MaxPlayers = GameManager.MaxPlayer };
            switch (connectionType)
            {
                case ConnectionType.Relay:
                    options.WithRelayNetwork();
                    break;
                case ConnectionType.Direct:
                    options.WithDirectNetwork("0.0.0.0", address, ushort.Parse(port));
                    break;
            }
            return options;
        }
    }
}
