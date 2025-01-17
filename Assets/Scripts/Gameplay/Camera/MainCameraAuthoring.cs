using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [DisallowMultipleComponent]
    public class MainCameraAuthoring : MonoBehaviour
    {
        public float Fov = 75f;

        public class Baker : Baker<MainCameraAuthoring>
        {
            public override void Bake(MainCameraAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MainCamera(authoring.Fov));
            }
        }
    }

    /// <summary>
    /// This Camera component is added to the player character entity by the <see cref="ClientGameSystem"/>
    /// after the server spawns a new character.
    ///
    /// It is used by the <see cref="MainCameraSystem"/> to position the GameObject of the MainCamera at the player position.
    /// </summary>
    public struct MainCamera : IComponentData
    {
        public MainCamera(float fov)
        {
            BaseFov = fov;
            CurrentFov = fov;
        }

        public float BaseFov;
        public float CurrentFov;
    }
}
