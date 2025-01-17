using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public static class MiscUtilities
    {
        public static void SetShadowModeInHierarchy(EntityManager entityManager, EntityCommandBuffer ecb, Entity onEntity, ref BufferLookup<Child> childBufferFromEntity, ShadowCastingMode mode)
        {
            if (entityManager.HasComponent<RenderFilterSettings>(onEntity))
            {
                RenderFilterSettings renderFilterSettings = entityManager.GetSharedComponent<RenderFilterSettings>(onEntity);
                renderFilterSettings.ShadowCastingMode = mode;
                ecb.SetSharedComponent(onEntity, renderFilterSettings);
            }

            if (childBufferFromEntity.HasBuffer(onEntity))
            {
                DynamicBuffer<Child> childBuffer = childBufferFromEntity[onEntity];
                for (var i = 0; i < childBuffer.Length; i++)
                {
                    SetShadowModeInHierarchy(entityManager, ecb, childBuffer[i].Value, ref childBufferFromEntity, mode);
                }
            }
        }

        public static void DisableRenderingInHierarchy(EntityCommandBuffer ecb, Entity onEntity, ref BufferLookup<Child> childBufferFromEntity)
        {
            ecb.RemoveComponent<MaterialMeshInfo>(onEntity);

            if (childBufferFromEntity.HasBuffer(onEntity))
            {
                DynamicBuffer<Child> childBuffer = childBufferFromEntity[onEntity];
                for (var i = 0; i < childBuffer.Length; i++)
                {
                    DisableRenderingInHierarchy(ecb, childBuffer[i].Value, ref childBufferFromEntity);
                }
            }
        }
    }
}
