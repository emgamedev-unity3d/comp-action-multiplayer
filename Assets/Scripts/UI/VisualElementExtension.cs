using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    static class VisualElementExtension
    {
        public static void RegisterTextFieldInputCallbacks(this VisualElement root)
        {
            root.RegisterCallback<FocusInEvent>(OnFocusInTextField);
            root.RegisterCallback<FocusOutEvent>(OnFocusOutTextField);
        }
        public static void UnregisterTextFieldInputCallbacks(this VisualElement root)
        {
            root.UnregisterCallback<FocusInEvent>(OnFocusInTextField);
            root.UnregisterCallback<FocusOutEvent>(OnFocusOutTextField);
        }

        static void OnFocusInTextField(FocusInEvent evt)
        {
            if (evt.target is TextElement)
                GameInput.Actions.DebugActions.Disable();
        }
        static void OnFocusOutTextField(FocusOutEvent evt)
        {
            if (evt.target is TextElement)
                GameInput.Actions.DebugActions.Enable();
        }
    }
}
