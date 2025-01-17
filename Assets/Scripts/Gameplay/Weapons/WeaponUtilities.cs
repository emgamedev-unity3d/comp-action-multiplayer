using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public static class WeaponUtilities
    {
        public static bool GetClosestValidWeaponRaycastHit(
            in NativeList<RaycastHit> hits,
            in DynamicBuffer<WeaponShotIgnoredEntity> ignoredEntities,
            out RaycastHit closestValidHit)
        {
            closestValidHit = default;
            closestValidHit.Fraction = float.MaxValue;
            for (var j = 0; j < hits.Length; j++)
            {
                RaycastHit tmpHit = hits[j];

                // Check closest so far.
                if (tmpHit.Fraction < closestValidHit.Fraction)
                {
                    // Check collidable.
                    if (PhysicsUtilities.IsCollidable(tmpHit.Material))
                    {
                        // Check entity ignore.
                        var entityValid = true;
                        for (var k = 0; k < ignoredEntities.Length; k++)
                        {
                            if (tmpHit.Entity == ignoredEntities[k].Entity)
                            {
                                entityValid = false;
                                break;
                            }
                        }

                        // Final hit.
                        if (entityValid)
                        {
                            closestValidHit = tmpHit;
                        }
                    }
                }
            }

            return closestValidHit.Entity != Entity.Null;
        }

        public static RigidTransform GetShotSimulationOrigin(
            Entity shotOriginEntity,
            in WeaponShotSimulationOriginOverride shotSimulationOriginOverride,
            ref ComponentLookup<LocalTransform> localTransformLookup,
            ref ComponentLookup<Parent> parentLookup,
            ref ComponentLookup<PostTransformMatrix> postTransformMatrixLookup)
        {
            // In an FPS game, it is often desirable for the weapon shot raycast to start from the camera (screen center)
            // rather than from the actual barrel of the weapon mesh as it will match the crosshair at the center of the screen.
            // The shot "Simulation" represents the camera point for the raycast, while the shot "Visual" represents the point where the shot mesh is spawned.
            Entity shotSimulationOriginEntity = localTransformLookup.HasComponent(shotSimulationOriginOverride.Entity) ? shotSimulationOriginOverride.Entity : shotOriginEntity;
            TransformHelpers.ComputeWorldTransformMatrix(shotSimulationOriginEntity, out float4x4 shotSimulationOriginTransform, ref localTransformLookup, ref parentLookup, ref postTransformMatrixLookup);

            return new RigidTransform(shotSimulationOriginTransform.Rotation(), shotSimulationOriginTransform.Translation());
        }

        public static quaternion CalculateSpreadRotation(quaternion shotSimulationRotation, float spreadRadians, ref Random random)
        {
            quaternion shotSpreadRotation = quaternion.identity;
            if (spreadRadians > 0f)
            {
                shotSpreadRotation = math.slerp(random.NextQuaternionRotation(), quaternion.identity, (math.PI - math.clamp(spreadRadians, 0f, math.PI)) / math.PI);
            }
            return math.mul(shotSpreadRotation, shotSimulationRotation);
        }

        // Shooting update for logic that is common to both simulation and presentation.
        public static void CalculateIndividualRaycastShot(
            float3 shotSimulationOrigin,
            float3 shotSimulationDirection,
            float range,
            in CollisionWorld collisionWorld,
            ref NativeList<RaycastHit> hits,
            in DynamicBuffer<WeaponShotIgnoredEntity> ignoredEntities,
            out bool hitFound,
            out float3 hitNormal,
            out Entity hitEntity,
            out float3 shotEndPoint)
        {
            // Hit detection.
            hits.Clear();
            RaycastInput rayInput = new RaycastInput
            {
                Start = shotSimulationOrigin,
                End = shotSimulationOrigin + shotSimulationDirection * range,
                Filter = CollisionFilter.Default, // Todo; customizable
            };
            collisionWorld.CastRay(rayInput, ref hits);
            hitFound = GetClosestValidWeaponRaycastHit(in hits, in ignoredEntities, out RaycastHit closestValidHit);

            var hitDistance = range;
            hitNormal = default;
            hitEntity = default;
            if (hitFound)
            {
                hitDistance = closestValidHit.Fraction * range;
                hitNormal = closestValidHit.SurfaceNormal;
                hitEntity = closestValidHit.Entity;
            }

            shotEndPoint = shotSimulationOrigin + shotSimulationDirection * hitDistance;
        }
    }
}
