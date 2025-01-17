using Unity.Entities;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// Updates the <see cref="MainGameObjectCamera"/> postion to match the current player <see cref="MainCamera"/> component position if it exists.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MainCameraSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<MainCamera>();
        }

        protected override void OnUpdate()
        {
            if (MainGameObjectCamera.Instance != null)
            {
                // Move Camera:
                Entity mainEntityCameraEntity = SystemAPI.GetSingletonEntity<MainCamera>();
                MainCamera mainCamera = SystemAPI.GetSingleton<MainCamera>();
                LocalToWorld targetLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(mainEntityCameraEntity);
                MainGameObjectCamera.Instance.transform.SetPositionAndRotation(targetLocalToWorld.Position,
                    targetLocalToWorld.Rotation);
                MainGameObjectCamera.Instance.fieldOfView = mainCamera.CurrentFov;
            }
        }
    }
}
