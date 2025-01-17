using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>In-game respawn screen.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class RespawnScreen : MonoBehaviour
    {
        static class UIElementNames
        {
            public const string RespawnMessageLabel = "RespawnMessage";
        }

        float m_Timer;
        bool m_ResetTimer = true;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            root.SetBinding("style.display", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(GameSettings.RespawnScreenStylePropertyName),
                bindingMode = BindingMode.ToTarget,
            });

            var respawnMessageText = root.Q<Label>(UIElementNames.RespawnMessageLabel);
            respawnMessageText.SetBinding("text", new DataBinding
            {
                dataSource = RespawnData.Instance,
                dataSourcePath = new PropertyPath(RespawnData.RespawnMessagePropertyName),
                bindingMode = BindingMode.ToTarget,
            });
        }

        void LateUpdate()
        {
            if (GameSettings.Instance.PlayerState == PlayerState.Dead)
            {
                m_ResetTimer = true;
                if (m_Timer > 0f)
                {
                    m_Timer -= Time.deltaTime;
                    if (m_Timer < 0f) m_Timer = 0f;
                }
                RespawnData.Instance.RespawnTimer = (int)math.ceil(m_Timer);
            }
            else if (m_ResetTimer)
            {
                if (ClientServerBootstrap.ClientWorld != null)
                {
                    var request = ClientServerBootstrap.ClientWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<GameResources>());
                    if (request.TryGetSingleton(out GameResources gameResources))
                    {
                        m_Timer = gameResources.RespawnTime;
                        RespawnData.Instance.RespawnTimer = (int)math.ceil(m_Timer);
                        m_ResetTimer = false;
                    }
                }
            }
        }
    }
}
