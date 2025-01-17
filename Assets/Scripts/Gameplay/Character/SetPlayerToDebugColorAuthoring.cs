using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// Denotes that a ghost will be set to the debug color specified in <see cref="NetworkIdDebugColorUtility"/>.
    /// </summary>
    public struct SetPlayerToDebugColor : IComponentData
    {
    }

    /// <summary>
    /// NetcodeSamples authoring that sets a ghost to the debug color specified in <see cref="NetworkIdDebugColorUtility"/>.
    /// </summary>
    [UnityEngine.DisallowMultipleComponent]
    public class SetPlayerToDebugColorAuthoring : UnityEngine.MonoBehaviour
    {
        class SetPlayerToDebugColorBaker : Baker<SetPlayerToDebugColorAuthoring>
        {
            public override void Bake(SetPlayerToDebugColorAuthoring authoring)
            {
                SetPlayerToDebugColor component = default(SetPlayerToDebugColor);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
                AddComponent(entity, new URPMaterialPropertyBaseColor() {Value = new float4(1, 0, 0, 1)});
            }
        }
    }

    /// <summary>
    /// Every <see cref="NetworkId"/> has its own unique Debug color. This sample system sets it on all root ghosts (and
    /// child entities of said ghosts) containing a <see cref="SetPlayerToDebugColor"/> component (see its authoring component).
    /// </summary>
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    public partial struct SetPlayerToDebugColorSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // The LinkedEntityGroup contains the root ghost, as well as all children.
            var hasDebugColorLookupRO = SystemAPI.GetComponentLookup<SetPlayerToDebugColor>(true);
            var urpMaterialPropertyBaseColorLookupRW = SystemAPI.GetComponentLookup<URPMaterialPropertyBaseColor>(false);
            foreach (var (ghostLinkedEntityGroup, ghostOwner) in
                     SystemAPI.Query<DynamicBuffer<LinkedEntityGroup>, RefRO<GhostOwner>>()
                         .WithChangeFilter<GhostOwner>().WithAll<SetPlayerToDebugColor>())
            {
                foreach (var linkedEntity in ghostLinkedEntityGroup)
                {
                    if (!hasDebugColorLookupRO.HasComponent(linkedEntity.Value)) continue;
                    var colorRefRW = urpMaterialPropertyBaseColorLookupRW.GetRefRWOptional(linkedEntity.Value);
                    //UnityEngine.Debug.Log($"[{state.WorldUnmanaged.Name}] SetPlayerToDebugColor on {linkedEntity.Value.ToFixedString()} (owner:{ghostOwner.ValueRO.NetworkId}) - colorRefRW.IsValid:{colorRefRW.IsValid}.");
                    if (!colorRefRW.IsValid) continue;
                    colorRefRW.ValueRW.Value = NetworkIdDebugColorUtility.Get(ghostOwner.ValueRO.NetworkId);
                }
            }
        }
    }
}
