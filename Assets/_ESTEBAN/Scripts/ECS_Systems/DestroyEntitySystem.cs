using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    public partial struct DestroyEntitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();

            if (!networkTime.IsFirstTimeFullyPredictingTick)
                return;

            var ecbSingleton =
                SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

            var entityCommandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, entity) in
                SystemAPI.Query<RefRW<LocalTransform>>()
                    .WithAll<DestroyEntityComponent, Simulate>()
                    .WithEntityAccess())
            {
                // since netcode for entities is server-authoritative, the server owns most or all
                // of the entities
                if (state.World.IsServer())
                {
                    entityCommandBuffer.DestroyEntity(entity);
                }
                else // running from client
                {
                    transform.ValueRW.Position = new float3(0f, -2000f, 0f);
                    //^ clear the entities away from player view, on client so that they're
                    //    destroyed later in the server
                }
            }
        }
    }
}
