using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(WeaponVisualsUpdateGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct CharacterWeaponVisualFeedbackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FirstPersonCharacterControl>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            CharacterWeaponVisualFeedbackJob job = new CharacterWeaponVisualFeedbackJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                WeaponVisualFeedbackLookup = SystemAPI.GetComponentLookup<WeaponVisualFeedback>(true),
                WeaponControlLookup = SystemAPI.GetComponentLookup<WeaponControl>(true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                MainEntityCameraLookup = SystemAPI.GetComponentLookup<MainCamera>(),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct CharacterWeaponVisualFeedbackJob : IJobEntity
        {
            public float DeltaTime;
            public float ElapsedTime;
            [ReadOnly]
            public ComponentLookup<WeaponVisualFeedback> WeaponVisualFeedbackLookup;
            [ReadOnly]
            public ComponentLookup<WeaponControl> WeaponControlLookup;
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            public ComponentLookup<MainCamera> MainEntityCameraLookup;

            void Execute(ref CharacterWeaponVisualFeedback characterWeaponVisualFeedback, in FirstPersonCharacterComponent character, in KinematicCharacterBody characterBody, in ActiveWeapon activeWeapon)
            {
                var isAiming = WeaponControlLookup.TryGetComponent(activeWeapon.Entity, out WeaponControl weaponControl) && weaponControl.AimHeld;
                var characterMaxSpeed = characterBody.IsGrounded ? character.GroundMaxSpeed : character.AirMaxSpeed;

                if (WeaponVisualFeedbackLookup.TryGetComponent(activeWeapon.Entity, out WeaponVisualFeedback weaponFeedback))
                {
                    var characterVelocityRatio = math.length(characterBody.RelativeVelocity) / characterMaxSpeed;

                    // Weapon bob.
                    {
                        float3 targetBobPos = default;
                        if (characterBody.IsGrounded)
                        {
                            var bobSpeedMultiplier = isAiming ? weaponFeedback.WeaponBobAimRatio : 1f;
                            var hBob = math.sin(ElapsedTime * weaponFeedback.WeaponBobFrequency) * weaponFeedback.WeaponBobHAmount * bobSpeedMultiplier * characterVelocityRatio;
                            var vBob = (math.sin(ElapsedTime * weaponFeedback.WeaponBobFrequency * 2f) * 0.5f + 0.5f) * weaponFeedback.WeaponBobVAmount * bobSpeedMultiplier * characterVelocityRatio;
                            targetBobPos = new float3(hBob, vBob, 0f);
                        }

                        characterWeaponVisualFeedback.WeaponLocalPosBob = math.lerp(characterWeaponVisualFeedback.WeaponLocalPosBob, targetBobPos, math.saturate(weaponFeedback.WeaponBobSharpness * DeltaTime));
                    }

                    // Weapon recoil.
                    {
                        // Clamp current recoil.
                        characterWeaponVisualFeedback.CurrentRecoil = math.clamp(characterWeaponVisualFeedback.CurrentRecoil, 0f, weaponFeedback.RecoilMaxDistance);

                        // Go towards recoil.
                        if (characterWeaponVisualFeedback.WeaponLocalPosRecoil.z >= -characterWeaponVisualFeedback.CurrentRecoil * 0.99f)
                        {
                            characterWeaponVisualFeedback.WeaponLocalPosRecoil = math.lerp(characterWeaponVisualFeedback.WeaponLocalPosRecoil, math.forward() * -characterWeaponVisualFeedback.CurrentRecoil, math.saturate(weaponFeedback.RecoilSharpness * DeltaTime));
                        }
                        // Go towards restitution.
                        else
                        {
                            characterWeaponVisualFeedback.WeaponLocalPosRecoil = math.lerp(characterWeaponVisualFeedback.WeaponLocalPosRecoil, float3.zero, math.saturate(weaponFeedback.RecoilRestitutionSharpness * DeltaTime));
                            characterWeaponVisualFeedback.CurrentRecoil = -characterWeaponVisualFeedback.WeaponLocalPosRecoil.z;
                        }
                    }

                    // Final weapon pose.
                    float3 targetWeaponAnimSocketLocalPosition = characterWeaponVisualFeedback.WeaponLocalPosBob + characterWeaponVisualFeedback.WeaponLocalPosRecoil;
                    LocalTransformLookup[character.WeaponAnimationSocketEntity] = LocalTransform.FromPosition(targetWeaponAnimSocketLocalPosition);

                    // Fov modifications.
                    if (MainEntityCameraLookup.TryGetComponent(character.ViewEntity, out MainCamera entityCamera))
                    {
                        // Fov kick.
                        {
                            // Clamp current.
                            characterWeaponVisualFeedback.TargetRecoilFovKick = math.clamp(characterWeaponVisualFeedback.TargetRecoilFovKick, 0f, weaponFeedback.RecoilMaxFovKick);

                            // Fov go towards recoil.
                            if (characterWeaponVisualFeedback.CurrentRecoilFovKick <= characterWeaponVisualFeedback.TargetRecoilFovKick * 0.99f)
                            {
                                characterWeaponVisualFeedback.CurrentRecoilFovKick = math.lerp(characterWeaponVisualFeedback.CurrentRecoilFovKick, characterWeaponVisualFeedback.TargetRecoilFovKick, math.saturate(weaponFeedback.RecoilFovKickSharpness * DeltaTime));
                            }
                            // Fov go towards restitution.
                            else
                            {
                                characterWeaponVisualFeedback.CurrentRecoilFovKick = math.lerp(characterWeaponVisualFeedback.CurrentRecoilFovKick, 0f, math.saturate(weaponFeedback.RecoilFovKickRestitutionSharpness * DeltaTime));
                                characterWeaponVisualFeedback.TargetRecoilFovKick = characterWeaponVisualFeedback.CurrentRecoilFovKick;
                            }
                        }

                        // Aiming.
                        {
                            var targetFov = isAiming ? entityCamera.BaseFov * weaponFeedback.AimFovRatio : entityCamera.BaseFov;
                            entityCamera.CurrentFov = math.lerp(entityCamera.CurrentFov, targetFov + characterWeaponVisualFeedback.CurrentRecoilFovKick, math.saturate(weaponFeedback.AimFovSharpness * DeltaTime));
                        }

                        MainEntityCameraLookup[character.ViewEntity] = entityCamera;
                    }
                }
            }
        }
    }
}
