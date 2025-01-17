using Unity.Entities;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [DisallowMultipleComponent]
    public class FirstPersonCharacterViewAuthoring : MonoBehaviour
    {
        public GameObject Character;

        public class Baker : Baker<FirstPersonCharacterViewAuthoring>
        {
            public override void Bake(FirstPersonCharacterViewAuthoring authoring)
            {
                if (authoring.transform.parent != authoring.Character.transform)
                {
                    Debug.LogError("ERROR: the Character View must be a direct 1st-level child of the character authoring GameObject. Conversion will be aborted");
                    return;
                }

                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new FirstPersonCharacterView { CharacterEntity = GetEntity(authoring.Character, TransformUsageFlags.Dynamic) });
            }
        }
    }
}
