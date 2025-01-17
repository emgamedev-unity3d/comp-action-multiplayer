using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class SpectatorSpawnPointAuthoring : MonoBehaviour
    {
        public class Baker : Baker<SpectatorSpawnPointAuthoring>
        {
            public override void Bake(SpectatorSpawnPointAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpectatorSpawnPoint());
            }
        }
    }

    /// <summary>
    /// Placed in the GameScene subscene, the SpectatorSpawnPoint components are used by the <see cref="ClientGameSystem"/>
    /// to spawn the spectator controller during a game session.
    /// </summary>
    public struct SpectatorSpawnPoint : IComponentData
    {
    }
}
