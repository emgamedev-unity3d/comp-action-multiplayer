using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class MachineGunBarrelVisualsAuthoring : MonoBehaviour
    {
        public GameObject BarrelEntity;
        public float SpinVelocity = math.PI * 2f;
        public float SpinVelocityDecay = 3f;

        public class Baker : Baker<MachineGunBarrelVisualsAuthoring>
        {
            public override void Bake(MachineGunBarrelVisualsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MachineGunBarrelVisual
                {
                    BarrelEntity = GetEntity(authoring.BarrelEntity, TransformUsageFlags.Dynamic),
                    SpinVelocity = authoring.SpinVelocity,
                    SpinVelocityDecay = authoring.SpinVelocityDecay,
                });
            }
        }
    }
    
    public struct MachineGunBarrelVisual : IComponentData
    {
        public Entity BarrelEntity;
        public float SpinVelocity;
        public float SpinVelocityDecay;

        public float CurrentSpinVelocity;
    }
}
