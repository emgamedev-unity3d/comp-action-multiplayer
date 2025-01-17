using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>In-game crosshair behavior.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class Crosshair : MonoBehaviour
    {
        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.SetBinding("style.display", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(GameSettings.InGameUIPropertyName),
                bindingMode = BindingMode.ToTarget,
            });
        }
    }
}
