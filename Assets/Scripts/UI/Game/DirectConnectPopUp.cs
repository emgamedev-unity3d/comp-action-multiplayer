using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [RequireComponent(typeof(UIDocument))]
    public class DirectConnectPopUp : MonoBehaviour
    {
        static class UIElementNames
        {
            public const string IPAddressInputField = "IPAddressField";
            public const string PortInputField = "PortField";
            public const string ConfirmButton = "ConfirmButton";
            public const string CancelButton = "CancelButton";
        }

        VisualElement m_DirectConnectPopUp;
        Button m_ConfirmButton;
        Button m_CancelButton;

        void OnEnable()
        {
            m_DirectConnectPopUp = GetComponent<UIDocument>().rootVisualElement;
            m_DirectConnectPopUp.RegisterTextFieldInputCallbacks();

            m_DirectConnectPopUp.SetBinding("style.display", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(GameSettings.DirectConnectStylePropertyName),
                bindingMode = BindingMode.ToTarget,
            });

            var ipAddressInputField = m_DirectConnectPopUp.Q<TextField>(UIElementNames.IPAddressInputField);
            ipAddressInputField.SetBinding("value", new DataBinding
            {
                dataSource = ConnectionSettings.Instance,
                dataSourcePath = new PropertyPath(nameof(ConnectionSettings.IPAddress)),
                bindingMode = BindingMode.TwoWay,
            });
            
            var portInputField = m_DirectConnectPopUp.Q<TextField>(UIElementNames.PortInputField);
            portInputField.SetBinding("value", new DataBinding
            {
                dataSource = ConnectionSettings.Instance,
                dataSourcePath = new PropertyPath(nameof(ConnectionSettings.Port)),
                bindingMode = BindingMode.TwoWay,
            });

            m_ConfirmButton = m_DirectConnectPopUp.Q<Button>(UIElementNames.ConfirmButton);
            m_ConfirmButton.clicked += OnConfirmPressed;
            m_ConfirmButton.SetBinding("enabledSelf", new DataBinding
            {
                dataSource = ConnectionSettings.Instance,
                dataSourcePath = new PropertyPath(nameof(ConnectionSettings.IsNetworkEndpointValid)),
                bindingMode = BindingMode.ToTarget,
            });

            m_CancelButton = m_DirectConnectPopUp.Q<Button>(UIElementNames.CancelButton);
            m_CancelButton.clicked += OnCancelPressed;
        }

        void OnDisable()
        {
            m_DirectConnectPopUp.UnregisterTextFieldInputCallbacks();

            m_ConfirmButton.clicked -= OnConfirmPressed;
            m_CancelButton.clicked -= OnCancelPressed;
        }

        static void OnConfirmPressed() => GameSettings.Instance.CancellableUserInputPopUp.SetResult();

        static void OnCancelPressed()
        {
            GameSettings.Instance.CancellableUserInputPopUp.SetCanceled();
            ConnectionSettings.Instance.IPAddress = ConnectionSettings.DefaultServerAddress;
            ConnectionSettings.Instance.Port = ConnectionSettings.DefaultServerPort.ToString();
        }
    }
}
