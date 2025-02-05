using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [GhostComponentVariation(typeof(KinematicCharacterBody))]
    [GhostComponent()]
    public struct KinematicCharacterBody_DefaultVariant
    {
        // These two fields represent the basic synchronized state data that all networked characters will need.
        [GhostField()]
        public float3 RelativeVelocity;

        [GhostField()]
        public bool IsGrounded;

        // The following fields are only needed for characters that need to support parent entities (stand on moving platforms).
        // You can safely omit these from ghost sync if your game does not make use of character parent entities (any entities that have a TrackedTransform component).
        [GhostField()]
        public Entity ParentEntity;

        [GhostField()]
        public float3 ParentLocalAnchorPoint;

        [GhostField()]
        public float3 ParentVelocity;
    }
}
