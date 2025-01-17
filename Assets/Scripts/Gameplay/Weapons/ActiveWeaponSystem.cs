using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(PredictedFixedStepSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct ActiveWeaponSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ActiveWeaponSetupJob setupJob = new ActiveWeaponSetupJob
            {
                Ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged),
                WeaponControlLookup = SystemAPI.GetComponentLookup<WeaponControl>(true),
                FirstPersonCharacterComponentLookup = SystemAPI.GetComponentLookup<FirstPersonCharacterComponent>(true),
                WeaponSimulationShotOriginOverrideLookup = SystemAPI.GetComponentLookup<WeaponShotSimulationOriginOverride>(),
                LinkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(),
                WeaponShotIgnoredEntityLookup = SystemAPI.GetBufferLookup<WeaponShotIgnoredEntity>(),
            };
            state.Dependency = setupJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct ActiveWeaponSetupJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;
            [ReadOnly]
            public ComponentLookup<WeaponControl> WeaponControlLookup;
            [ReadOnly]
            public ComponentLookup<FirstPersonCharacterComponent> FirstPersonCharacterComponentLookup;
            public ComponentLookup<WeaponShotSimulationOriginOverride> WeaponSimulationShotOriginOverrideLookup;
            public BufferLookup<LinkedEntityGroup> LinkedEntityGroupLookup;
            public BufferLookup<WeaponShotIgnoredEntity> WeaponShotIgnoredEntityLookup;

            void Execute(Entity entity, ref ActiveWeapon activeWeapon)
            {
                // Detect changes in active weapon.
                if (activeWeapon.Entity != activeWeapon.PreviousEntity)
                {
                    // Setup new weapon.
                    if (WeaponControlLookup.HasComponent(activeWeapon.Entity))
                    {
                        // Setup for characters.
                        if (FirstPersonCharacterComponentLookup.TryGetComponent(entity, out FirstPersonCharacterComponent character))
                        {
                            // Set the weapon raycast start point to the character View entity.
                            if (WeaponSimulationShotOriginOverrideLookup.TryGetComponent(activeWeapon.Entity, out WeaponShotSimulationOriginOverride shotOriginOverride))
                            {
                                shotOriginOverride.Entity = character.ViewEntity;
                                WeaponSimulationShotOriginOverrideLookup[activeWeapon.Entity] = shotOriginOverride;
                            }

                            // Parent weapon into the character weapon socket.
                            Ecb.AddComponent(activeWeapon.Entity, new Parent { Value = character.WeaponAnimationSocketEntity });

                            // Remember weapon owner.
                            Ecb.SetComponent(activeWeapon.Entity, new WeaponOwner { Entity = entity });

                            // Link the weapon to the character.
                            DynamicBuffer<LinkedEntityGroup> linkedEntityBuffer = LinkedEntityGroupLookup[entity];
                            linkedEntityBuffer.Add(new LinkedEntityGroup { Value = activeWeapon.Entity });

                            // Add character as an ignored shot entity.
                            if (WeaponShotIgnoredEntityLookup.TryGetBuffer(activeWeapon.Entity, out DynamicBuffer<WeaponShotIgnoredEntity> ignoredEntities))
                            {
                                ignoredEntities.Add(new WeaponShotIgnoredEntity { Entity = entity });
                            }
                        }
                    }

                    // TODO: Un-setup previous weapon
                    // if (WeaponControlLookup.HasComponent(activeWeapon.PreviousEntity))
                    // {
                    // Disable weapon update, reset owner, reset data, unparent, etc...
                    // }
                }

                activeWeapon.PreviousEntity = activeWeapon.Entity;
            }
        }
    }
}
