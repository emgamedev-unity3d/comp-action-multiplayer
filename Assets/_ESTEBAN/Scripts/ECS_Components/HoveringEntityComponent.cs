using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [GhostComponent]
    public struct HoveringEntityComponent : IComponentData
    {
        public float3 originalPosition;
        public float sinHeight;
        public float sinLenght;
    }
}
