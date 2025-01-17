using UnityEngine;
using UnityEditor;
using Unity.Tutorials.Core.Editor;
using Object = UnityEngine.Object;

namespace Unity.Template.CompetitiveActionMultiplayer.Editor.Tutorials
{
    [CreateAssetMenu(fileName = nameof(TutorialCallbacks), menuName = "Tutorials/Callbacks/" + nameof(TutorialCallbacks))]
    class TutorialCallbacks : ScriptableObject
    {
        [SerializeField] SceneAsset m_MainMenuScene;

        public void StartTutorial(TutorialContainer tutorial)
        {
            var firstTutorial = tutorial.Sections?[0].Tutorial;
            if (firstTutorial != null)
                TutorialWindow.StartTutorial(firstTutorial);
        }

        public void FocusGameView()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }

        public void FocusSceneView()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Scene");
        }

        public void PingAsset(Object asset)
        {
            EditorGUIUtility.PingObject(asset);
        }

        public void LoadMainMenuScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(m_MainMenuScene));
        }

        public void RevealFolder(Object folder)
        {
            PingInnerAssetAndRevealFolder(folder);
            Selection.activeObject = folder;
            // Calling it twice over two frames because sometimes the ProjectBrowser doesn't refresh properly.
            EditorApplication.delayCall += () =>
            {
                AssetDatabase.OpenAsset(folder);
                EditorApplication.delayCall += () =>
                {
                    AssetDatabase.OpenAsset(folder);
                };
            };
        }

        static void PingInnerAssetAndRevealFolder(Object folder, int recursiveCount = 1)
        {
            var path = AssetDatabase.GetAssetPath(folder);
            if (AssetDatabase.IsValidFolder(path))
            {
                var directories = System.IO.Directory.GetDirectories(path);
                foreach (var directory in directories)
                {
                    if (AssetDatabase.IsValidFolder(directory))
                    {
                        var subAsset = AssetDatabase.LoadMainAssetAtPath(directory);
                        if (recursiveCount > 0)
                        {
                            PingInnerAssetAndRevealFolder(subAsset, recursiveCount - 1);
                            return;
                        }
                        EditorGUIUtility.PingObject(subAsset);
                        // Calling OpenAsset here will unfold the project browser hierarchy
                        AssetDatabase.OpenAsset(folder);
                        return;
                    }
                }

                var files = System.IO.Directory.GetFiles(path);
                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(file)))
                    {
                        var subAsset =  AssetDatabase.LoadMainAssetAtPath(file);
                        EditorGUIUtility.PingObject(subAsset);
                    }
                }
            }

            EditorGUIUtility.PingObject(folder);
        }
    }
}
