using Unity.Entities;
using Unity.NetCode;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [GhostComponent]
    [GhostEnabledBit]
    public struct DelayedDespawn : IComponentData, IEnableableComponent
    {
        public uint Ticks;
        public byte HasHandledPreDespawn;
    }
}
