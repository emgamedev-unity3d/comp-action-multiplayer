using Unity.Mathematics;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// This utility class contains methods to smooth inputs delta when the previous and new inputs are too far apart.
    /// </summary>
    public static class FirstPersonInputDeltaUtilities
    {
        const float k_DefaultWrapAroundValue = 1000f;

        public static void AddInputDelta(ref float2 input, float2 addedDelta)
        {
            input = math.fmod(input + addedDelta, k_DefaultWrapAroundValue);
        }

        public static float2 GetInputDelta(float2 currentValue, float2 previousValue)
        {
            float2 delta = currentValue - previousValue;

            // When delta is very large, consider that the input has wrapped around
            if (math.abs(delta.x) > (k_DefaultWrapAroundValue * 0.5f))
            {
                delta.x += (math.sign(previousValue.x - currentValue.x) * k_DefaultWrapAroundValue);
            }

            if (math.abs(delta.y) > (k_DefaultWrapAroundValue * 0.5f))
            {
                delta.y += (math.sign(previousValue.y - currentValue.y) * k_DefaultWrapAroundValue);
            }

            return delta;
        }
    }
}
