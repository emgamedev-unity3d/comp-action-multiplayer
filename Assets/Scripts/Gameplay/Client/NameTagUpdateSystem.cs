using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public struct NameTagProxy : IComponentData
    {
        public Entity PlayerEntity;
    }

    public class NameTagProxyCleanup : ICleanupComponentData
    {
        public UIDocument UIDocumentComponent;
    }

    /// <summary>
    /// This class creates and update the position of the players name on each character currently playing.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class NameTagUpdateSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<GameManagedResources>();
        }

        protected override void OnUpdate()
        {
            SpawnNameTag();
            UpdateNameTagPosition();
            CleanUpNameTag();
        }

        void SpawnNameTag()
        {
            EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(World.Unmanaged);
            foreach (var (nameTag, entity) in SystemAPI.Query<RefRO<NameTagProxy>>().WithNone<NameTagProxyCleanup>().WithEntityAccess())
            {
                Entity playerEntity = nameTag.ValueRO.PlayerEntity;

                var playerName = "Player";
                if (EntityManager.HasComponent<FirstPersonPlayer>(playerEntity))
                    playerName = EntityManager.GetComponentData<FirstPersonPlayer>(playerEntity).Name.ToString();

                var playerNameContainer = GameManager.Instance.PlayerNameContainer;
                GameObject nameTagInstance = Object.Instantiate(SystemAPI.GetSingleton<GameManagedResources>().NameTagPrefab.Value, playerNameContainer.transform, true);
                var uiDocumentComponent = nameTagInstance.GetComponent<UIDocument>();
                uiDocumentComponent.rootVisualElement.Q<Label>().text = playerName;

                ecb.AddComponent(entity, new NameTagProxyCleanup { UIDocumentComponent = uiDocumentComponent });
            }
        }

        void CleanUpNameTag()
        {
            EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(World.Unmanaged);
            foreach (var (cleanup, entity) in SystemAPI.Query<NameTagProxyCleanup>().WithNone<NameTagProxy>().WithEntityAccess())
            {
                if (cleanup.UIDocumentComponent)
                {
                    cleanup.UIDocumentComponent.rootVisualElement.Clear();
                    Object.Destroy(cleanup.UIDocumentComponent.gameObject);
                }
                ecb.RemoveComponent<NameTagProxyCleanup>(entity);
            }
        }

        void UpdateNameTagPosition()
        {
            if (SystemAPI.HasSingleton<MainCamera>())
            {
                Entity mainCameraEntity = SystemAPI.GetSingletonEntity<MainCamera>();
                float3 mainCameraPosition = SystemAPI.GetComponent<LocalToWorld>(mainCameraEntity).Position;

                foreach (var (ltw, cleanup) in SystemAPI.Query<RefRO<LocalToWorld>, NameTagProxyCleanup>())
                {
                    if (cleanup.UIDocumentComponent)
                    {
                        var ltwPosition = ltw.ValueRO.Position;
                        var lookAtDirection = ltwPosition - mainCameraPosition;
                        cleanup.UIDocumentComponent.transform.SetPositionAndRotation(ltwPosition, Quaternion.LookRotation(lookAtDirection));
                    }
                }
            }
        }
    }
}
