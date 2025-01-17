using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// Reads player inputs and forward them to the <see cref="FirstPersonPlayerCommands"/>
    /// that the server is using to process each character movement.
    /// </summary>
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct FirstPersonPlayerInputsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FirstPersonPlayer, FirstPersonPlayerCommands>()
                .Build());
            state.RequireForUpdate<GameResources>();
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            var defaultActionsMap = GameInput.Actions.Gameplay;

            foreach (var playerCommands in SystemAPI
                         .Query<RefRW<FirstPersonPlayerCommands>>()
                         .WithAll<GhostOwnerIsLocal, FirstPersonPlayer>())
            {
                if (GameSettings.Instance.IsPauseMenuOpen)
                {
                    //If pause menu is open, reset all input values and release Shoot button
                    var currentRotation = playerCommands.ValueRO.LookYawPitchDegrees;
                    var aimHeld = playerCommands.ValueRO.AimHeld;
                    playerCommands.ValueRW = default;
                    playerCommands.ValueRW.LookYawPitchDegrees = currentRotation;
                    playerCommands.ValueRW.ShootReleased.Set();
                    playerCommands.ValueRW.AimHeld = aimHeld;

                    continue;
                }

                // Move
                playerCommands.ValueRW.MoveInput =
                    Vector2.ClampMagnitude(defaultActionsMap.Move.ReadValue<Vector2>(), 1f);

                // Look
                var invertYMultiplier = GameSettings.Instance.InvertYAxis ? new float2(1.0f, -1.0f) : new float2(1.0f, 1.0f);
                if (math.lengthsq(defaultActionsMap.LookConst.ReadValue<Vector2>()) >
                    math.lengthsq(defaultActionsMap.LookDelta.ReadValue<Vector2>()))
                {
                    // Gamepad stick (constant) input.
                    // As the look input handling expects a "delta" rather than a constant value, we multiply stick input value by deltaTime.
                    FirstPersonInputDeltaUtilities.AddInputDelta(ref playerCommands.ValueRW.LookYawPitchDegrees,
                        (float2)defaultActionsMap.LookConst.ReadValue<Vector2>() * deltaTime *
                        GameSettings.Instance.LookSensitivity * invertYMultiplier);
                }
                else
                {
                    // Mouse (delta) input
                    FirstPersonInputDeltaUtilities.AddInputDelta(ref playerCommands.ValueRW.LookYawPitchDegrees,
                        (float2)defaultActionsMap.LookDelta.ReadValue<Vector2>() *
                        GameSettings.Instance.LookSensitivity * invertYMultiplier);
                }

                // Jump
                playerCommands.ValueRW.JumpPressed = default;
                if (defaultActionsMap.Jump.WasPressedThisFrame())
                    playerCommands.ValueRW.JumpPressed.Set();

                // Shoot pressed
                playerCommands.ValueRW.ShootPressed = default;
                if (defaultActionsMap.Shoot.WasPressedThisFrame())
                    playerCommands.ValueRW.ShootPressed.Set();

                //Shoot released
                playerCommands.ValueRW.ShootReleased = default;
                if (defaultActionsMap.Shoot.WasReleasedThisFrame())
                    playerCommands.ValueRW.ShootReleased.Set();

                // Aim
                playerCommands.ValueRW.AimHeld = defaultActionsMap.Aim.IsPressed();
            }
        }
    }
}
