using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class LaserShotVisualsAuthoring : MonoBehaviour
    {
        public float Lifetime = 1f;
        public float Width = 1f;

        class Baker : Baker<LaserShotVisualsAuthoring>
        {
            public override void Bake(LaserShotVisualsAuthoring authoring)
            {
                Entity selfEntity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.NonUniformScale);

                AddComponent(selfEntity, new LaserShotVisuals
                {
                    LifeTime = authoring.Lifetime,
                    Width = authoring.Width,
                });
                AddComponent(selfEntity, new PostTransformMatrix { Value = float4x4.Scale(1f) });
            }
        }
    }

    public struct LaserShotVisuals : IComponentData
    {
        public float LifeTime;
        public float Width;

        public float StartTime;
        public float3 StartingScale;
        public bool HasInitialized;
    }
}
