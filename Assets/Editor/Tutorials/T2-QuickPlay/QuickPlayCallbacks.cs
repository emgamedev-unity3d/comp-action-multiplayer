using Unity.NetCode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Template.CompetitiveActionMultiplayer.Editor.Tutorials
{
    [CreateAssetMenu(fileName = nameof(QuickPlayCallbacks), menuName = "Tutorials/Callbacks/" + nameof(QuickPlayCallbacks))]
    public class QuickPlayCallbacks : ScriptableObject
    {
        [SerializeField]
        SceneAsset m_GameplayScene;

        public bool MultiplayerPlayModeTypeIsClientAndServer()
        {
            return MultiplayerPlayModePreferences.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer;
        }

        public bool GameplaySceneIsLoaded()
        {
            return SceneManager.GetActiveScene().path == AssetDatabase.GetAssetPath(m_GameplayScene);
        }

        public bool ThinClientIsNotZero()
        {
            return MultiplayerPlayModePreferences.RequestedNumThinClients > 0;
        }
    }
}
