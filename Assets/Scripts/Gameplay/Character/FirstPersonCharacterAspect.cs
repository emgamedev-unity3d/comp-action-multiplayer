using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public struct FirstPersonCharacterUpdateContext
    {
        // You can add additional global data for your character updates here, such as ComponentLookups, Singletons, NativeCollections, etc...
        // This data will be accessible in your character updates and all of your character "callbacks".
        [ReadOnly]
        public ComponentLookup<WeaponVisualFeedback> WeaponVisualFeedbackLookup;

        [ReadOnly]
        public ComponentLookup<WeaponControl> WeaponControlLookup;

        public void OnSystemCreate(ref SystemState state)
        {
            WeaponVisualFeedbackLookup = state.GetComponentLookup<WeaponVisualFeedback>(true);
            WeaponControlLookup = state.GetComponentLookup<WeaponControl>(true);
        }

        public void OnSystemUpdate(ref SystemState state)
        {
            WeaponVisualFeedbackLookup.Update(ref state);
            WeaponControlLookup.Update(ref state);
        }
    }

    public readonly partial struct FirstPersonCharacterAspect : IAspect,
        IKinematicCharacterProcessor<FirstPersonCharacterUpdateContext>
    {
        readonly KinematicCharacterAspect m_CharacterAspect;
        readonly RefRW<FirstPersonCharacterComponent> m_CharacterComponent;
        readonly RefRW<FirstPersonCharacterControl> m_CharacterControl;
        readonly RefRW<ActiveWeapon> m_ActiveWeapon;

        public void PhysicsUpdate(ref FirstPersonCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext)
        {
            ref var characterComponent = ref m_CharacterComponent.ValueRW;
            ref var characterBody = ref m_CharacterAspect.CharacterBody.ValueRW;
            ref var characterPosition = ref m_CharacterAspect.LocalTransform.ValueRW.Position;

            // First phase of default character update.
            m_CharacterAspect.Update_Initialize(in this, ref context, ref baseContext, ref characterBody,
                baseContext.Time.DeltaTime);
            m_CharacterAspect.Update_ParentMovement(in this, ref context, ref baseContext, ref characterBody,
                ref characterPosition, characterBody.WasGroundedBeforeCharacterUpdate);
            m_CharacterAspect.Update_Grounding(in this, ref context, ref baseContext, ref characterBody,
                ref characterPosition);

            // Update desired character velocity after grounding was detected and before doing additional processing that depends on velocity.
            HandleVelocityControl(ref context, ref baseContext);

            // Second phase of default character update.
            m_CharacterAspect.Update_PreventGroundingFromFutureSlopeChange(in this, ref context, ref baseContext,
                ref characterBody, in characterComponent.StepAndSlopeHandling);
            m_CharacterAspect.Update_GroundPushing(in this, ref context, ref baseContext, characterComponent.Gravity);
            m_CharacterAspect.Update_MovementAndDecollisions(in this, ref context, ref baseContext, ref characterBody,
                ref characterPosition);
            m_CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterBody);
            m_CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterBody);
            m_CharacterAspect.Update_ProcessStatefulCharacterHits();
        }

        void HandleVelocityControl(ref FirstPersonCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext)
        {
            var deltaTime = baseContext.Time.DeltaTime;
            ref var characterBody = ref m_CharacterAspect.CharacterBody.ValueRW;
            ref var characterComponent = ref m_CharacterComponent.ValueRW;
            ref var characterControl = ref m_CharacterControl.ValueRW;

            // Rotate move input and velocity to take into account parent rotation
            if (characterBody.ParentEntity != Entity.Null)
            {
                characterControl.MoveVector =
                    math.rotate(characterBody.RotationFromParent, characterControl.MoveVector);
                characterBody.RelativeVelocity =
                    math.rotate(characterBody.RotationFromParent, characterBody.RelativeVelocity);
            }

            if (characterBody.IsGrounded)
            {
                // Move on ground
                var targetVelocity = characterControl.MoveVector * characterComponent.GroundMaxSpeed;
                CharacterControlUtilities.StandardGroundMove_Interpolated(ref characterBody.RelativeVelocity,
                    targetVelocity, characterComponent.GroundedMovementSharpness, deltaTime, characterBody.GroundingUp,
                    characterBody.GroundHit.Normal);

                // Jump
                if (characterControl.Jump)
                    CharacterControlUtilities.StandardJump(ref characterBody,
                        characterBody.GroundingUp * characterComponent.JumpSpeed, true, characterBody.GroundingUp);
            }
            else
            {
                // Move in air
                var airAcceleration = characterControl.MoveVector * characterComponent.AirAcceleration;
                if (math.lengthsq(airAcceleration) > 0f)
                {
                    var tmpVelocity = characterBody.RelativeVelocity;
                    CharacterControlUtilities.StandardAirMove(ref characterBody.RelativeVelocity, airAcceleration,
                        characterComponent.AirMaxSpeed, characterBody.GroundingUp, deltaTime, false);

                    // Cancel air acceleration from input if we hit a non-grounded surface (prevents air-climbing slopes at high air accelerations)
                    if (characterComponent.PreventAirAccelerationAgainstUngroundedHits &&
                        m_CharacterAspect.MovementWouldHitNonGroundedObstruction(in this, ref context, ref baseContext,
                            characterBody.RelativeVelocity * deltaTime, out _))
                        characterBody.RelativeVelocity = tmpVelocity;
                }

                // Gravity
                CharacterControlUtilities.AccelerateVelocity(ref characterBody.RelativeVelocity,
                    characterComponent.Gravity, deltaTime);

                // Drag
                CharacterControlUtilities.ApplyDragToVelocity(ref characterBody.RelativeVelocity, deltaTime,
                    characterComponent.AirDrag);
            }
        }

        public void VariableUpdate(ref FirstPersonCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext)
        {
            ref var characterComponent = ref m_CharacterComponent.ValueRW;
            var characterControl = m_CharacterControl.ValueRO;
            ref var characterRotation = ref m_CharacterAspect.LocalTransform.ValueRW.Rotation;
            var activeWeapon = m_ActiveWeapon.ValueRO;

            // Handle aiming look sensitivity
            if (context.WeaponControlLookup.TryGetComponent(activeWeapon.Entity, out var weaponControl))
                if (weaponControl.AimHeld)
                    if (context.WeaponVisualFeedbackLookup.TryGetComponent(activeWeapon.Entity,
                            out var weaponFeedback))

                        characterControl.LookYawPitchDegreesDelta *=
                            weaponFeedback.LookSensitivityMultiplierWhileAiming;

            // Compute character & view rotations from rotation input
            FirstPersonCharacterUtilities.ComputeFinalRotationsFromRotationDelta(
                ref characterComponent.ViewPitchDegrees,
                ref characterComponent.CharacterYDegrees,
                math.up(),
                characterControl.LookYawPitchDegreesDelta,
                0, // don't include roll angle in simulation
                characterComponent.MinViewAngle,
                characterComponent.MaxViewAngle,
                out characterRotation,
                out characterComponent.ViewLocalRotation);
        }

        #region Character Processor Callbacks

        public void UpdateGroundingUp(
            ref FirstPersonCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext)
        {
            ref var characterBody = ref m_CharacterAspect.CharacterBody.ValueRW;

            m_CharacterAspect.Default_UpdateGroundingUp(ref characterBody);
        }

        public bool CanCollideWithHit(
            ref FirstPersonCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            in BasicHit hit)
        {
            return PhysicsUtilities.IsCollidable(hit.Material);
        }

        public bool IsGroundedOnHit(
            ref FirstPersonCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            in BasicHit hit,
            int groundingEvaluationType)
        {
            var characterComponent = m_CharacterComponent.ValueRO;

            return m_CharacterAspect.Default_IsGroundedOnHit(
                in this,
                ref context,
                ref baseContext,
                in hit,
                in characterComponent.StepAndSlopeHandling,
                groundingEvaluationType);
        }

        public void OnMovementHit(
            ref FirstPersonCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            ref KinematicCharacterHit hit,
            ref float3 remainingMovementDirection,
            ref float remainingMovementLength,
            float3 originalVelocityDirection,
            float hitDistance)
        {
            ref var characterBody = ref m_CharacterAspect.CharacterBody.ValueRW;
            ref var characterPosition = ref m_CharacterAspect.LocalTransform.ValueRW.Position;
            var characterComponent = m_CharacterComponent.ValueRO;

            m_CharacterAspect.Default_OnMovementHit(
                in this,
                ref context,
                ref baseContext,
                ref characterBody,
                ref characterPosition,
                ref hit,
                ref remainingMovementDirection,
                ref remainingMovementLength,
                originalVelocityDirection,
                hitDistance,
                characterComponent.StepAndSlopeHandling.StepHandling,
                characterComponent.StepAndSlopeHandling.MaxStepHeight,
                characterComponent.StepAndSlopeHandling.CharacterWidthForStepGroundingCheck);
        }

        public void OverrideDynamicHitMasses(
            ref FirstPersonCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            ref PhysicsMass characterMass,
            ref PhysicsMass otherMass,
            BasicHit hit)
        {
        }

        public void ProjectVelocityOnHits(
            ref FirstPersonCharacterUpdateContext context,
            ref KinematicCharacterUpdateContext baseContext,
            ref float3 velocity,
            ref bool characterIsGrounded,
            ref BasicHit characterGroundHit,
            in DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHits,
            float3 originalVelocityDirection)
        {
            var characterComponent = m_CharacterComponent.ValueRO;

            m_CharacterAspect.Default_ProjectVelocityOnHits(
                ref velocity,
                ref characterIsGrounded,
                ref characterGroundHit,
                in velocityProjectionHits,
                originalVelocityDirection,
                characterComponent.StepAndSlopeHandling.ConstrainVelocityToGroundPlane);
        }

        #endregion
    }
}
