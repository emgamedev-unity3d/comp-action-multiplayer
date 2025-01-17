using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [RequireComponent(typeof(RaycastProjectileAuthoring))]
    public class BulletShotVisualsAuthoring : MonoBehaviour
    {
        public float Speed = 10f;
        public float StretchFromSpeed = 1f;
        public float MaxStretch = 1f;

        public class Baker : Baker<BulletShotVisualsAuthoring>
        {
            public override void Bake(BulletShotVisualsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.NonUniformScale);
                AddComponent(entity, new BulletShotVisuals
                {
                    Speed = authoring.Speed,
                    StretchFromSpeed = authoring.StretchFromSpeed,
                    MaxStretch = authoring.MaxStretch,
                });
            }
        }
    }
    
    public struct BulletShotVisuals : IComponentData
    {
        public float Speed;
        public float StretchFromSpeed;
        public float MaxStretch;

        public bool IsInitialized;
        public float DistanceTraveled;
    }
}
