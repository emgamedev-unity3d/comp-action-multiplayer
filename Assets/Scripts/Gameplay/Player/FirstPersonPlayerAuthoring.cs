using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [DisallowMultipleComponent]
    public class FirstPersonPlayerAuthoring : MonoBehaviour
    {
        public GameObject ControlledCharacter;

        public class Baker : Baker<FirstPersonPlayerAuthoring>
        {
            public override void Bake(FirstPersonPlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new FirstPersonPlayer
                {
                    ControlledCharacter = GetEntity(authoring.ControlledCharacter, TransformUsageFlags.Dynamic),
                });
                AddComponent(entity, new FirstPersonPlayerNetworkInput());
                AddComponent<FirstPersonPlayerCommands>(entity);
            }
        }
    }
    
    [GhostComponent]
    public struct FirstPersonPlayer : IComponentData
    {
        [GhostField]
        public FixedString128Bytes Name;

        [GhostField]
        public Entity ControlledCharacter;
    }

    [GhostComponent(SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
    public struct FirstPersonPlayerNetworkInput : IComponentData
    {
        [GhostField]
        public float2 LastProcessedLookYawPitchDegrees;
    }

    public struct FirstPersonPlayerCommands : IInputComponentData
    {
        public float2 MoveInput;
        public float2 LookYawPitchDegrees;
        public InputEvent JumpPressed;
        public InputEvent ShootPressed;
        public InputEvent ShootReleased;
        public bool AimHeld;
    }
}
