using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    public class LoadingData : INotifyBindablePropertyChanged
    {
        public enum LoadingSteps
        {
            StartLoading,
            InitializeConnection,
            LookingForMatch,
            CreateWorld,
            WaitingConnection,
            LoadGameScene,
            LoadResourcesScene,
            LoadServer,
            LoadClient,
            WorldReplication,
            WaitingOnPlayer,
            LoadingDone,

            UnloadingGame,
            DisconnectingClient,
            UnloadingWorld,
            UnloadingGameScene,
            UnloadingResourcesScene,
            BackToMainMenu,

            NotLoading,
        }

        struct LoadingStep
        {
            public readonly string Text;
            public readonly float Start;
            public readonly float End;

            public LoadingStep(string text, float start, float end)
            {
                Text = text;
                Start = start;
                End = end;
            }
        }

        static readonly Dictionary<LoadingSteps, LoadingStep> k_LoadingSteps = new()
        {
            { LoadingSteps.StartLoading , new LoadingStep("Loading game...", 0f, 0f) },
            { LoadingSteps.InitializeConnection , new LoadingStep("Initializing connection...", 0.1f, 0.1f) },
            { LoadingSteps.LookingForMatch , new LoadingStep("Looking for a match session...", 0.12f, 0.12f) },
            { LoadingSteps.CreateWorld , new LoadingStep("Creating entity worlds...", 0.15f, 0.15f) },
            { LoadingSteps.WaitingConnection , new LoadingStep("Waiting for Client connection...", 0.2f, 0.2f) },
            { LoadingSteps.LoadGameScene , new LoadingStep("Loading Gameplay scene...", 0.3f, 0.5f) },
            { LoadingSteps.LoadResourcesScene , new LoadingStep("Loading Resources scene...", 0.6f, 0.6f) },
            { LoadingSteps.LoadServer , new LoadingStep("Loading Server world...", 0.6f, 0.7f) },
            { LoadingSteps.LoadClient , new LoadingStep("Loading Client world...", 0.7f, 0.8f) },
            { LoadingSteps.WorldReplication , new LoadingStep("Replicating world...", 0.8f, 0.9f) },
            { LoadingSteps.WaitingOnPlayer , new LoadingStep("Waiting for Player spawn...", 0.9f, 0.9f) },
            { LoadingSteps.LoadingDone , new LoadingStep("Starting gameplay...", 1f, 1f) },

            { LoadingSteps.UnloadingGame , new LoadingStep("Leaving gameplay...", 0f, 0f) },
            { LoadingSteps.DisconnectingClient , new LoadingStep("Disconnecting Client...", 0.1f, 0.1f) },
            { LoadingSteps.UnloadingWorld , new LoadingStep("Disposing entity worlds...", 0.1f, 0.2f) },
            { LoadingSteps.UnloadingGameScene , new LoadingStep("Unloading Gameplay scene...", 0.2f, 0.5f) },
            { LoadingSteps.UnloadingResourcesScene , new LoadingStep("Unloading Resources scene...", 0.5f, 0.9f) },
            { LoadingSteps.BackToMainMenu , new LoadingStep("Opening Main menu...", 1f, 1f) },

            { LoadingSteps.NotLoading , new LoadingStep("_", 0f, 0f) },
        };

        public static LoadingData Instance { get; private set; } = null!;

        /// <summary>
        /// This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad() => Instance = new LoadingData();

        LoadingData()
        {
            m_LoadingProgress = 0.0f;
        }

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        void Notify([CallerMemberName] string property = "") =>
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));

        public void UpdateLoading(LoadingSteps step, float stepProgress = 0f)
        {
            var currentStep = k_LoadingSteps[step];
            LoadingProgress = currentStep.Start + stepProgress * (currentStep.End - currentStep.Start);
            LoadingStatusText = currentStep.Text;
        }

        float m_LoadingProgress;
        public const string LoadingProgressPropertyName = nameof(LoadingProgress);
        [CreateProperty]
        float LoadingProgress
        {
            get => m_LoadingProgress;
            set
            {
                m_LoadingProgress = value;
                Notify();
            }
        }

        string m_LoadingStatusText;
        public const string LoadingStatusTextPropertyName = nameof(LoadingStatusText);
        [CreateProperty]
        string LoadingStatusText
        {
            get => m_LoadingStatusText;
            set
            {
                if (m_LoadingStatusText == value)
                    return;

                m_LoadingStatusText = value;
                Notify();
            }
        }
    }
}
