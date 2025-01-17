using System;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Burst;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Profiling;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [RequireComponent(typeof(UIDocument))]
    public class SessionInfo : MonoBehaviour
    {
        const float k_UpdateSessionInfoDelay = 2f;
        const string k_RedColor = "#ff5555";
        const string k_OrangeColor = "#ffb86c";
        const string k_GreenColor = "#50fa7b";

        static class UIElementNames
        {
            public const string SessionInfoContainer = "SessionInfoContainer";
            public const string BinaryInfoLabel = "BinaryInfo";
            public const string ArgsInfoLabel = "ArgsInfo";
            public const string RightInfoLabel = "RightInfo";
            public const string HardwareInfoLabel = "HardwareInfo";
            public const string ConnectionInfoLabel = "ConnectionInfo";
            public const string ShowSessionInfo = "ShowSessionInfo";
        }

        VisualElement m_SessionInfoBanner;
        VisualElement m_SessionInfoContainer;

        Label m_BinaryInfoLabel;
        Label m_RightInfoLabel;
        Label m_HardwareInfoLabel;
        Label m_ConnectionInfoLabel;
        Label m_ArgsInfoLabel;
        Label m_ShowSessionInfo;

        string m_RightInfo;
        string m_ConnectionInfo;
        string m_MachineInfo;

        ProfilerRecorder m_SystemMemoryRecorder;
        ProfilerRecorder m_GcMemoryRecorder;
        ProfilerRecorder m_CpuRecorder;
        ProfilerRecorder m_CpuToGpuRecorder;
        ProfilerRecorder m_GpuRecorder;
        ProfilerRecorder m_GcCountRecorder;
        ProfilerRecorder m_DrawCallsRecorder;
        ProfilerRecorder m_FilesOpenRecorder;
        ProfilerRecorder m_FilesBytesReadRecorder;

        float m_Timer;

        async void OnEnable()
        {
            m_SessionInfoBanner = GetComponent<UIDocument>().rootVisualElement;

            GameInput.Actions.DebugActions.ToggleSessionInfo.performed += OnToggleSessionInfoVisibilityPerformed;

            m_SessionInfoContainer = m_SessionInfoBanner.Q<VisualElement>(UIElementNames.SessionInfoContainer);
            m_BinaryInfoLabel = m_SessionInfoBanner.Q<Label>(UIElementNames.BinaryInfoLabel);
            m_RightInfoLabel = m_SessionInfoBanner.Q<Label>(UIElementNames.RightInfoLabel);
            m_HardwareInfoLabel = m_SessionInfoBanner.Q<Label>(UIElementNames.HardwareInfoLabel);
            m_ConnectionInfoLabel = m_SessionInfoBanner.Q<Label>(UIElementNames.ConnectionInfoLabel);
            m_ConnectionInfoLabel.RegisterCallback<ClickEvent>(CopySessionCode);
            m_ArgsInfoLabel = m_SessionInfoBanner.Q<Label>(UIElementNames.ArgsInfoLabel);
            m_ShowSessionInfo = m_SessionInfoBanner.Q<Label>(UIElementNames.ShowSessionInfo);

            var visibilityButton = await InputSystemManager.IsMobile ? "4 Finger Tap" : "i";
            m_ShowSessionInfo.text = $"Toggle visibility with <color={k_RedColor}>{visibilityButton}</color>";

            // https://docs.unity3d.com/Manual/frame-timing-manager.html
            m_SystemMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            m_GcMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
            m_CpuRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time", 15);
            m_CpuToGpuRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Render Thread Frame Time", 15);
            m_GpuRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GPU Frame Time", 15);
            m_GcCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC Allocation In Frame Count", 15);
            m_DrawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Draw Calls Count", 15);
            m_FilesOpenRecorder = ProfilerRecorder.StartNew(ProfilerCategory.FileIO, "File Handles Open", 15);
            m_FilesBytesReadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.FileIO, "File Bytes Read", 60);

            m_RightInfo = $"{{0}} | {{1}}";

            var hasArgs = HasArgs(out var args);
            m_ArgsInfoLabel.style.display = hasArgs ? DisplayStyle.Flex : DisplayStyle.None;
            m_ArgsInfoLabel.text = args;

            m_ConnectionInfo = $"Player: \"{{0}}\" on \"{Environment.MachineName}\" | {{1}}";

            var burst = BurstCompiler.IsEnabled ? " +BC" : "";
            var qualityPreset = $"{QualitySettings.names[QualitySettings.GetQualityLevel()]}";
            var targetFrameRate = Application.targetFrameRate > 0 ? Application.targetFrameRate.ToString() : "OFF";
            var vSync = QualitySettings.vSyncCount > 0 ? $"{QualitySettings.vSyncCount}th @ {Screen.currentResolution.refreshRateRatio.value}hz" : "OFF";
            m_MachineInfo = $"<color=#8be9fd>{SystemInfo.operatingSystem} | {SystemInfo.deviceModel} | {SystemInfo.processorType} | {SystemInfo.graphicsDeviceName}</color>";

            m_BinaryInfoLabel.text = $"{Application.productName} by {Application.companyName} | Ver {Application.version}{burst} | {GetBuildType()} | QSetting:{qualityPreset} | TargetFPS:{targetFrameRate} | VSync:{vSync}";
            UpdateSessionInfo();
        }

        void CopySessionCode(ClickEvent _)
        {
            if (ConnectionSettings.Instance.GameConnectionState != GameConnectionState.NotConnected
                && !string.IsNullOrEmpty(ConnectionSettings.Instance.SessionCode))
            {
                GUIUtility.systemCopyBuffer = ConnectionSettings.Instance.SessionCode;
                Debug.Log($"Session code {ConnectionSettings.Instance.SessionCode} was copied to clipboard.");
            }
        }

        void OnToggleSessionInfoVisibilityPerformed(InputAction.CallbackContext _) => ToggleSessionInfoVisibility();

        void ToggleSessionInfoVisibility()
        {
            m_SessionInfoContainer.style.display = m_SessionInfoContainer.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnDisable()
        {
            m_ConnectionInfoLabel.UnregisterCallback<ClickEvent>(CopySessionCode);
            GameInput.Actions.DebugActions.ToggleSessionInfo.performed -= OnToggleSessionInfoVisibilityPerformed;
        }

        void OnDestroy()
        {
            m_SystemMemoryRecorder.Dispose();
            m_GcMemoryRecorder.Dispose();
            m_CpuToGpuRecorder.Dispose();
            m_CpuRecorder.Dispose();
            m_GpuRecorder.Dispose();
            m_GcCountRecorder.Dispose();
            m_DrawCallsRecorder.Dispose();
            m_FilesOpenRecorder.Dispose();
            m_FilesBytesReadRecorder.Dispose();
        }

        void LateUpdate()
        {
            if (m_SessionInfoContainer.style.display == DisplayStyle.None)
                return;

            m_Timer += Time.deltaTime;
            if (m_Timer >= k_UpdateSessionInfoDelay)
            {
                m_Timer -= k_UpdateSessionInfoDelay;

                UpdateSessionInfo();
            }
        }

        static string GetBuildType()
        {
            if (Application.isEditor)
                return "EDITOR";
            return Debug.isDebugBuild ? "DEVELOP" : "RELEASE";
        }

        static bool HasArgs(out string args)
        {
            args = "";
            if (Application.isEditor)
                return false;

            var commandLineArgs = Environment.GetCommandLineArgs();

            StringBuilder sb = new StringBuilder();
            sb.Append("Args: ");

            // Ignore the first, as it's just the full path.
            for (var i = 1; i < commandLineArgs.Length; i++)
            {
                sb.Append(commandLineArgs[i]);
                if (i < commandLineArgs.Length - 1) sb.Append(' ');
            }
            args = sb.ToString();
            return commandLineArgs.Length > 1;
        }

        void UpdateSessionInfo()
        {
            var netcodeInfo = GetNetcodeInfo();
            var playerName = GameSettings.Instance.PlayerName;
            var networkRole = ClientServerBootstrap.HasServerWorld ? NetworkRole.Host : NetworkRole.Client;
            var sessionState =
                ConnectionSettings.Instance.GameConnectionState != GameConnectionState.NotConnected
                ? $"Session-Code: <b>{ConnectionSettings.Instance.SessionCode}</b> (click to copy) | Role: <b>{networkRole}</b>"
                : "No Session";

            m_ConnectionInfoLabel.text = string.Format(m_ConnectionInfo, playerName, sessionState);
            m_RightInfoLabel.text = string.Format(m_RightInfo, netcodeInfo, GetFps());
            m_HardwareInfoLabel.text =  GetCurrentParameters();
        }

        string GetFps()
        {
            var avgFps = Mathf.CeilToInt(1f / Time.smoothDeltaTime);
            if (avgFps <= 0) avgFps = Mathf.CeilToInt(1f / Time.deltaTime);
            var targetFps = GetImpliedTargetFps();
            return $"<color={GetPingColor()}>FPS {avgFps} / {(int)targetFps}</color>";

            double GetImpliedTargetFps()
            {
                var screenRefreshRate = Screen.currentResolution.refreshRateRatio.value;
                if (QualitySettings.vSyncCount <= 0)
                {
                    if (Application.targetFrameRate > 0)
                        return Application.targetFrameRate;
                    return screenRefreshRate;
                }
                return screenRefreshRate / QualitySettings.vSyncCount;
            }
            string GetPingColor()
            {
                var currentFpsVsTargetFpsRatio = avgFps / targetFps;
                if (currentFpsVsTargetFpsRatio >= 0.99f)
                    return k_GreenColor;
                if (currentFpsVsTargetFpsRatio >= 0.5f || avgFps >= 60)
                    return k_OrangeColor;
                return k_RedColor;
            }
        }

        string GetCurrentParameters()
        {
            var cpu = m_CpuRecorder.Valid ? $"CPU:{GetRecorderFrameAverage(m_CpuRecorder) * (1e-6f):F1}ms" : null;
            var sendToGpu = m_CpuToGpuRecorder.Valid ? $"Render:{GetRecorderFrameAverage(m_CpuToGpuRecorder) * (1e-6f):F1}ms" : null;
            var gpu = m_GpuRecorder.Valid ? $"GPU:{GetRecorderFrameAverage(m_GpuRecorder) * (1e-6f):F1}ms" : null;
            var drawCalls = m_DrawCallsRecorder.Valid ? $"Draw Calls:{GetRecorderFrameAverage(m_DrawCallsRecorder):0}" : null;
            var gcMemory = m_GcMemoryRecorder.Valid ? $"Managed:{m_GcMemoryRecorder.LastValue / (1024 * 1024)}MB" : null;
            var systemMemory = m_SystemMemoryRecorder.Valid ? $"Native:{m_SystemMemoryRecorder.LastValue / (1024 * 1024)}MB" : null;
            var gcCount = m_GcCountRecorder.Valid ? $"GC/Frame:{GetRecorderFrameAverage(m_GcCountRecorder):0}" : null;
            var filesOpen = m_FilesOpenRecorder.Valid ? $"Files Open:{m_FilesOpenRecorder.LastValue:0}" : null;
            var fileReadBytes = m_FilesBytesReadRecorder.Valid ? $"File Read Bytes:{GetRecorderFrameAverage(m_FilesBytesReadRecorder, false) / (1024 * 1024):0.0}MB" : null;


            var output = $"{cpu} | {sendToGpu} | {gpu} | {drawCalls} | {gcMemory} | {gcCount} | {systemMemory} | {filesOpen} | {fileReadBytes} | {m_MachineInfo}";

            // Replace multiple | | in case a value is null.
            return Regex.Replace(output,  @"\s*\|\s*(\|\s*)+", " | ");
        }

        static double GetRecorderFrameAverage(ProfilerRecorder recorder, bool avg = true)
        {
            var samplesCount = recorder.Capacity;
            if (samplesCount == 0)
                return 0;

            double r = 0;
            unsafe
            {
                var samples = stackalloc ProfilerRecorderSample[samplesCount];
                recorder.CopyTo(samples, samplesCount);
                for (var i = 0; i < samplesCount; ++i)
                    r += samples[i].Value;
                if (avg) r /= samplesCount;
            }

            return r;
        }

        /// <summary>
        /// TODO - Ideally this data would be fetched from a system (which can cache), but it works well enough for now.
        /// </summary>
        /// <returns></returns>
        static string GetNetcodeInfo()
        {
            var clientWorld = ClientServerBootstrap.ClientWorld;
            if (clientWorld != null && clientWorld.IsCreated)
            {
                using var connectionQuery = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamConnection));
                connectionQuery.CompleteDependency();
                if (connectionQuery.TryGetSingleton<NetworkStreamConnection>(out var networkStreamConnection))
                {
                    using var driverQuery = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver));
                    driverQuery.CompleteDependency();
                    if (driverQuery.TryGetSingleton<NetworkStreamDriver>(out var networkStreamDriver))
                    {
                        using var pingQuery = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkSnapshotAck));
                        pingQuery.CompleteDependency();
                        var ping = string.Empty;
                        var pingColor = "gray";
                        if (pingQuery.TryGetSingleton<NetworkSnapshotAck>(out var ack))
                        {
                            pingColor = k_OrangeColor;
                            if (networkStreamConnection.CurrentState == ConnectionState.State.Connected)
                            {
                                var pingEstimate = (int)ack.EstimatedRTT;
                                // The Netcode for Entities `EstimatedRTT` value adds some amount of the frame time
                                // to the reported value. Thus, a realistic ping value is more accurate if it is removed.
                                const float assumedSimulationTickRate = 60;
                                const float lastSimulationTickRateFrameMs = (1000f / assumedSimulationTickRate);
                                pingEstimate = (int) math.max(0, pingEstimate - lastSimulationTickRateFrameMs);
                                ping = $"Ping:{pingEstimate}±{(int)ack.DeviationRTT}ms, ";
                                if (ack.EstimatedRTT > 200)
                                    pingColor = k_RedColor;
                                else if (ack.EstimatedRTT > 100)
                                    pingColor = k_OrangeColor;
                                else pingColor = k_GreenColor;
                            }
                        }
                        return $"<color={pingColor}>{ping}{networkStreamConnection.CurrentState.ToFixedString()} @ {networkStreamDriver.GetRemoteEndPoint(networkStreamConnection)}</color>";
                    }
                }
                return $"<color={k_RedColor}>Not connected!</color>";
            }
            return $"<color={k_RedColor}>No client world!</color>";
        }
    }
}
