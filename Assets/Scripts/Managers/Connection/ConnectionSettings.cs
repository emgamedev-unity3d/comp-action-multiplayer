using System;
using System.Runtime.CompilerServices;
using Unity.Networking.Transport;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public enum GameConnectionState
    {
        NotConnected,
        Connecting,
        Connected,
        Matchmaking,
    }

    /// <summary>
    /// Relay or Direct connection type, set by <see cref="ServicesSettings"/>.
    /// </summary>
    public enum ConnectionType
    {
        Relay = 0,
        Direct = 1,
    }

    /// <summary>
    /// P2P (peer to peer) or Dgs (dedicated game server) matchmaker type, set by <see cref="ServicesSettings"/>.
    /// </summary>
    public enum MatchmakerType
    {
        P2P = 0,
        Dgs = 1,
    }

    public enum CreationType
    {
        Create = 0,
        Join = 1,
        QuickJoin = 2,
    }

    public class ConnectionSettings : INotifyBindablePropertyChanged
    {
        public static ConnectionSettings Instance { get; private set; } = null!;

        /// <summary>
        /// This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad() => Instance = new ConnectionSettings();

        public const string DefaultServerAddress = "127.0.0.1";
        public const ushort DefaultServerPort = 7979;

        const string k_IPAddressKey = "IPAddress";
        const string k_PortKey = "Port";

        public NetworkEndpoint ConnectionEndpoint;

        ConnectionSettings()
        {
            IPAddress = PlayerPrefs.GetString(k_IPAddressKey, DefaultServerAddress);
            if (!NetworkEndpoint.TryParse(IPAddress, 0, out _))
                IPAddress = DefaultServerAddress;

            Port = PlayerPrefs.GetString(k_PortKey, DefaultServerPort.ToString());
            if (!ushort.TryParse(Port, out _))
                Port = DefaultServerPort.ToString();
        }

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        void Notify([CallerMemberName] string property = "") =>
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));

        GameConnectionState m_GameConnectionState;
        public GameConnectionState GameConnectionState
        {
            get => m_GameConnectionState;
            set
            {
                if (m_GameConnectionState == value)
                    return;
                m_GameConnectionState = value;
                Notify(ConnectionStatusStylePropertyName);
            }
        }
        public static readonly string ConnectionStatusStylePropertyName = nameof(ConnectionStatusStyle);
        [CreateProperty]
        DisplayStyle ConnectionStatusStyle =>
            m_GameConnectionState is GameConnectionState.Connecting or GameConnectionState.Matchmaking
                ? DisplayStyle.Flex
                : DisplayStyle.None;


        bool m_IsNetworkEndpointFormatValid;
        [CreateProperty]
        public bool IsNetworkEndpointValid
        {
            get => m_IsNetworkEndpointFormatValid;
            set
            {
                if (m_IsNetworkEndpointFormatValid == value)
                    return;
                m_IsNetworkEndpointFormatValid = value;
                Notify();
            }
        }
        string m_IPAddress;
        [CreateProperty]
        public string IPAddress
        {
            get => m_IPAddress;
            set
            {
                if (m_IPAddress == value)
                    return;

                m_IPAddress = value;
                PlayerPrefs.SetString(k_IPAddressKey, value);
                IsNetworkEndpointValid = NetworkEndpoint.TryParse(m_IPAddress, 0, out _) && ushort.TryParse(m_Port, out _);
                Notify();
            }
        }
        string m_Port;
        [CreateProperty]
        public string Port
        {
            get => m_Port;
            set
            {
                if (m_Port == value)
                    return;

                m_Port = value;
                PlayerPrefs.SetString(k_PortKey, value);
                IsNetworkEndpointValid = NetworkEndpoint.TryParse(m_IPAddress, 0, out _) && ushort.TryParse(m_Port, out _);
                Notify();
            }
        }

        bool m_IsSessionCodeFormatValid;
        [CreateProperty]
        public bool IsSessionCodeFormatValid
        {
            get => m_IsSessionCodeFormatValid;
            private set
            {
                if (m_IsSessionCodeFormatValid == value)
                    return;
                m_IsSessionCodeFormatValid = value;
                Notify();
            }
        }
        string m_SessionCode;
        [CreateProperty]
        public string SessionCode
        {
            get => m_SessionCode;
            set
            {
                if(m_SessionCode == value)
                    return;

                m_SessionCode = value;
                IsSessionCodeFormatValid = CheckIsSessionCodeFormatValid(m_SessionCode);
                Notify();
            }
        }
        static bool CheckIsSessionCodeFormatValid(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length != 6)
                return false;

            foreach (var c in str)
            {
                if (!char.IsLetter(c) && !char.IsNumber(c))
                    return false;
            }
            return true;
        }
    }
}
