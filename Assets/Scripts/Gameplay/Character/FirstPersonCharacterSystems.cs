using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(PredictedFixedStepSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct BuildCharacterPredictedRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LocalTransform, FirstPersonCharacterComponent>()
                .Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new BuildCharacterPredictedRotationJob();
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct BuildCharacterPredictedRotationJob : IJobEntity
        {
            void Execute(ref LocalTransform localTransform, in FirstPersonCharacterComponent characterComponent)
            {
                FirstPersonCharacterUtilities.ComputeRotationFromYAngleAndUp(characterComponent.CharacterYDegrees,
                    math.up(), out var tmpRotation);
                localTransform.Rotation = tmpRotation;
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct BuildCharacterInterpolatedRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LocalTransform, FirstPersonCharacterComponent>()
                .Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new BuildCharacterInterpolatedRotationJob
            {
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithNone(typeof(GhostOwnerIsLocal))]
        public partial struct BuildCharacterInterpolatedRotationJob : IJobEntity
        {
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            void Execute(Entity entity, in FirstPersonCharacterComponent characterComponent)
            {
                if (LocalTransformLookup.TryGetComponent(entity, out var characterLocalTransform))
                {
                    FirstPersonCharacterUtilities.ComputeRotationFromYAngleAndUp(characterComponent.CharacterYDegrees,
                        math.up(), out var tmpRotation);
                    characterLocalTransform.Rotation = tmpRotation;
                    LocalTransformLookup[entity] = characterLocalTransform;

                    if (LocalTransformLookup.TryGetComponent(characterComponent.ViewEntity, out var viewLocalTransform))
                    {
                        viewLocalTransform.Rotation =
                            FirstPersonCharacterUtilities.CalculateLocalViewRotation(
                                characterComponent.ViewPitchDegrees, 0f);
                        LocalTransformLookup[characterComponent.ViewEntity] = viewLocalTransform;
                    }
                }
            }
        }
    }

    [UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct FirstPersonCharacterPhysicsUpdateSystem : ISystem
    {
        EntityQuery m_CharacterQuery;
        FirstPersonCharacterUpdateContext m_Context;
        KinematicCharacterUpdateContext m_BaseContext;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_CharacterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
                .WithAll<
                    FirstPersonCharacterComponent,
                    FirstPersonCharacterControl>()
                .Build(ref state);

            m_Context = new FirstPersonCharacterUpdateContext();
            m_Context.OnSystemCreate(ref state);
            m_BaseContext = new KinematicCharacterUpdateContext();
            m_BaseContext.OnSystemCreate(ref state);

            state.RequireForUpdate(m_CharacterQuery);
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_Context.OnSystemUpdate(ref state);
            m_BaseContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            var job = new FirstPersonCharacterPhysicsUpdateJob
            {
                Context = m_Context,
                BaseContext = m_BaseContext,
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        [WithDisabled(typeof(DelayedDespawn))]
        public partial struct FirstPersonCharacterPhysicsUpdateJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public FirstPersonCharacterUpdateContext Context;
            public KinematicCharacterUpdateContext BaseContext;

            void Execute(FirstPersonCharacterAspect characterAspect)
            {
                characterAspect.PhysicsUpdate(ref Context, ref BaseContext);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                BaseContext.EnsureCreationOfTmpCollections();
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(FirstPersonPlayerVariableStepControlSystem))]
    [UpdateAfter(typeof(BuildCharacterPredictedRotationSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct FirstPersonCharacterVariableUpdateSystem : ISystem
    {
        EntityQuery m_CharacterQuery;
        FirstPersonCharacterUpdateContext m_Context;
        KinematicCharacterUpdateContext m_BaseContext;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_CharacterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
                .WithAll<
                    FirstPersonCharacterComponent,
                    FirstPersonCharacterControl>()
                .Build(ref state);

            m_Context = new FirstPersonCharacterUpdateContext();
            m_Context.OnSystemCreate(ref state);
            m_BaseContext = new KinematicCharacterUpdateContext();
            m_BaseContext.OnSystemCreate(ref state);

            state.RequireForUpdate(m_CharacterQuery);
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_Context.OnSystemUpdate(ref state);
            m_BaseContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            var variableUpdateJob = new FirstPersonCharacterVariableUpdateJob
            {
                Context = m_Context,
                BaseContext = m_BaseContext,
            };
            state.Dependency = variableUpdateJob.Schedule(state.Dependency);

            var viewJob = new FirstPersonCharacterViewJob
            {
                FirstPersonCharacterLookup = SystemAPI.GetComponentLookup<FirstPersonCharacterComponent>(true),
            };
            state.Dependency = viewJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        [WithDisabled(typeof(DelayedDespawn))]
        public partial struct FirstPersonCharacterVariableUpdateJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public FirstPersonCharacterUpdateContext Context;
            public KinematicCharacterUpdateContext BaseContext;

            void Execute(FirstPersonCharacterAspect characterAspect)
            {
                characterAspect.VariableUpdate(ref Context, ref BaseContext);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                BaseContext.EnsureCreationOfTmpCollections();
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct FirstPersonCharacterViewJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<FirstPersonCharacterComponent> FirstPersonCharacterLookup;

            void Execute(ref LocalTransform localTransform, in FirstPersonCharacterView characterView)
            {
                if (FirstPersonCharacterLookup.TryGetComponent(characterView.CharacterEntity, out var character))
                    localTransform.Rotation = character.ViewLocalRotation;
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct FirstPersonCharacterPresentationOnlySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var viewJob = new FirstPersonCharacterViewRollJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                FirstPersonCharacterViewLookup = SystemAPI.GetComponentLookup<FirstPersonCharacterView>(true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
            };
            state.Dependency = viewJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        [WithDisabled(typeof(DelayedDespawn))]
        public partial struct FirstPersonCharacterViewRollJob : IJobEntity
        {
            public float DeltaTime;

            [ReadOnly]
            public ComponentLookup<FirstPersonCharacterView> FirstPersonCharacterViewLookup;

            public ComponentLookup<LocalTransform> LocalTransformLookup;

            void Execute(Entity entity, ref FirstPersonCharacterComponent characterComponent,
                in KinematicCharacterBody characterBody)
            {
                if (LocalTransformLookup.TryGetComponent(entity, out var characterTransform) &&
                    LocalTransformLookup.TryGetComponent(characterComponent.ViewEntity, out var viewTransform) &&
                    FirstPersonCharacterViewLookup.HasComponent(characterComponent.ViewEntity))
                {
                    // View roll angles
                    {
                        var characterRight = MathUtilities.GetRightFromRotation(characterTransform.Rotation);
                        var characterMaxSpeed = characterBody.IsGrounded
                            ? characterComponent.GroundMaxSpeed
                            : characterComponent.AirMaxSpeed;
                        var characterLateralVelocity = math.projectsafe(characterBody.RelativeVelocity, characterRight);
                        var characterLateralVelocityRatio =
                            math.clamp(math.length(characterLateralVelocity) / characterMaxSpeed, 0f, 1f);
                        var velocityIsRight = math.dot(characterBody.RelativeVelocity, characterRight) > 0f;
                        var targetTiltAngle = math.lerp(0f, characterComponent.ViewRollAmount,
                            characterLateralVelocityRatio);
                        targetTiltAngle = velocityIsRight ? -targetTiltAngle : targetTiltAngle;
                        characterComponent.ViewRollDegrees = math.lerp(characterComponent.ViewRollDegrees,
                            targetTiltAngle,
                            MathUtilities.GetSharpnessInterpolant(characterComponent.ViewRollSharpness, DeltaTime));
                    }

                    // Calculate view local rotation
                    characterComponent.ViewLocalRotation =
                        FirstPersonCharacterUtilities.CalculateLocalViewRotation(characterComponent.ViewPitchDegrees,
                            characterComponent.ViewRollDegrees);

                    // Set view local transform
                    viewTransform.Rotation = characterComponent.ViewLocalRotation;
                    LocalTransformLookup[characterComponent.ViewEntity] = viewTransform;
                }
            }
        }
    }
}
