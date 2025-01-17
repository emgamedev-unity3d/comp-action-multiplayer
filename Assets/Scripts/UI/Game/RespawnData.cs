using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class RespawnData : INotifyBindablePropertyChanged
    {
        public static RespawnData Instance { get; private set; } = null!;

        /// <summary>
        /// This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad() => Instance = new RespawnData();

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        void Notify([CallerMemberName] string property = "") =>
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));

        public static readonly string RespawnMessagePropertyName = nameof(m_RespawnMessageText);
        [CreateProperty]
        string m_RespawnMessageText;

        int m_RespawnTimer;
        public int RespawnTimer
        {
            get => m_RespawnTimer;
            set
            {
                if (m_RespawnTimer != value)
                {
                    m_RespawnTimer = value;
                    m_RespawnMessageText = m_RespawnTimer > 0
                        ? $"Respawning in {(int)math.ceil(m_RespawnTimer)}..."
                        : "Waiting for free spawn point...";
                    Notify(RespawnMessagePropertyName);
                }
            }
        }
    }
}
