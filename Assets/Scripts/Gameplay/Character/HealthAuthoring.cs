using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class HealthAuthoring : MonoBehaviour
    {
        public float MaxHealth = 100f;

        public class Baker : Baker<HealthAuthoring>
        {
            public override void Bake(HealthAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Health
                {
                    MaxHealth = authoring.MaxHealth,
                    CurrentHealth = authoring.MaxHealth,
                });
            }
        }
    }

    [GhostComponent]
    public struct Health : IComponentData
    {
        public float MaxHealth;
        [GhostField(Quantization = 100)]
        public float CurrentHealth;

        public readonly bool IsDead()
        {
            return CurrentHealth <= 0f;
        }
    }
}
