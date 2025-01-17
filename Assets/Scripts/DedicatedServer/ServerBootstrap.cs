#if UNITY_SERVER
using System;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Authentication.Server;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// This class is a specific game start used when building as a Dedicated Game Server.
    /// </summary>
    public class ServerBootstrap : MonoBehaviour
    {
        IMultiplaySessionManager m_MultiplayManager;

        // Start is called once before the first execution of Update and after the MonoBehaviour is created
        async void Start()
        {
            GameSettings.Instance.GameState = GlobalGameState.Loading;

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    Debug.LogError("Unable to initialize services?");
                    return;
                }
            }

            if (!ServerAuthenticationService.Instance.IsAuthorized)
            {
                await ServerAuthenticationService.Instance.SignInFromServerAsync();
                if (!ServerAuthenticationService.Instance.IsAuthorized)
                {
                    Debug.LogError("Unable to authorize");
                    return;
                }
            }

            var networkHandler = new EntityNetworkHandler();
            try
            {
                // In Services 1.0.0, StartMultiplaySessionManagerAsync returns once the Manager is created but is not waiting on the server allocation.
                // We are adding a callback registration here to wait on the allocation
                // because we need to know the listen NetworkEndpoint created after the allocation to continue the server initialization
                var managerCallbacks = new MultiplaySessionManagerEventCallbacks();
                var serverAllocationTask = new TaskCompletionSource<IMultiplayAllocation>();
                managerCallbacks.Allocated += allocation => serverAllocationTask.SetResult(allocation);

                // Request server allocation
                m_MultiplayManager = await MultiplayerServerService.Instance.StartMultiplaySessionManagerAsync(
                    new MultiplaySessionManagerOptions
                    {
                        SessionOptions = new SessionOptions { MaxPlayers = GameManager.MaxPlayer }
                            .WithDirectNetwork()
                            .WithNetworkHandler(networkHandler)
                            .WithBackfillingConfiguration(enable: true, autoStart: true),
                        MultiplayServerOptions =
                            new MultiplayServerOptions("server", "gameplay", "1", "gameplay", false),
                        Callbacks = managerCallbacks,
                    });
                // Wait on allocation callback
                _ = await serverAllocationTask.Task;
            }
            catch (Exception e)
            {
                Debug.LogError("Multiplay services didn't start, see following exception.");
                Debug.LogException(e);
                return;
            }

            var listenEndpoint = await networkHandler.ListenEndpoint;

            m_MultiplayManager.Session.PlayerHasLeft += async _ =>
            {
                if (m_MultiplayManager.Session.PlayerCount == 0)
                {
                    Debug.Log("Last player is leaving the server, closing...");
                    await m_MultiplayManager.Session.DeleteAsync();
                    Application.Quit();
                }
            };

            // Destroy the local simulation world to avoid the game scene to be loaded into it.
            // This prevents rendering and other issues (rendering from multiple world with presentation is not greatly supported).
            GameManager.DestroyLocalSimulationWorld();
            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");

            using var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            drvQuery.CompleteDependency();
            var serverDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
            serverDriver.Listen(listenEndpoint);

            await ScenesLoader.LoadGameplayAsync(server, null);
            await m_MultiplayManager.SetPlayerReadinessAsync(true);
            GameSettings.Instance.GameState = GlobalGameState.InGame;
        }
    }
}
#endif
