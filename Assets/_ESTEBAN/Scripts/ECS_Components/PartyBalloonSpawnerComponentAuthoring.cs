using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    class PartyBalloonSpawnerComponentAuthoring : MonoBehaviour
    {
        [Range(1f, 10f)]
        [SerializeField]
        private float partyModeDuration = 5f;

        [SerializeField]
        private GameObject partyBalloonPrefab;

        class PartyBalloonSpawnerComponentAuthoringBaker :
            Baker<PartyBalloonSpawnerComponentAuthoring>
        {
            public override void Bake(
                PartyBalloonSpawnerComponentAuthoring authoring)
            {
                var partyBallonPrefab = GetEntity(
                    authoring.partyBalloonPrefab,
                    TransformUsageFlags.Dynamic);

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(
                    entity,
                    new PartyBalloonSpawnerComponent()
                    {
                        partyModeDuration = authoring.partyModeDuration,
                        isInPartyMode = false,
                        partyBalloonPrefab = partyBallonPrefab
                    });
            }
        }
    }
}
