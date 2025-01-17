using System.Collections.Generic;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [CreateBefore(typeof(TransformDefaultVariantSystem))]
    public partial class DefaultVariantSystem : DefaultVariantSystemBase
    {
        protected override void RegisterDefaultVariants(Dictionary<ComponentType, Rule> defaultVariants)
        {
            defaultVariants.Add(typeof(LocalTransform), Rule.ForAll(typeof(DontSerializeVariant)));
            defaultVariants.Add(typeof(KinematicCharacterBody), Rule.ForAll(typeof(KinematicCharacterBodyGhostVariant)));
            defaultVariants.Add(typeof(CharacterInterpolation), Rule.ForAll(typeof(CharacterInterpolationGhostVariant)));
        }
    }

    [GhostComponentVariation(typeof(KinematicCharacterBody))]
    [GhostComponent(SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
    public struct KinematicCharacterBodyGhostVariant
    {
        [GhostField(Quantization = 1000)]
        public float3 RelativeVelocity;
        [GhostField]
        public bool IsGrounded;
    }

    // Character interpolation must be Client-only, it would otherwise prevent proper LocalToWorld updates on server
    [GhostComponentVariation(typeof(CharacterInterpolation))]
    [GhostComponent(PrefabType = GhostPrefabType.PredictedClient)]
    public struct CharacterInterpolationGhostVariant
    {
    }
}
