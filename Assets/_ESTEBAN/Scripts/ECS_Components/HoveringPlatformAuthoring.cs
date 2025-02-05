using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class HoveringPlatformAuthoring : MonoBehaviour
    {
        class Baker : Baker<HoveringPlatformAuthoring>
        {
            public override void Bake(HoveringPlatformAuthoring authoring)
            {
                Entity entity = GetEntity(
                    TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

                AddComponent(entity, new HoveringWallComponent()
                {
                    originalPosition = authoring.transform.position
                });
            }
        }
    }
}
