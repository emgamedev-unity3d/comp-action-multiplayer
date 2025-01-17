using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class SpawnPointAuthoring : MonoBehaviour
    {
        public class Baker : Baker<SpawnPointAuthoring>
        {
            public override void Bake(SpawnPointAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpawnPoint());
            }
        }
    }

    /// <summary>
    /// Placed in the GameScene subscene, the SpawnPoint components are used by the <see cref="ServerGameSystem"/>
    /// to spawn player characters during a game session.
    /// </summary>
    public struct SpawnPoint : IComponentData
    {
    }
}
