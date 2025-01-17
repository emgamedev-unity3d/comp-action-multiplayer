using Unity.CharacterController;
using Unity.Mathematics;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public static class FirstPersonCharacterUtilities
    {
        public static float ComputeCharacterYAngleFromDirection(float3 forward)
        {
            float direction = math.dot(forward, math.right()) >= 0 ? 1 : -1;
            return math.degrees(math.acos(math.dot(forward, math.forward()))) * direction;
        }

        public static void ComputeFinalRotationsFromRotationDelta(
            ref float viewPitchDegrees,
            ref float characterRotationYDegrees,
            float3 characterTransformUp,
            float2 yawPitchDeltaDegrees,
            float viewRollDegrees,
            float minPitchDegrees,
            float maxPitchDegrees,
            out quaternion characterRotation,
            out quaternion viewLocalRotation)
        {
            // Yaw
            characterRotationYDegrees += yawPitchDeltaDegrees.x;
            ComputeRotationFromYAngleAndUp(characterRotationYDegrees, characterTransformUp, out characterRotation);

            // Pitch
            viewPitchDegrees += yawPitchDeltaDegrees.y;
            viewPitchDegrees = math.clamp(viewPitchDegrees, minPitchDegrees, maxPitchDegrees);

            viewLocalRotation = CalculateLocalViewRotation(viewPitchDegrees, viewRollDegrees);
        }

        public static void ComputeRotationFromYAngleAndUp(
            float characterRotationYDegrees,
            float3 characterTransformUp,
            out quaternion characterRotation)
        {
            characterRotation = math.mul(MathUtilities.CreateRotationWithUpPriority(characterTransformUp, math.forward()), quaternion.Euler(0f, math.radians(characterRotationYDegrees), 0f));
        }

        public static quaternion CalculateLocalViewRotation(float viewPitchDegrees, float viewRollDegrees)
        {
            // Pitch
            quaternion viewLocalRotation = quaternion.AxisAngle(-math.right(), math.radians(viewPitchDegrees));

            // Roll
            viewLocalRotation = math.mul(viewLocalRotation, quaternion.AxisAngle(math.forward(), math.radians(viewRollDegrees)));

            return viewLocalRotation;
        }
    }
}
