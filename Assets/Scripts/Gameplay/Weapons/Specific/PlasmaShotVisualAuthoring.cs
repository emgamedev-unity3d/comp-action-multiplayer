using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [RequireComponent(typeof(PrefabProjectileAuthoring))]
    public class PlasmaShotVisualAuthoring : MonoBehaviour
    {
        public float Damage;

        class Baker : Baker<PlasmaShotVisualAuthoring>
        {
            public override void Bake(PlasmaShotVisualAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlasmaShotVisual
                {
                    Damage = authoring.Damage,
                });
            }
        }
    }

    public struct PlasmaShotVisual : IComponentData
    {
        public float Damage;

        public byte HasProcessedHitSimulation;
        public byte HasProcessedHitVfx;
    }
}
