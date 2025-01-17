using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class SpectatorControllerAuthoring : MonoBehaviour
    {
        public SpectatorController.Parameters Parameters;

        public class Baker : Baker<SpectatorControllerAuthoring>
        {
            public override void Bake(SpectatorControllerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpectatorController { Params = authoring.Parameters });
            }
        }
    }

    public struct SpectatorController : IComponentData
    {
        [Serializable]
        public struct Parameters
        {
            public float MoveSpeed;
            public float MoveSharpness;
            public float RotationSpeed;
        }

        public Parameters Params;
        public float3 Velocity;
    }
}
