using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [GhostComponent]
    public struct HoveringWallComponent : IComponentData
    {
        public float3 originalPosition;
    }
}
