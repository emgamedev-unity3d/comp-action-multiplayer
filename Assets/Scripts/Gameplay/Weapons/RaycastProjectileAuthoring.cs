using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    class RaycastProjectileAuthoring : MonoBehaviour
    {
        public float Damage = 1f;
        public float Range = 1000f;

        class Baker : Baker<RaycastProjectileAuthoring>
        {
            public override void Bake(RaycastProjectileAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RaycastProjectile
                {
                    Damage = authoring.Damage,
                    Range = authoring.Range,
                });
            }
        }
    }
}
