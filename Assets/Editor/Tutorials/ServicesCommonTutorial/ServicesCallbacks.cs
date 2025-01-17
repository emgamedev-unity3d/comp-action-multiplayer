using Unity.Services.Core.Editor.Environments;
using Unity.Services.DeploymentApi.Editor;
using Unity.Tutorials.Core.Editor;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer.Editor.Tutorials
{
    [CreateAssetMenu(fileName = nameof(ServicesCallbacks), menuName = "Tutorials/Callbacks/" + nameof(ServicesCallbacks))]
    class ServicesCallbacks : ScriptableObject
    {
        [SerializeField]
        BuildProfile m_ServerBuildProfile;
        [SerializeField]
        ServicesSettings m_ServicesSettings;

        public bool IsProjectLinkedToUnityCloud()
        {
            return !string.IsNullOrEmpty(Application.cloudProjectId);
        }

        public bool IsUnityCloudEnvironmentSet()
        {
            return EnvironmentsApi.Instance.ActiveEnvironmentName == "production" && EnvironmentsApi.Instance.ActiveEnvironmentId != null;
        }

        public void SelectServicesSettings()
        {
            EditorGUIUtility.PingObject(m_ServicesSettings);
            Selection.activeObject = m_ServicesSettings;
            EditorApplication.delayCall += () => TutorialWindow.Instance.CurrentTutorial.CurrentPage.RaiseMaskingSettingsChanged();
        }

        public void CreateOnlyMainMenu()
        {
            GameSettings.Instance.SpectatorToggle = false;
            var rootElement = FindAnyObjectByType<MainMenu>().GetComponent<UIDocument>().rootVisualElement;
            rootElement.Q<Toggle>("SpectatorToggle").SetEnabled(false);
            rootElement.Q<Button>("JoinGame").SetEnabled(false);
            rootElement.Q<Button>("QuickJoin").SetEnabled(false);
            rootElement.Q<Button>("QuitButton").SetEnabled(false);
        }

        public void QuickJoinOnlyMainMenu()
        {
            GameSettings.Instance.SpectatorToggle = false;
            var rootElement = FindAnyObjectByType<MainMenu>().GetComponent<UIDocument>().rootVisualElement;
            rootElement.Q<Toggle>("SpectatorToggle").SetEnabled(false);
            rootElement.Q<Button>("CreateGame").SetEnabled(false);
            rootElement.Q<Button>("JoinGame").SetEnabled(false);
            rootElement.Q<Button>("QuitButton").SetEnabled(false);
        }

        public void EnableFullMainMenu()
        {
            var rootElement = FindAnyObjectByType<MainMenu>().GetComponent<UIDocument>().rootVisualElement;
            rootElement.Q<Toggle>("SpectatorToggle").SetEnabled(true);
            rootElement.Q<Button>("CreateGame").SetEnabled(true);
            rootElement.Q<Button>("JoinGame").SetEnabled(true);
            rootElement.Q<Button>("QuickJoin").SetEnabled(true);
            rootElement.Q<Button>("QuitButton").SetEnabled(true);
        }

        public bool IsPlayerInGame()
        {
            return GameSettings.Instance != null && GameSettings.Instance.GameState == GlobalGameState.InGame;
        }

        public bool ConnectionTypeIsRelay()
        {
            return m_ServicesSettings.ConnectionTypeRequested == ConnectionType.Relay;
        }

        public bool MatchmakerTypeIsP2P()
        {
            return m_ServicesSettings.MatchmakerTypeRequested == MatchmakerType.P2P;
        }

        public bool MatchmakerTypeIsDgs()
        {
            return m_ServicesSettings.MatchmakerTypeRequested == MatchmakerType.Dgs;
        }

        public bool MatchmakerP2PEnvironmentAndQueueDeployed()
        {
            bool queueConfigDeployed = false;
            bool envConfigDeployed = false;
            foreach (var provider in Deployments.Instance.DeploymentProviders)
            {
                foreach (var item in provider.DeploymentItems)
                {
                    if (item.Name == "Matchmaker-P2P-Queue.mmq")
                    {
                        queueConfigDeployed = item.Status.Message == "Deployed";
                    }
                    else if (item.Name == "Matchmaker-Environment.mme")
                    {
                        envConfigDeployed = item.Status.Message == "Deployed";
                    }
                }
            }
            return queueConfigDeployed && envConfigDeployed;
        }

        public bool DgsBuildDeployed()
        {
            bool buildDeployed = false;
            bool buildConfigDeployed = false;
            bool buildFleetDeployed = false;
            foreach (var provider in Deployments.Instance.DeploymentProviders)
            {
                foreach (var item in provider.DeploymentItems)
                {
                    if (item.Name == "DedicatedGameServer.build")
                    {
                        buildDeployed = item.Status.Message == "Deployed";
                    }
                    else if (item.Name == "DedicatedGameServer.buildConfig")
                    {
                        buildConfigDeployed = item.Status.Message == "Deployed";
                    }
                    else if (item.Name == "DedicatedGameServer.fleet")
                    {
                        buildFleetDeployed = item.Status.Message == "Deployed";
                    }
                }
            }
            return buildDeployed && buildConfigDeployed && buildFleetDeployed;
        }

        public bool DgsQueueDeployed()
        {
            bool queueConfigDeployed = false;
            bool envConfigDeployed = false;
            foreach (var provider in Deployments.Instance.DeploymentProviders)
            {
                foreach (var item in provider.DeploymentItems)
                {
                    if (item.Name == "DedicatedGameServer-Queue.mmq")
                    {
                        queueConfigDeployed = item.Status.Message == "Deployed";
                    }
                    else if (item.Name == "DedicatedGameServer-Environment.mme")
                    {
                        envConfigDeployed = item.Status.Message == "Deployed";
                    }
                }
            }
            return queueConfigDeployed && envConfigDeployed;
        }

        public bool LinuxServerBuildTargetInstalled()
        {
            return BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
        }

        public bool IsDedicatedGameServerProfileSelected()
        {
            var buildProfileWindow = EditorWindow.focusedWindow;
            var lists = buildProfileWindow.rootVisualElement.Query<ListView>().ToList();
            foreach (var list in lists)
            {
                if (list.selectedItem as BuildProfile == m_ServerBuildProfile)
                {
                    return true;
                }
            }

            return false;
        }

        public bool LinuxServerBuildProfileIsSelected()
        {
            return BuildProfile.GetActiveBuildProfile() == m_ServerBuildProfile;
        }

        public void ChangeMultiplayLinkToUserDashboard()
        {
            ChangeMultiplayLink(2, "overview");
        }

        public void ChangeMultiplayLinkToUserServerDashboard()
        {
            ChangeMultiplayLink(1, "servers");
        }

        async void ChangeMultiplayLink(int paragraphIndex, string multiplayPage)
        {
            await EnvironmentsApi.Instance.RefreshAsync();
            EnvironmentsApi.Instance.SetActiveEnvironment(EnvironmentsApi.Instance.ActiveEnvironmentName);
            var dashboardUrl =
                $"https://cloud.unity3d.com/organizations/{CloudProjectSettings.organizationKey}/projects/{CloudProjectSettings.projectId}/environments/{EnvironmentsApi.Instance.ActiveEnvironmentId}/multiplay/{multiplayPage}";
            var currentTutorial = TutorialWindow.Instance.CurrentTutorial.CurrentPage;
            var savedText = currentTutorial.Paragraphs[paragraphIndex].Text.Untranslated;
            currentTutorial.Paragraphs[paragraphIndex].Text.Untranslated = savedText.Replace("{0}", dashboardUrl);
            currentTutorial.RaiseNonMaskingSettingsChanged();

            currentTutorial.Paragraphs[paragraphIndex].Text.Untranslated = savedText;
        }
    }
}
