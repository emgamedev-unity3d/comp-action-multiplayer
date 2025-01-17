using Unity.Properties;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// PauseMenu functionality.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenu : MonoBehaviour
    {
        static class UIElementNames
        {
            public const string LookSensitivitySlider = "LookSensitivitySlider";
            public const string ResumeButton = "ResumeButton";
            public const string MainMenuButton = "MainMenuButton";
            public const string QuitButton = "QuitButton";
            public const string InvertYAxis = "InvertYAxis";
        }

        Button m_ResumeButton;
        Button m_MainMenuButton;
        Button m_QuitButton;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            GameInput.Actions.Gameplay.TogglePauseMenu.performed += TogglePauseMenuVisibility;

            root.SetBinding("style.display", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(GameSettings.PauseMenuStylePropertyName),
                bindingMode = BindingMode.ToTarget,
            });
            var sensitivitySlider = root.Q<Slider>(UIElementNames.LookSensitivitySlider);
            sensitivitySlider.SetBinding("value", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(nameof(GameSettings.LookSensitivity)),
                bindingMode = BindingMode.TwoWay,
            });
            var invertYAxis = root.Q<Toggle>(UIElementNames.InvertYAxis);
            invertYAxis.SetBinding("value", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(nameof(GameSettings.InvertYAxis)),
                bindingMode = BindingMode.TwoWay,
            });

            m_ResumeButton = root.Q<Button>(UIElementNames.ResumeButton);
            m_ResumeButton.clicked += OnResumePressed;

            m_MainMenuButton = root.Q<Button>(UIElementNames.MainMenuButton);
            m_MainMenuButton.clicked += OnMainMenuPressed;
            m_MainMenuButton.SetEnabled(GameManager.CanUseMainMenu);

            m_QuitButton = root.Q<Button>(UIElementNames.QuitButton);
            m_QuitButton.clicked += OnQuitPressed;
        }

        void OnDisable()
        {
            GameInput.Actions.Gameplay.TogglePauseMenu.performed -= TogglePauseMenuVisibility;

            m_ResumeButton.clicked -= OnResumePressed;
            m_MainMenuButton.clicked -= OnMainMenuPressed;
            m_QuitButton.clicked -= OnQuitPressed;
        }

        void TogglePauseMenuVisibility(InputAction.CallbackContext obj)
        {
            // UIToolkit does not have any focus by default which prevent keyboards to navigate the UI until a mouse click happens.
            // Forcing the focus here ensure that no mouse click is required before other devices inputs to interact with the main menu.
            EventSystem.current.SetSelectedGameObject(transform.parent.GetComponentInChildren<PanelRaycaster>().gameObject);
            GameSettings.Instance.IsPauseMenuOpen = true;
        }

        static void OnResumePressed() => GameSettings.Instance.IsPauseMenuOpen = false;

        static void OnMainMenuPressed() => GameManager.Instance.ReturnToMainMenuAsync();

        static void OnQuitPressed() => GameManager.Instance.QuitAsync();
    }
}
