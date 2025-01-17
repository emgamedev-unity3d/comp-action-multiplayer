using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    [RequireComponent(typeof(UIDocument))]
    public class LoadingScreen : MonoBehaviour
    {
        static class UIElementNames
        {
            public const string LoadingStatus = "LoadingStatus";
        }

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            root.SetBinding("style.display", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(GameSettings.LoadingScreenStylePropertyName),
                bindingMode = BindingMode.ToTarget,
            });

            root.Q<ProgressBar>().SetBinding("value", new DataBinding
            {
                dataSource = LoadingData.Instance,
                dataSourcePath = new PropertyPath(LoadingData.LoadingProgressPropertyName),
                bindingMode = BindingMode.ToTarget,
            });

            var label = root.Q<Label>(UIElementNames.LoadingStatus);
            label.SetBinding("text", new DataBinding
            {
                dataSource = LoadingData.Instance,
                dataSourcePath = new PropertyPath(LoadingData.LoadingStatusTextPropertyName),
                bindingMode = BindingMode.ToTarget,
            });
        }
    }
}
