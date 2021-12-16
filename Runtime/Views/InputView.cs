using System;
using System.Collections;
using UnityEngine;

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Yarn.Unity
{
    public class InputView : MonoBehaviour
    {
        internal enum ContinueActionType
        {
            None,
            KeyCode,
            InputSystemAction,
            InputSystemActionFromAsset,
        }

        // the dialogue view that will be told about user skipping events
        [SerializeField] private DialogueViewBase attachedView;

        [SerializeField]
        internal ContinueActionType continueActionType;

        [SerializeField]
        internal KeyCode continueActionKeyCode = KeyCode.Escape;


#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        [SerializeField]
        internal InputActionReference continueActionReference = null;

        [SerializeField]
        internal InputAction continueAction = new InputAction("Skip", InputActionType.Button, CommonUsages.Cancel);
#endif

        public void Start()
        {
            if (attachedView == null)
            {
                attachedView = GetComponent<DialogueViewBase>();
            }
        }

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        void UserPerformedSkipAction(InputAction.CallbackContext obj)
        {
            attachedView.UserRequestedViewAdvancement();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        public void Update()
        {
            // We need to be configured to use a keycode to interrupt/continue
            // lines.
            if (continueActionType != ContinueActionType.KeyCode)
            {
                return;
            }

            // That keycode needs to have been pressed this frame.
            if (!UnityEngine.Input.GetKeyUp(continueActionKeyCode))
            {
                return;
            }

            // We're good to indicate that we want to skip/continue.
            attachedView.UserRequestedViewAdvancement();
        }
#endif
    }
}
