using System;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using System.Threading;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class GameManager : MonoBehaviour
    {
        public const int MaxPlayer = 16;
        public const string MainMenuSceneName = "MainMenu";
        public const string GameSceneName = "GameScene";
        public const string ResourcesSceneName = "GameResources";

        /// <summary>
        /// Flag denotes whether we can go back to the main menu.
        /// When using the GameBootstrap to start the game from a gameplay scene, the main menu scene is not accessible.
        /// </summary>
        public static bool CanUseMainMenu => SceneManager.GetActiveScene().name == MainMenuSceneName;

        /// <summary>Singleton instance.</summary>
        public static GameManager Instance { get; private set; }

        public ServicesSettings CurrentSessionSettings;
        public Transform PlayerNameContainer;

        enum SpectatorState
        {
            DoNotOverride,
            ForcePlayer,
            ForceSpectator,
        }
        [SerializeField]
        SpectatorState m_OverrideSpectatorState;

        GameConnection m_GameConnection;
        Task m_LoadingGame;
        CancellationTokenSource m_LoadingGameCancel;
        Task m_LoadingMainMenu;
        CancellationTokenSource m_LoadingMainMenuCancel;

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Debug.LogError($"Multiple instances of '{this}' instance found!", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("Game manager instance created");
        }

        async void Start()
        {
#if UNITY_EDITOR
            GameSettings.Instance.SpectatorToggle =
                m_OverrideSpectatorState == SpectatorState.DoNotOverride ? GameSettings.Instance.SpectatorToggle :
                    m_OverrideSpectatorState == SpectatorState.ForceSpectator;
#endif

            GameSettings.Instance.MainMenuSceneLoaded = false;
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                m_LoadingMainMenuCancel = new CancellationTokenSource();
                try
                {
                    m_LoadingMainMenu = StartMenuAsync(m_LoadingMainMenuCancel.Token);
                    await m_LoadingMainMenu;
                }
                catch (OperationCanceledException)
                {
                    // Nothing to do when the task is cancelled.
                }
                finally
                {
                    m_LoadingMainMenuCancel.Dispose();
                    m_LoadingMainMenuCancel = null;
                }
            }
        }

        void LateUpdate()
        {
            var gameInput = GameInput.Actions;

            if (gameInput.DebugActions.ReturnToMainMenu.WasPerformedThisFrame())
            {
                if (GameSettings.Instance.GameState != GlobalGameState.MainMenu) ReturnToMainMenuAsync();
                else QuitAsync();
            }

			if (gameInput.DebugActions.StartClientServer.WasPerformedThisFrame()) StartGameAsync(CreationType.Create);
            if (gameInput.DebugActions.StartClient.WasPerformedThisFrame()) StartGameAsync(CreationType.Join);
        }

        async Task StartMenuAsync(CancellationToken cancellationToken)
        {
            DestroyLocalSimulationWorld();
            var clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            await ScenesLoader.LoadGameplayAsync(null, clientWorld);
            GameSettings.Instance.MainMenuSceneLoaded = true;
            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// This method start the Gameplay session.
        /// </summary>
        /// <remarks>
        /// It is an asynchronous method because it is waiting on several API like <see cref="GameConnection"/> and <see cref="ScenesLoader"/>.
        /// </remarks>
        public async void StartGameAsync(CreationType creationType)
        {
            if (GameSettings.Instance.GameState != GlobalGameState.MainMenu)
            {
                Debug.Log("[StartGameAsync] Called but in-game, cannot start while in-game!");
                return;
            }
            Debug.Log($"[{nameof(StartGameAsync)}] Called with creation type '{creationType}'");

            if (creationType == CreationType.Create && CurrentSessionSettings.ConnectionTypeRequested == ConnectionType.Direct)
            {
                GameSettings.Instance.CancellableUserInputPopUp = new AwaitableCompletionSource();
                GameSettings.Instance.MainMenuState = MainMenuState.DirectConnectPopUp;
                try
                {
                    await GameSettings.Instance.CancellableUserInputPopUp.Awaitable;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    GameSettings.Instance.MainMenuState = MainMenuState.MainMenuScreen;
                }
            }
            else if (creationType == CreationType.Join)
            {
                GameSettings.Instance.CancellableUserInputPopUp = new AwaitableCompletionSource();
                GameSettings.Instance.MainMenuState = MainMenuState.JoinCodePopUp;
                try
                {
                    await GameSettings.Instance.CancellableUserInputPopUp.Awaitable;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    GameSettings.Instance.MainMenuState = MainMenuState.MainMenuScreen;
                }
            }
            else if (creationType == CreationType.QuickJoin &&
                     CurrentSessionSettings.MatchmakerTypeRequested == MatchmakerType.P2P &&
                     CurrentSessionSettings.ConnectionTypeRequested == ConnectionType.Direct)
            {
                GameSettings.Instance.CancellableUserInputPopUp = new AwaitableCompletionSource();
                GameSettings.Instance.MainMenuState = MainMenuState.DirectConnectPopUp;
                try
                {
                    await GameSettings.Instance.CancellableUserInputPopUp.Awaitable;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                finally
                {
                    GameSettings.Instance.MainMenuState = MainMenuState.MainMenuScreen;
                }
            }

            BeginEnteringGame();

            m_LoadingGameCancel = new CancellationTokenSource();
            try
            {
                m_LoadingGame = StartGameAsync(creationType, m_LoadingGameCancel.Token);
                await m_LoadingGame;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[{nameof(StartGameAsync)}] Loading has been cancelled.");
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(StartGameAsync)}] Loading has failed, returning to main menu");
                Debug.LogException(e);
                // Disposing the token here because the error has been handled and ReturnToMainMenu should not check it.
                m_LoadingGameCancel.Dispose();
                m_LoadingGameCancel = null;
                ReturnToMainMenuAsync();
                return;
            }
            finally
            {
                m_LoadingGameCancel?.Dispose();
                m_LoadingGameCancel = null;
            }

            FinishLoadingGame();
        }

        async Task StartGameAsync(CreationType creationType, CancellationToken cancellationToken)
        {
            // If the MainMenu world is loaded or loading, we need to unload it before joining the game.
            if (m_LoadingMainMenuCancel != null || GameSettings.Instance.MainMenuSceneLoaded)
            {
                if (m_LoadingMainMenuCancel != null)
                {
                    m_LoadingMainMenuCancel.Cancel();
                    try
                    {
                        await m_LoadingMainMenu;
                    }
                    catch (OperationCanceledException)
                    {
                        // We are ignoring the cancelled exception as it is expected.
                    }
                }

                if(GameSettings.Instance.MainMenuSceneLoaded)
                    await DisconnectAndUnloadWorlds();

                cancellationToken.ThrowIfCancellationRequested();
            }

            // Connecting to a Multiplayer Session.
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.InitializeConnection);
            switch (creationType)
            {
                case (CreationType.Create):
                {
                    m_GameConnection = await GameConnection.CreateGameAsync();
                    break;
                }
                case (CreationType.Join):
                {
                    m_GameConnection = await GameConnection.JoinGameAsync();
                    break;
                }
                case (CreationType.QuickJoin):
                {
                    m_GameConnection = await GameConnection.JoinOrCreateMatchmakerGameAsync(cancellationToken);
                    break;
                }
            }

            m_GameConnection.Session.RemovedFromSession += OnSessionLeft;

            cancellationToken.ThrowIfCancellationRequested();

            ConnectionSettings.Instance.SessionCode = m_GameConnection.Session.Code;

            // Creating entity worlds.
            CreateEntityWorlds(m_GameConnection.Session, m_GameConnection.SessionConnectionType, out var server, out var client);

            // If we have a server, start listening.
            if (server != null)
            {
                using var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                var serverDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
                serverDriver.Listen(m_GameConnection.ListenEndpoint);
            }
            if (client != null)
            {
                ConnectionSettings.Instance.ConnectionEndpoint = m_GameConnection.ConnectEndpoint;
                await WaitForPlayerConnectionAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Worlds are created and connected, game is ready to load and start.
            await ScenesLoader.LoadGameplayAsync(server, client);

            cancellationToken.ThrowIfCancellationRequested();

            if (client != null)
            {
                await WaitForGhostReplicationAsync(client, cancellationToken);
                await WaitForAttachedCameraAsync(client, cancellationToken);
            }

        }

        void OnSessionLeft()
        {
            m_GameConnection = null;
            ReturnToMainMenuAsync();
        }

        /// <summary>
        /// Called from the GameBootstrap.
        /// This method assumes the client world has been created and is connected to a server.
        /// </summary>
        public async void StartFromBootstrapAsync(World server, World client)
        {
            if (GameSettings.Instance.GameState != GlobalGameState.MainMenu)
            {
                Debug.Log($"[{nameof(StartFromBootstrapAsync)}] Must not be in-game to join game!");
                return;
            }
            if (SceneManager.GetActiveScene().name == MainMenuSceneName)
            {
                Debug.Log($"Must not be in {MainMenuSceneName} to use [{nameof(StartFromBootstrapAsync)}]!");
                return;
            }

            Debug.Log($"[{nameof(StartFromBootstrapAsync)}] Starting game");
            BeginEnteringGame();

            // The bootstrap is creating the worlds and start the connection for us,
            // let's make sure the client is connected before the next step.
            if (client != null)
            {
                await WaitForPlayerConnectionAsync();
            }

            // Load any additional scene that would be required by the Gameplay.
            await ScenesLoader.LoadGameplayAsync(server, client);

            if (client != null)
            {
                await WaitForGhostReplicationAsync(client);
                await WaitForAttachedCameraAsync(client);
            }

            FinishLoadingGame();
        }

        void BeginEnteringGame()
        {
            GameSettings.Instance.GameState = GlobalGameState.Loading;
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.StartLoading);
        }

        static void CreateEntityWorlds(ISession session, NetworkType connectionType,
            out World serverWorld, out World clientWorld)
        {
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.CreateWorld);
            DestroyLocalSimulationWorld();

#if UNITY_EDITOR
            if (connectionType == NetworkType.Relay && MultiplayerPlayModePreferences.RequestedNumThinClients > 0)
            {
                Debug.Log($"[{nameof(CreateEntityWorlds)}] A number of Thin Clients was set while the connection mode is set to use Relay. Disabling Thin Clients.");
                MultiplayerPlayModePreferences.RequestedNumThinClients = 0;
            }
#endif

            serverWorld = null;
            clientWorld = null;
            if (session.IsHost)
            {
                serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            }
            if (!session.IsServer())
            {
                clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            }
        }

        /// <summary>
        /// Destroy all local game simulation worlds if any before creating new server/client worlds.
        /// </summary>
        public static void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
            }
        }

        static async Task DestroyGameSessionWorlds()
        {
            // This prevents the "Cannot dispose world while updating it" error,
            // allowing us to call this from anywhere.
            await Awaitable.EndOfFrameAsync();

            // Destroy netcode worlds:
            for (var i = World.All.Count - 1; i >= 0; i--)
            {
                var world = World.All[i];
                if (world.IsServer() || world.IsClient())
                {
                    world.Dispose();
                }
            }
        }

        static async Task WaitForPlayerConnectionAsync(CancellationToken cancellationToken = default)
        {
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WaitingConnection);
            // The GameManagerSystem is handling the connection/reconnection once the client world is created.
            ConnectionSettings.Instance.GameConnectionState = GameConnectionState.Connecting;
            while (ConnectionSettings.Instance.GameConnectionState == GameConnectionState.Connecting)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Wait until the vast majority of ghosts have been spawned.
        /// If we don't do this, we'll see a bunch of ghosts pop in as the scene loads.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        static async Task WaitForGhostReplicationAsync(World world, CancellationToken cancellationToken = default)
        {
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WorldReplication);
            using var ghostCountQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GhostCount>());
            var waitedForTicks = 0;
            while (true)
            {
                if (ghostCountQuery.TryGetSingleton<GhostCount>(out var ghostCount))
                {
                    var synchronizingPercentage = ghostCount.GhostCountOnServer == 0
                        ? math.saturate(ghostCount.GhostCountOnClient / (float)ghostCount.GhostCountOnServer)
                        : waitedForTicks > 60 ? 1f : 0f; // Apparently the server has no ghosts to send us, so ghost loading is complete.

                    LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WorldReplication, synchronizingPercentage);
                    if (synchronizingPercentage > 0.99f) // A bit of wiggle room, because in most games, ghosts are constantly created and destroyed.
                        return;
                }
                await Awaitable.NextFrameAsync(cancellationToken);
                waitedForTicks++;
            }
        }

        static async Task WaitForAttachedCameraAsync(World world, CancellationToken cancellationToken = default)
        {
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WaitingOnPlayer);
            using var mainEntityCameraQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<MainCamera>());
            while (!mainEntityCameraQuery.HasSingleton<MainCamera>())
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
            // Waiting an extra frame so that the player position is properly synced with the server.
            await Awaitable.NextFrameAsync(cancellationToken);
        }

        void FinishLoadingGame()
        {
            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.LoadingDone);
            GameSettings.Instance.GameState = GlobalGameState.InGame;
        }

        /// <summary>
        /// Safe return to main menu, can be called by the pause menu button.
        /// </summary>
        public async void ReturnToMainMenuAsync()
        {
            Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Called.");
            if (!CanUseMainMenu)
            {
                QuitAsync();
                return;
            }

            if (m_LoadingGameCancel != null)
            {
                Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Cancelling loading game.");
                m_LoadingGameCancel.Cancel();
                try
                {
                    await m_LoadingGame;
                }
                catch (OperationCanceledException)
                {
                    // Discarding this exception because we're the one asking for it.
                }
                Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Loading Cancelled, start returning to main menu.");
            }

            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.UnloadingGame);
            GameSettings.Instance.GameState = GlobalGameState.Loading;

            GameSettings.Instance.IsPauseMenuOpen = false;
            await DisconnectAndUnloadWorlds();

            // Restart the main menu scene.
            Start();

            LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.BackToMainMenu);
            GameSettings.Instance.GameState = GlobalGameState.MainMenu;
        }

        async Task DisconnectAndUnloadWorlds()
        {
            ConnectionSettings.Instance.GameConnectionState = GameConnectionState.NotConnected;

            bool requestedDisconnect = false;
            foreach (var world in World.All)
            {
                if (world.IsClient())
                {
                    using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
                    if (query.TryGetSingletonEntity<NetworkId>(out var networkId))
                    {
                        requestedDisconnect = true;
                        world.EntityManager.AddComponentData(networkId, new NetworkStreamRequestDisconnect());
                    }
                }
            }

            if (requestedDisconnect)
                await Awaitable.NextFrameAsync();

            await LeaveSessionAsync();
            await DestroyGameSessionWorlds();
            await ScenesLoader.UnloadGameplayScenesAsync();
        }

        async Task LeaveSessionAsync()
        {
            if (m_GameConnection != null)
            {
                m_GameConnection.Session.RemovedFromSession -= OnSessionLeft;

                if (m_GameConnection.Session.IsHost || m_GameConnection.Session.IsServer())
                    ConnectionSettings.Instance.SessionCode = null;

                if (m_GameConnection.Session.IsHost)
                    await m_GameConnection.Session.AsHost().DeleteAsync();
                else
                    await m_GameConnection.Session.LeaveAsync();

                m_GameConnection = null;
            }
        }

        /// <summary>
        /// Safe shutdown of the game. Saves everything that needs to be saved.
        /// </summary>
        public async void QuitAsync()
        {
            await LeaveSessionAsync();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Debug.Log("[GameFlowManager] Application.Quit called!");
            Application.Quit();
#endif
        }
    }
}
