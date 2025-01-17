using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectilePredictionUpdateGroup))]
    [BurstCompile]
    public partial struct CharacterDeathServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Health, FirstPersonCharacterComponent>().WithDisabled<DelayedDespawn>().Build());
            state.RequireForUpdate<GameplayMaps>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            CharacterDeathServerJob serverJob = new CharacterDeathServerJob
            {
                Ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW
                    .CreateCommandBuffer(state.WorldUnmanaged),
                RespawnTime = SystemAPI.GetSingleton<GameResources>().RespawnTime,
                GameplayMaps = SystemAPI.GetSingletonBuffer<GameplayMaps>(),
                DelayedDespawnLookup = SystemAPI.GetComponentLookup<DelayedDespawn>(),
            };
            state.Dependency = serverJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        [WithDisabled(typeof(DelayedDespawn))]
        public partial struct CharacterDeathServerJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;
            public float RespawnTime;

            [ReadOnly]
            public DynamicBuffer<GameplayMaps> GameplayMaps;

            public ComponentLookup<DelayedDespawn> DelayedDespawnLookup;

            void Execute(Entity entity, in FirstPersonCharacterComponent character, in Health health,
                in GhostOwner ghostOwner)
            {
                if (health.IsDead())
                {
                    var map = GameplayMaps[ghostOwner.NetworkId];
                    if (map.ConnectionEntity != Entity.Null)
                    {
                        // Set up the server to perform local respawn for this client
                        Entity spawnCharacterRequestEntity = Ecb.CreateEntity();
                        Ecb.AddComponent(spawnCharacterRequestEntity,
                            new SpawnCharacter { ClientEntity = map.ConnectionEntity, Delay = RespawnTime });
                    }

                    // Activate delayed despawn
                    DelayedDespawnLookup.SetComponentEnabled(entity, true);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct CharacterDeathClientSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FirstPersonCharacterComponent, DelayedDespawn>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            CharacterDeathClientJob job = new CharacterDeathClientJob
            {
                Ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW
                    .CreateCommandBuffer(state.WorldUnmanaged),
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(FirstPersonCharacterComponent), typeof(DelayedDespawn))]
        public partial struct CharacterDeathClientJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;
            [ReadOnly]
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;

            void Execute(ref FirstPersonCharacterComponent character, ref VfxAttributeSettings vfxAttributeSettings)
            {
                if (character.HasProcessedDeath == 0)
                {
                    if (LocalToWorldLookup.TryGetComponent(character.DeathVfxSpawnPoint, out LocalToWorld deathVfxLtW))
                    {
                        Entity spawnVfxDeathRequestEntity = Ecb.CreateEntity();
                        Ecb.AddComponent(spawnVfxDeathRequestEntity,
                            new VfxHitRequest()
                        {
                            VfxHitType = VfxType.Death,
                            LowCount = vfxAttributeSettings.LowVfxSpawnCount,
                            MidCount = vfxAttributeSettings.MidVfxSpawnCount,
                            HighCount = vfxAttributeSettings.HighVfxSpawnCount,
                            Position = deathVfxLtW.Position,
                            HitNormal = new float3(0, 1, 0),
                        });
                    }

                    character.HasProcessedDeath = 1;
                }
            }
        }
    }
}
