using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// This system is doing prediction on the player movements that are not affected by the game's physic.
    /// </summary>
    /// <seealso cref="FirstPersonPlayerFixedStepControlSystem"/>
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(WeaponPredictionUpdateGroup))]
    [UpdateBefore(typeof(FirstPersonCharacterVariableUpdateSystem))]
    [UpdateAfter(typeof(BuildCharacterPredictedRotationSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct FirstPersonPlayerVariableStepControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FirstPersonPlayer, FirstPersonPlayerCommands>()
                .Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new FirstPersonPlayerVariableStepControlJob
            {
                CharacterControlLookup = SystemAPI.GetComponentLookup<FirstPersonCharacterControl>(),
                ActiveWeaponlLookup = SystemAPI.GetComponentLookup<ActiveWeapon>(true),
                WeaponControlLookup = SystemAPI.GetComponentLookup<WeaponControl>(),
                CommandDataInterpolationDelayLookup = SystemAPI.GetComponentLookup<CommandDataInterpolationDelay>(),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct FirstPersonPlayerVariableStepControlJob : IJobEntity
        {
            public ComponentLookup<FirstPersonCharacterControl> CharacterControlLookup;

            [ReadOnly]
            public ComponentLookup<ActiveWeapon> ActiveWeaponlLookup;

            public ComponentLookup<WeaponControl> WeaponControlLookup;

            public ComponentLookup<CommandDataInterpolationDelay> CommandDataInterpolationDelayLookup;

            void Execute(Entity firstPersonPlayerEntity, ref FirstPersonPlayerCommands playerCommands,
                ref FirstPersonPlayerNetworkInput playerNetworkInput, in FirstPersonPlayer player)
            {
                // Compute a rotation delta from inputs, compared to last known value
                var lookYawPitchDegreesDelta = FirstPersonInputDeltaUtilities.GetInputDelta(
                    playerCommands.LookYawPitchDegrees,
                    playerNetworkInput.LastProcessedLookYawPitchDegrees);
                playerNetworkInput.LastProcessedLookYawPitchDegrees = playerCommands.LookYawPitchDegrees;

                // Character
                if (CharacterControlLookup.TryGetComponent(player.ControlledCharacter, out var characterControl))
                {
                    // Look
                    characterControl.LookYawPitchDegreesDelta = lookYawPitchDegreesDelta;

                    CharacterControlLookup[player.ControlledCharacter] = characterControl;
                }

                // Weapon
                if (ActiveWeaponlLookup.TryGetComponent(player.ControlledCharacter, out var activeWeapon))
                {
                    if (WeaponControlLookup.TryGetComponent(activeWeapon.Entity, out var weaponControl))
                    {
                        // Shoot
                        weaponControl.ShootPressed = playerCommands.ShootPressed.IsSet;
                        weaponControl.ShootReleased = playerCommands.ShootReleased.IsSet;

                        // Aim
                        weaponControl.AimHeld = playerCommands.AimHeld;

                        WeaponControlLookup[activeWeapon.Entity] = weaponControl;

                        // Each weapon has its own `CommandDataInterpolationDelay`,
                        // so we forward the player ghost's value for it here.
                        CommandDataInterpolationDelayLookup[activeWeapon.Entity] = CommandDataInterpolationDelayLookup[firstPersonPlayerEntity];
                    }
                }
            }
        }
    }

    /// <summary>
    /// This system is doing prediction on the player movements that are affected by the game's physic.
    /// </summary>
    /// <seealso cref="FirstPersonPlayerVariableStepControlSystem"/>
    [UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct FirstPersonPlayerFixedStepControlSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FirstPersonPlayer, FirstPersonPlayerCommands>()
                .Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new FirstPersonPlayerFixedStepControlJob
            {
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                CharacterControlLookup = SystemAPI.GetComponentLookup<FirstPersonCharacterControl>(),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct FirstPersonPlayerFixedStepControlJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            public ComponentLookup<FirstPersonCharacterControl> CharacterControlLookup;

            void Execute(in FirstPersonPlayerCommands playerCommands, in FirstPersonPlayer player,
                in CommandDataInterpolationDelay commandInterpolationDelay)
            {
                // Character
                if (CharacterControlLookup.HasComponent(player.ControlledCharacter))
                {
                    var characterControl = CharacterControlLookup[player.ControlledCharacter];

                    var characterRotation = LocalTransformLookup[player.ControlledCharacter].Rotation;

                    // Move
                    var characterForward = math.mul(characterRotation, math.forward());
                    var characterRight = math.mul(characterRotation, math.right());
                    characterControl.MoveVector = playerCommands.MoveInput.y * characterForward +
                                                  playerCommands.MoveInput.x * characterRight;
                    characterControl.MoveVector = MathUtilities.ClampToMaxLength(characterControl.MoveVector, 1f);

                    // Jump
                    characterControl.Jump = playerCommands.JumpPressed.IsSet;

                    CharacterControlLookup[player.ControlledCharacter] = characterControl;
                }
            }
        }
    }
}
