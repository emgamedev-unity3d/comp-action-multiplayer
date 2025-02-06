using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class PartyBalloonComponentAuthoring : MonoBehaviour
    {
        class Baker : Baker<PartyBalloonComponentAuthoring>
        {
            public override void Bake(PartyBalloonComponentAuthoring authoring)
            {
                Entity entity = GetEntity(
                    TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

                AddComponent(entity, new PartyBalloonComponent());
            }
        }
    }
}
