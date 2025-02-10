using Unity.Entities;
using Unity.NetCode;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public struct ClientRequestPartyBalloonsRpc : IRpcCommand
    {
    }

    [GhostComponent]
    public struct PartyBalloonSpawnerComponent : IComponentData
    {
        [GhostField]
        public bool isInPartyMode;
        public float partyModeDuration;
        public Entity partyBalloonPrefab;
    }
}