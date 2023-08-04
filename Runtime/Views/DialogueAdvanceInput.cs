using System;
using System.Collections;
using UnityEngine;

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Yarn.Unity
{
    /// <summary>
    /// A component that listens for user input, and uses it to notify a
    /// dialogue view that the user wishes to advance to the next step in the
    /// dialogue.
    /// </summary>
    /// <remarks>
    /// <para>This class may be used with the Unity Input System, or the legacy
    /// Input Manager. The specific type of input it's looking for is configured
    /// via the <see cref="continueActionType"/> field.</para>
    /// <para>When the configured input occurs, this component calls the <see
    /// cref="DialogueViewBase.UserRequestedViewAdvancement"/> method on its
    /// <see cref="dialogueView"/>.
    /// </para>
    /// </remarks>
    public class DialogueAdvanceInput : MonoBehaviour
    {
        /// <summary>
        /// The type of input that this component is listening for in order to signal that its dialogue view should advance.
        /// </summary>
        public enum ContinueActionType
        {
            /// <summary>
            /// The component is listening for no input. This component will not
            /// signal to <see cref="dialogueView"/> that it should advance.
            /// </summary>
            None,

            /// <summary>
            /// The component is listening for a key on the keyboard to be
            /// pressed.
            /// </summary>
            /// <remarks>
            /// <para style="info">This input will only be used if the legacy
            /// Input Manager is enabled.</para>
            /// </remarks>
            KeyCode,

            /// <summary>
            /// The component is listening for the action configured in <see
            /// cref="continueAction"/> to be performed.
            /// </summary>
            /// <remarks>
            /// <para style="info">This input will only be used if the Input
            /// System is installed and enabled.</para>
            /// </remarks>
            InputSystemAction,

            /// <summary>
            /// The component is listening for the action referred to by <see
            /// cref="continueActionReference"/> to be performed.
            /// </summary>
            /// <remarks>
            /// <para style="info">This input will only be used if the Input
            /// System is installed and enabled.</para>
            /// </remarks>
            InputSystemActionFromAsset,
        }

        /// <summary>
        /// The dialogue view that will be notified when the user performs the
        /// advance input (as configured by <see cref="continueActionType"/> and
        /// related fields.)
        /// </summary>
        /// <remarks>
        /// When the input is performed, this dialogue view will have its <see
        /// cref="DialogueViewBase.UserRequestedViewAdvancement"/> method
        /// called.
        /// </remarks>
        [SerializeField] public DialogueViewBase dialogueView;

        /// <summary>
        /// The type of input that this component is listening for.
        /// </summary>
        /// <seealso cref="ContinueActionType"/>
        [SerializeField]
        public ContinueActionType continueActionType = ContinueActionType.KeyCode;

        /// <summary>
        /// The keyboard key that this component is listening for.
        /// </summary>
        /// <remarks>
        /// <para style="info">
        /// This value is only used when <see cref="continueActionType"/> is
        /// <see cref="ContinueActionType.KeyCode"/>.
        /// </para>
        /// </remarks>
        [SerializeField]
        public KeyCode continueActionKeyCode = KeyCode.Space;

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        /// <summary>
        /// An <see cref="InputActionReference"/> that refers to the action that
        /// this component is listening for.
        /// </summary>
        /// <remarks>
        /// <para style="info">
        /// This value is only used when <see cref="continueActionType"/> is
        /// <see cref="ContinueActionType.InputSystemActionFromAsset"/>.
        /// </para>
        /// <para>
        /// Use this continue action type when you want this component to make
        /// use of an input action you've configured elsewhere in your project.
        /// </para>
        /// </remarks>
        [SerializeField]
        public InputActionReference continueActionReference = null;

        /// <summary>
        /// The <see cref="InputAction"/> that this component is listening for.
        /// </summary>
        /// <remarks>
        /// <para style="info">
        /// This value is only used when <see cref="continueActionType"/> is
        /// <see cref="ContinueActionType.InputSystemAction"/>.
        /// </para>
        /// <para>
        /// Use this continue action type when you want this component to use a
        /// configurable input action, but don't want to have to set up other
        /// actions in your project.
        /// </para>
        /// </remarks>
        [SerializeField]
        public InputAction continueAction = new InputAction("Skip", InputActionType.Button, CommonUsages.Submit);

        /// <summary>
        /// Configures whether <see cref="Action"/> should be enabled on start.
        /// </summary>
        /// <remarks>
        /// If this field is <see langword="false"/>, the input action specified
        /// by <see cref="Action"/> will not be enabled by default, and will
        /// need to be enabled by your game's code.
        /// </remarks>
        [SerializeField]
        public bool enableActionOnStart = true;

        /// <summary>
        /// Gets the <see cref="InputAction"/> configured by this <see
        /// cref="DialogueAdvanceInput"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This methods returns the following potential values:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// If <see cref="continueActionType"/> is <see
        /// cref="ContinueActionType.InputSystemAction"/>, this method returns
        /// <see cref="continueAction"/>.
        /// </item>
        /// <item>
        /// If <see cref="continueActionType"/> is <see
        /// cref="ContinueActionType.InputSystemActionFromAsset"/>, this method
        /// returns <see cref="continueActionReference"/>'s action.
        /// </item>
        /// <item>
        /// If <see cref="continueActionType"/> is <see
        /// cref="ContinueActionType.KeyCode"/> or <see
        /// cref="ContinueActionType.None"/>, this method returns <see
        /// langword="null"/>.
        /// </item>
        /// </list>
        /// </remarks>
        public InputAction Action
        {
            get
            {
                switch (continueActionType)
                {
                    case ContinueActionType.None:
                    case ContinueActionType.KeyCode:
                        return null;
                    case ContinueActionType.InputSystemAction:
                        return continueAction;
                    case ContinueActionType.InputSystemActionFromAsset:
                        return continueActionReference != null ? continueActionReference.action : null;
                    default:
                        throw new IndexOutOfRangeException($"Invalid continue action type {continueActionType}");
                }
            }
        }
#endif

        internal void Start()
        {
            if (dialogueView == null)
            {
                dialogueView = GetComponent<DialogueViewBase>();
            }

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            // If we have an input action, configure it and optionally enable it
            if (Action != null)
            {
                Action.performed += UserPerformedAdvanceAction;

                if (enableActionOnStart)
                {
                    Action.Enable();
                }
            }
#endif
        }

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        internal void OnDisable()
        {
            if (Action != null)
            {
                // if we have an action and are being turned off then need to disconnect the action
                // otherwise we have it as a dangling ref and will cause errors
                Action.performed -= UserPerformedAdvanceAction;
            }
        }
#endif


#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        private void UserPerformedAdvanceAction(InputAction.CallbackContext obj)
        {
            dialogueView.UserRequestedViewAdvancement();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        internal void Update()
        {
            // We need to be configured to use a keycode to interrupt/continue
            // lines.
            if (continueActionType != ContinueActionType.KeyCode)
            {
                return;
            }

            // Has the keycode been pressed this frame?
            if (Input.GetKeyUp(continueActionKeyCode))
            {
                // Indicate that we want to skip/continue.
                dialogueView.UserRequestedViewAdvancement();
            }
        }
#endif
    }
}
