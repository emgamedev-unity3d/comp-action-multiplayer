using UnityEngine;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// This class allows the <see cref="MainCameraSystem"/> to sync its position to the current player character position in the Client World.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MainGameObjectCamera : MonoBehaviour
    {
        public static Camera Instance;

        void Awake()
        {
            // We already have a main camera and don't need a new one.
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = GetComponent<Camera>();
        }

        void OnDestroy()
        {
            if (Instance == GetComponent<Camera>())
            {
                Instance = null;
            }
        }
    }
}
