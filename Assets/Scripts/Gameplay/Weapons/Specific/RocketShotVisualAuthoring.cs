using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [RequireComponent(typeof(PrefabProjectileAuthoring))]
    public class RocketShotVisualAuthoring : MonoBehaviour
    {
        public float DirectHitDamage;
        public float MaxRadiusDamage;
        public float DamageRadius;

        class Baker : Baker<RocketShotVisualAuthoring>
        {
            public override void Bake(RocketShotVisualAuthoring shotVisualAuthoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RocketShotVisual
                {
                    DirectHitDamage = shotVisualAuthoring.DirectHitDamage,
                    MaxRadiusDamage = shotVisualAuthoring.MaxRadiusDamage,
                    DamageRadius = shotVisualAuthoring.DamageRadius,
                });
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, DamageRadius);
        }
    }

    public struct RocketShotVisual : IComponentData, IEnableableComponent
    {
        public float DirectHitDamage;
        public float MaxRadiusDamage;
        public float DamageRadius;

        public byte HasProcessedHitSimulation;
        public byte HasProcessedHitVfx;
    }
}
