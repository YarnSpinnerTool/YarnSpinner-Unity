using System;
using System.Collections;
using UnityEngine;

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Yarn.Unity
{
    public class InputView : DialogueViewBase
    {
        internal enum ContinueActionType
        {
            None,
            KeyCode,
            InputSystemAction,
            InputSystemActionFromAsset,
        }

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

        LocalizedLine currentLine = null;

        public void Start()
        {
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            // If we are using an action reference, and it's not null,
            // configure it
            if (continueActionType == ContinueActionType.InputSystemActionFromAsset && continueActionReference != null)
            {
                continueActionReference.action.started += UserPerformedSkipAction;
            }

            // The custom skip action always starts disabled
            continueAction?.Disable();
            continueAction.started += UserPerformedSkipAction;
#endif
        }

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        void UserPerformedSkipAction(InputAction.CallbackContext obj)
        {
            OnContinueClicked();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.L))
            {
                FindObjectOfType<DialogueRunner>().InterruptLine();
                return;
            }
            
            // We need to be configured to use a keycode to interrupt/continue
            // lines.
            if (continueActionType != ContinueActionType.KeyCode)
            {
                return;
            }

            // That keycode needs to have been pressed this frame.
            if (!UnityEngine.Input.GetKeyDown(continueActionKeyCode))
            {
                return;
            }

            // We're good to indicate that we want to skip/continue.
            OnContinueClicked();
        }
#endif

        public override void DismissLine(Action onDismissalComplete)
        {
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            if (continueActionType == ContinueActionType.InputSystemAction)
            {
                continueAction?.Disable();
            }
            else if (continueActionType == ContinueActionType.InputSystemActionFromAsset)
            {
                continueActionReference?.action?.Disable();
            }
#endif
            currentLine = null;

            onDismissalComplete();
        }

        public override void InterruptLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            currentLine = dialogueLine;
            onDialogueLineFinished();
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            currentLine = dialogueLine;

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            // If we are using a custom Unity Input System action, enable
            // it now.
            if (continueActionType == ContinueActionType.InputSystemAction)
            {
                continueAction?.Enable();
            }
            else if (continueActionType == ContinueActionType.InputSystemActionFromAsset)
            {
                continueActionReference?.action.Enable();
            }
#endif
            onDialogueLineFinished();
        }

        public void OnContinueClicked()
        {
            if (currentLine == null)
            {
                // We're not actually displaying a line. No-op.
                return;
            }
            ReadyForNextLine();
        }
    }
}
