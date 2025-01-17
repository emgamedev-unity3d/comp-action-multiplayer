using System;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public enum GlobalGameState
    {
        MainMenu,
        InGame,
        Loading,
    }

    public enum MainMenuState
    {
        MainMenuScreen,
        DirectConnectPopUp,
        JoinCodePopUp,
    }

    public enum PlayerState
    {
        Playing,
        Dead,
    }

    public class GameSettings : INotifyBindablePropertyChanged
    {
        public static GameSettings Instance { get; private set; } = null!;

        /// <summary>
        /// This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad() => Instance = new GameSettings();

        const string k_PlayerNameKey = "PlayerName";
        const string k_SpectatorToggleKey = "SpectatorToggle";
        const string k_LookSensitivityKey = "LookSensitivity";
        const string k_InvertYAxisKey = "InvertYAxis";

        GameSettings()
        {
            m_PlayerName = PlayerPrefs.GetString(k_PlayerNameKey, Environment.UserName);
            m_SpectatorToggle = PlayerPrefs.GetInt(k_SpectatorToggleKey, 0) != 0;
            m_LookSensitivity = PlayerPrefs.GetFloat(k_LookSensitivityKey, 3.0f);
            m_InvertYAxis = PlayerPrefs.GetInt(k_InvertYAxisKey, 0) != 0;
        }

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        void Notify([CallerMemberName] string property = "") =>
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));

        public AwaitableCompletionSource CancellableUserInputPopUp;

        GlobalGameState m_GameState;
        public GlobalGameState GameState
        {
            get => m_GameState;
            set
            {
                if (m_GameState == value)
                    return;

                m_GameState = value;

                Notify(MainMenuStylePropertyName);

                Notify(LoadingScreenStylePropertyName);

                Notify(InGameUIPropertyName);
                Notify(RespawnScreenStylePropertyName);
            }
        }
        MainMenuState m_MainMenuState;
        public MainMenuState MainMenuState
        {
            get => m_MainMenuState;
            set
            {
                if (m_MainMenuState == value)
                    return;

                m_MainMenuState = value;
                Notify(MainMenuStylePropertyName);
                Notify(SessionCodeStylePropertyName);
                Notify(DirectConnectStylePropertyName);
            }
        }

        public static readonly string MainMenuStylePropertyName = nameof(MainMenuStyle);
        [CreateProperty]
        DisplayStyle MainMenuStyle => m_GameState == GlobalGameState.MainMenu && MainMenuState == MainMenuState.MainMenuScreen ? DisplayStyle.Flex : DisplayStyle.None;

        public static readonly string SessionCodeStylePropertyName = nameof(SessionCodeStyle);
        [CreateProperty]
        DisplayStyle SessionCodeStyle => m_GameState == GlobalGameState.MainMenu && MainMenuState == MainMenuState.JoinCodePopUp ? DisplayStyle.Flex : DisplayStyle.None;

        public static readonly string DirectConnectStylePropertyName = nameof(DirectConnectStyle);
        [CreateProperty]
        DisplayStyle DirectConnectStyle => m_GameState == GlobalGameState.MainMenu && MainMenuState == MainMenuState.DirectConnectPopUp ? DisplayStyle.Flex : DisplayStyle.None;

        public static readonly string LoadingScreenStylePropertyName = nameof(LoadingScreenStyle);
        [CreateProperty]
        DisplayStyle LoadingScreenStyle
        {
            get
            {
                if (m_GameState == GlobalGameState.Loading)
                    return DisplayStyle.Flex;

                LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.NotLoading,  0.0f);
                return DisplayStyle.None;
            }
        }

        public static readonly string InGameUIPropertyName = nameof(InGameUI);
        [CreateProperty]
        DisplayStyle InGameUI => m_GameState == GlobalGameState.InGame ? DisplayStyle.Flex : DisplayStyle.None;

        bool m_IsPauseMenuOpen;
        public bool IsPauseMenuOpen
        {
            get => m_IsPauseMenuOpen;
            set
            {
                if (m_IsPauseMenuOpen == value)
                    return;

                m_IsPauseMenuOpen = value;
                Notify(PauseMenuStylePropertyName);
                Notify(MobileControlsOpacityPropertyName);
            }
        }
        public static readonly string PauseMenuStylePropertyName = nameof(PauseMenuStyle);
        [CreateProperty]
        public DisplayStyle PauseMenuStyle => IsPauseMenuOpen ? DisplayStyle.Flex : DisplayStyle.None;

        public static readonly string MobileControlsOpacityPropertyName = nameof(MobileControlsPauseMenuOpacity);
        [CreateProperty]
        public StyleFloat MobileControlsPauseMenuOpacity => IsPauseMenuOpen ? 0.5f : 1f;

        PlayerState m_PlayerState;
        public PlayerState PlayerState
        {
            get => m_PlayerState;
            set
            {
                if (m_PlayerState == value)
                    return;

                m_PlayerState = value;
                Notify(RespawnScreenStylePropertyName);
            }
        }

        public static readonly string RespawnScreenStylePropertyName = nameof(RespawnScreenStyle);
        [CreateProperty]
        public DisplayStyle RespawnScreenStyle =>
            m_GameState == GlobalGameState.InGame && m_PlayerState == PlayerState.Dead
                ? DisplayStyle.Flex
                : DisplayStyle.None;

        string m_PlayerName;
        [CreateProperty]
        public string PlayerName
        {
            get => m_PlayerName;
            set
            {
                if (m_PlayerName == value)
                    return;

                m_PlayerName = value;
                PlayerPrefs.SetString(k_PlayerNameKey, value);
            }
        }

        bool m_SpectatorToggle;
        [CreateProperty]
        public bool SpectatorToggle
        {
            get => m_SpectatorToggle;
            set
            {
                if (m_SpectatorToggle == value)
                    return;

                m_SpectatorToggle = value;
                PlayerPrefs.SetInt(k_SpectatorToggleKey, value ? 1 : 0);
                Notify();
                Notify(PlayerOnlyStylePropertyName);
                Notify(SpectatorOnlyStylePropertyName);
            }
        }

        public static readonly string PlayerOnlyStylePropertyName = nameof(PlayerOnlyStyle);
        [CreateProperty]
        DisplayStyle PlayerOnlyStyle => m_SpectatorToggle ? DisplayStyle.None : DisplayStyle.Flex;

        public static readonly string SpectatorOnlyStylePropertyName = nameof(SpectatorOnlyStyle);
        [CreateProperty]
        DisplayStyle SpectatorOnlyStyle => m_SpectatorToggle ? DisplayStyle.Flex : DisplayStyle.None;

        float m_LookSensitivity;
        [CreateProperty]
        public float LookSensitivity
        {
            get => m_LookSensitivity;
            set
            {
                m_LookSensitivity = value;
                PlayerPrefs.SetFloat(k_LookSensitivityKey, value);
                Notify();
            }
        }

        bool m_InvertYAxis;
        [CreateProperty]
        public bool InvertYAxis
        {
            get => m_InvertYAxis;
            set
            {
                m_InvertYAxis = value;
                PlayerPrefs.SetInt(k_InvertYAxisKey, value == true ? 1 : 0);
                Notify();
            }
        }

        public static readonly string MainMenuSceneLoadedPropertyName = nameof(MainMenuSceneLoadedStyle);
        [CreateProperty]
        DisplayStyle MainMenuSceneLoadedStyle => m_MainMenuSceneLoaded ? DisplayStyle.None : DisplayStyle.Flex;

        bool m_MainMenuSceneLoaded;
        public bool MainMenuSceneLoaded
        {
            get => m_MainMenuSceneLoaded;
            set
            {
                if (m_MainMenuSceneLoaded == value)
                    return;

                m_MainMenuSceneLoaded = value;
                Notify(MainMenuSceneLoadedPropertyName);
            }
        }
    }
}
