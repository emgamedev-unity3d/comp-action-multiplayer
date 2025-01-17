using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>In-game connection state screen.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class ConnectionStatusScreen : MonoBehaviour
    {
        static class UIElementNames
        {
            public const string CancelButton = "CancelButton";
        }

        Button m_CancelButton;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            root.SetBinding("style.display", new DataBinding
            {
                dataSource = ConnectionSettings.Instance,
                dataSourcePath = new PropertyPath(ConnectionSettings.ConnectionStatusStylePropertyName),
                bindingMode = BindingMode.ToTarget,
            });

            m_CancelButton = root.Q<Button>(UIElementNames.CancelButton);
            m_CancelButton.clicked += OnCancelPressed;
        }

        void OnDisable()
        {
            m_CancelButton.clicked -= OnCancelPressed;
        }

        static void OnCancelPressed() => GameManager.Instance.ReturnToMainMenuAsync();
    }
}
