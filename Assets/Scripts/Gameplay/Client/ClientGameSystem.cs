using System;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// Client to server. Required by the server.
    /// TODO - Server should time out players who don't send this.
    /// </summary>
    public struct ClientJoinRequestRpc : IRpcCommand
    {
        public FixedString128Bytes PlayerName;
        public bool IsSpectator;
    }

    /// <summary>
    /// Client request a death + respawn from the server. For testing purposes.
    /// </summary>
    public struct ClientRequestRespawnRpc : IRpcCommand
    {
    }

    /// <summary>
    /// This system updates the player state used in the <see cref="RespawnScreen"/>.
    ///
    /// The player is considered alive once the Camera is attached to it.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct PlayerStatusSystem : ISystem
    {
        EntityQuery m_PlayerAliveQuery;

        public void OnCreate(ref SystemState state)
        {
            m_PlayerAliveQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MainCamera>().Build(state.EntityManager);
        }

        public void OnUpdate(ref SystemState state)
        {
            // It's not possible to check on the CharacterInitialized component because in the case of Spectator mode it does not exist.
            GameSettings.Instance.PlayerState = m_PlayerAliveQuery.IsEmpty ? PlayerState.Dead : PlayerState.Playing;
        }
    }

    /// <summary>
    /// This system handles the player forced respawn using
    /// the <see cref="FPSInputActions.GameplayActions.RequestRespawn"/> action.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct DebugPlayerRespawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<FirstPersonCharacterComponent, GhostOwnerIsLocal>().WithDisabled<DelayedDespawn>()
                .Build(state.EntityManager));
        }

        public void OnUpdate(ref SystemState state)
        {
            if (GameInput.Actions.Gameplay.RequestRespawn.WasPerformedThisFrame())
                state.EntityManager.CreateEntity(ComponentType.ReadWrite<ClientRequestRespawnRpc>(),
                    ComponentType.ReadWrite<SendRpcCommandRequest>());
        }
    }

    /// <summary>
    /// This system handles the client side of the player connection and character spawning.
    /// It creates the first player join request so the server knows it has to spawn a character.
    /// It handles the Spectator prefab spawn if the player is a spectator.
    /// It creates the NameTagProxy on any spawned character that is not the active player.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    public partial struct ClientGameSystem : ISystem
    {
        EntityQuery m_SpectatorSpawnPointsQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();

            var randomSeed = (uint)DateTime.Now.Millisecond;
            var randomEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(randomEntity, new FixedRandom
            {
                Random = Random.CreateFromIndex(randomSeed),
            });

            m_SpectatorSpawnPointsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpectatorSpawnPoint, LocalToWorld>().Build(state.EntityManager);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<DisableCharacterDynamicContacts>())
                state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<DisableCharacterDynamicContacts>());

            var gameResources = SystemAPI.GetSingleton<GameResources>();
            HandleSendJoinRequest(ref state, gameResources);
            HandleCharacterSetup(ref state);
        }

        void HandleSendJoinRequest(ref SystemState state, GameResources gameResources)
        {
            if (!SystemAPI.TryGetSingletonEntity<NetworkId>(out var clientEntity)
                || SystemAPI.HasComponent<NetworkStreamInGame>(clientEntity))
                return;

            var joinRequestEntity = state.EntityManager.CreateEntity(ComponentType.ReadOnly<ClientJoinRequestRpc>(),
                ComponentType.ReadWrite<SendRpcCommandRequest>());
            var playerName = GameSettings.Instance.PlayerName;
            if (state.WorldUnmanaged.IsThinClient()) // Random names for thin clients.
            {
                ref var random = ref SystemAPI.GetSingletonRW<FixedRandom>().ValueRW;
                playerName = $"[Bot {random.Random.NextInt(1, 99):00}] {playerName}";
            }
            var clientJoinRequestRpc = new ClientJoinRequestRpc();
            clientJoinRequestRpc.IsSpectator = GameSettings.Instance.SpectatorToggle;
            clientJoinRequestRpc.PlayerName.CopyFromTruncated(playerName); // Prevents exceptions on long strings.
            state.EntityManager.SetComponentData(joinRequestEntity, clientJoinRequestRpc);
            state.EntityManager.AddComponentData(clientEntity, new NetworkStreamInGame());

            // Spectator mode
            if (GameSettings.Instance.SpectatorToggle)
            {
                LocalToWorld spawnPoint = default;
                using var spectatorSpawnPoints =
                    m_SpectatorSpawnPointsQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
                if (spectatorSpawnPoints.Length > 0)
                {
                    ref var random = ref SystemAPI.GetSingletonRW<FixedRandom>().ValueRW;
                    spawnPoint = spectatorSpawnPoints[random.Random.NextInt(0, spectatorSpawnPoints.Length - 1)];
                }

                var spectatorEntity = state.EntityManager.Instantiate(gameResources.SpectatorPrefab);
                state.EntityManager.SetComponentData(spectatorEntity,
                    LocalTransform.FromPositionRotation(spawnPoint.Position, spawnPoint.Rotation));
            }
        }

        void HandleCharacterSetup(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .ValueRW
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Initialize local-owned characters
            foreach (var (character, entity) in SystemAPI
                         .Query<FirstPersonCharacterComponent>()
                         .WithAll<GhostOwnerIsLocal, OwningPlayer, GhostOwner>()
                         .WithDisabled<CharacterInitialized>()
                         .WithEntityAccess())
            {
                // Make camera follow character's view
                ecb.AddComponent(character.ViewEntity, new MainCamera
                {
                    BaseFov = character.BaseFov,
                });
                // Make local character meshes rendering be shadow-only
                var childBufferLookup = SystemAPI.GetBufferLookup<Child>();
                MiscUtilities.SetShadowModeInHierarchy(state.EntityManager, ecb, entity, ref childBufferLookup,
                    ShadowCastingMode.ShadowsOnly);
            }

            // Initialize remote characters
            foreach (var (character, owningPlayer) in SystemAPI
                         .Query<FirstPersonCharacterComponent, OwningPlayer>()
                         .WithNone<GhostOwnerIsLocal>()
                         .WithDisabled<CharacterInitialized>())
                // Spawn nameTag
                ecb.AddComponent(character.NameTagSocketEntity, new NameTagProxy
                {
                    PlayerEntity = owningPlayer.Entity,
                });

            // Initialize characters common
            foreach (var (physicsCollider, characterInitialized, entity) in SystemAPI
                         .Query<RefRW<PhysicsCollider>, EnabledRefRW<CharacterInitialized>>()
                         .WithAll<FirstPersonCharacterComponent>()
                         .WithDisabled<CharacterInitialized>()
                         .WithEntityAccess())
            {
                physicsCollider.ValueRW.MakeUnique(entity, ecb);

                // Mark initialized
                characterInitialized.ValueRW = true;
            }
        }
    }
}
