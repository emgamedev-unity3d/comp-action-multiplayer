using Unity.Properties;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [RequireComponent(typeof(UIDocument))]
    public class SessionCodePopUp : MonoBehaviour
    {
        static class UIElementNames
        {
            public const string SessionInputField = "SessionField";
            public const string JoinButton = "JoinButton";
            public const string CancelButton = "CancelButton";
        }

        VisualElement m_SessionCodePopUp;
        Button m_JoinButton;
        Button m_CancelButton;

        void OnEnable()
        {
            m_SessionCodePopUp = GetComponent<UIDocument>().rootVisualElement;
            m_SessionCodePopUp.RegisterTextFieldInputCallbacks();

            m_SessionCodePopUp.SetBinding("style.display", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(GameSettings.SessionCodeStylePropertyName),
                bindingMode = BindingMode.ToTarget,
            });

            var sessionCode = m_SessionCodePopUp.Q<TextField>(UIElementNames.SessionInputField);
            sessionCode.SetBinding("value", new DataBinding
            {
                dataSource = ConnectionSettings.Instance,
                dataSourcePath = new PropertyPath(nameof(ConnectionSettings.SessionCode)),
                bindingMode = BindingMode.TwoWay,
            });

            m_JoinButton = m_SessionCodePopUp.Q<Button>(UIElementNames.JoinButton);
            m_JoinButton.clicked += OnJoinPressed;
            m_JoinButton.SetBinding("enabledSelf", new DataBinding
            {
                dataSource = ConnectionSettings.Instance,
                dataSourcePath = new PropertyPath(nameof(ConnectionSettings.IsSessionCodeFormatValid)),
                bindingMode = BindingMode.ToTarget,
            });

            m_CancelButton = m_SessionCodePopUp.Q<Button>(UIElementNames.CancelButton);
            m_CancelButton.clicked += OnCancelPressed;
        }

        void OnDisable()
        {
            m_SessionCodePopUp.UnregisterTextFieldInputCallbacks();

            m_JoinButton.clicked -= OnJoinPressed;
            m_CancelButton.clicked -= OnCancelPressed;
        }

        static void OnJoinPressed() => GameSettings.Instance.CancellableUserInputPopUp.SetResult();

        static void OnCancelPressed()
        {
            GameSettings.Instance.CancellableUserInputPopUp.SetCanceled();
            ConnectionSettings.Instance.SessionCode = "";
        }
    }
}
