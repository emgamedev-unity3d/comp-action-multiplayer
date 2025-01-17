using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [RequireComponent(typeof(BaseWeaponAuthoring))]
    public class PrefabProjectileWeaponAuthoring : MonoBehaviour
    {
        public GameObject ProjectilePrefab;

        class Baker : Baker<PrefabProjectileWeaponAuthoring>
        {
            public override void Bake(PrefabProjectileWeaponAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PrefabWeapon
                {
                    ProjectilePrefab = GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }
}
