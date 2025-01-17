using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unity.Template.CompetitiveActionMultiplayer
{
    /// <summary>
    /// This class handles the cursor visibility during the game and mobile specific ActionInputs.
    /// </summary>
    public class InputSystemManager : MonoBehaviour
    {
        /// <summary>
        /// IsMobile returns True if Mobile Inputs should be enabled, false otherwise.
        /// </summary>
        /// <remarks>
        /// This field is a Task&lt;bool&gt; instead of a simple bool to avoid issues with script initialization order in Unity.
        /// </remarks>
        /// <example>
        /// The following example shows how to use this value from any other Monobehaviour.
        /// <code>
        /// <![CDATA[
        /// public class Example : MonoBehaviour
        /// {
        ///     async void OnEnable()
        ///     {
        ///         var isMobile = await InputSystemBehaviour.IsMobile;
        ///         if (isMobile)
        ///         {
        ///             // Do something for mobile handling...
        ///         }
        ///     }
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <seealso cref="Touchscreen"/>
        /// <seealso cref="MobileGamepadBehaviour"/>
        public static Task<bool> IsMobile => s_IsMobile.Task;
        static TaskCompletionSource<bool> s_IsMobile;

        [SerializeField]
        Volume m_PostProcessingVolume;

        [Header("Debug"), Tooltip("This option is only used in Playmode in the Editor")]
        public bool ForceMobileInput;

        FPSInputActions.UIActions m_UIInputs;
        FPSInputActions.GameplayActions m_GameplayInputs;

        VolumeComponent m_DepthOfField;

        /// <summary>
        /// This method makes sure that <see cref="s_IsMobile"/> is initialized when the game is started.
        /// </summary>
        /// <remarks>
        /// The setup is done in this method rather than in static constructors to ensure that multiple
        /// editor Playmode sessions will be initialized properly if assembly reloading is not performed between sessions.
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitializeStatic() => s_IsMobile = new TaskCompletionSource<bool>();

        void Awake()
        {
#if UNITY_EDITOR
            if (ForceMobileInput)
#endif
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
            {
                // On mobile, the GameInput Actions are filtered to only allow the Mobile Scheme the MobileGamepadController class is using.
                GameInput.Actions.Disable();
                GameInput.Actions.bindingMask = InputBinding.MaskByGroup(GameInput.Actions.MobileScheme.bindingGroup);
                GameInput.Actions.Enable();
                s_IsMobile.SetResult(true);

#if UNITY_EDITOR
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
#endif
            }
#endif
#if UNITY_EDITOR
            else
#endif
#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IOS)
            {
                s_IsMobile.SetResult(false);
            }
#endif
        }

        void Start()
        {
            m_UIInputs = GameInput.Actions.UI;
            m_GameplayInputs = GameInput.Actions.Gameplay;

            m_UIInputs.Disable();
            m_GameplayInputs.Disable();

            if(!m_PostProcessingVolume.profile.TryGet(typeof(DepthOfField), out m_DepthOfField))
                Debug.Log("Could not find Depth Of Field Post Processing Component, needed for UI Blur");
        }

        void LateUpdate()
        {
            var gameIsInUI = GameSettings.Instance.GameState != GlobalGameState.InGame ||
                             GameSettings.Instance.IsPauseMenuOpen ||
                             ConnectionSettings.Instance.GameConnectionState == GameConnectionState.Connecting;
            if (gameIsInUI && !m_UIInputs.enabled)
            {
                //Set virtual device state and style.
                MobileGamepadState.GetOrCreate.ButtonShoot = false;

                m_GameplayInputs.Disable();
                m_UIInputs.Enable();
            }
            if (!gameIsInUI && !m_GameplayInputs.enabled)
            {
                m_UIInputs.Disable();
                m_GameplayInputs.Enable();
            }

            if (!IsMobile.Result)
            {
                Cursor.visible = gameIsInUI;
                Cursor.lockState = Cursor.visible
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
            }
            else
            {
                if (GameSettings.Instance.GameState != GlobalGameState.InGame)
                    MobileGamepadState.GetOrCreate.ButtonAim = false;
            }

            m_DepthOfField.active = gameIsInUI;
        }
    }
}
