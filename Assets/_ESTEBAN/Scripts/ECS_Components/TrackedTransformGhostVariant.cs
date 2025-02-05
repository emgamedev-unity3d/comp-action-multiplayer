using Unity.CharacterController;
using Unity.Mathematics;
using Unity.NetCode;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [GhostComponentVariation(typeof(TrackedTransform))]
    [GhostComponent()]
    public struct TrackedTransformGhostVariant
    {
        [GhostField()]
        public RigidTransform CurrentFixedRateTransform;
    }
}
