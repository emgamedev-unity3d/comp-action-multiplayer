using Unity.Physics;
using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// All defined layer masks in this project, as well as common combinations.
    /// </summary>
    public static class GameLayers
    {
        public static int Default;
        public static int Players;
        public static int Weapons;
        public static CollisionFilter CollideWithPlayers;

#if UNITY_EDITOR
        // Because the layer masks are using in the Dots subscene baking,
        // this method needs to be initialized both at runtime and in the editor.
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod]
        public static void Init()
        {
            Default = LayerMask.NameToLayer(nameof(Default));
            Players = LayerMask.NameToLayer(nameof(Players));
            Weapons = LayerMask.NameToLayer(nameof(Weapons));
            CollideWithPlayers = CreateCollidesWithCollisionFilter(Players);
        }

        static CollisionFilter CreateCollidesWithCollisionFilter(int layerAsIndex)
        {
            var mask = 1u << layerAsIndex;
            var filter = CollisionFilter.Default;
            filter.CollidesWith = mask;
            return filter;
        }

    }
}
