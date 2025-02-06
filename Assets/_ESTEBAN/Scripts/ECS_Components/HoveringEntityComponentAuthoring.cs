using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class HoveringEntityComponentAuthoring : MonoBehaviour
    {
        [SerializeField]
        private float sinHeight = 2f;

        [SerializeField]
        public float sinLenght = 0.5f;

        class Baker : Baker<HoveringEntityComponentAuthoring>
        {
            public override void Bake(HoveringEntityComponentAuthoring authoring)
            {
                Entity entity = GetEntity(
                    TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

                AddComponent(entity, new HoveringEntityComponent()
                {
                    originalPosition = authoring.transform.position,
                    sinHeight = authoring.sinHeight,
                    sinLenght = authoring.sinLenght
                });
            }
        }
    }
}
