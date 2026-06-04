
using UnityEngine;
using Yarn.Unity.Attributes;

#nullable enable

namespace Yarn.Unity.LineAdvancerInput
{
    [RequireComponent(typeof(LineAdvancer))]
    public class InputActions : MonoBehaviour, ILineAdvancerInput
    {
        [Yarn.Unity.Attributes.MessageBox(nameof(GetAvailabilityMessage))]
        [SerializeField] LineAdvancer? lineAdvancer;

        public LineAdvancer? LineAdvancer { get => lineAdvancer; set => lineAdvancer = value; }

#pragma warning disable CS0162
        public MessageBoxAttribute.Message GetAvailabilityMessage()
        {
            if (!InputSystemAvailability.inputSystemInstalled)
            {
                return MessageBoxAttribute.Error("The Unity Input System package is not installed.");
            }
            if (!InputSystemAvailability.enableInputSystem)
            {
                return MessageBoxAttribute.Warning("The Unity Input System package is installed, but not enabled.");
            }
            return MessageBoxAttribute.NoMessage;
        }
#pragma warning restore

#if USE_INPUTSYSTEM
        /// <summary>
        /// The Input Action that triggers a request to advance to the next
        /// piece of content.
        /// </summary>

        public UnityEngine.InputSystem.InputActionReference? hurryUpLineAction;

        /// <summary>
        /// The Input Action that triggers an instruction to cancel the current
        /// line.
        /// </summary>
        [ShowIf(nameof(ShowNextLine))]
        public UnityEngine.InputSystem.InputActionReference? nextLineAction;

        /// <summary>
        /// The Input Action that triggers an instruction to hurry up presenting the current options
        /// </summary>

        public UnityEngine.InputSystem.InputActionReference? hurryUpOptionsAction;

        /// <summary>
        /// The Input Action that triggers an instruction to cancel the entire
        /// dialogue.
        /// </summary>

        public UnityEngine.InputSystem.InputActionReference? cancelDialogueAction;

        /// <summary>
        /// If true, the <see cref="hurryUpLineAction"/>, <see
        /// cref="nextLineAction"/> and <see cref="cancelDialogueAction"/> Input
        /// Actions will be enabled when the the dialogue runner signals that a
        /// line is running.
        /// </summary>

        public bool enableActions = true;

        private bool ShowNextLine => lineAdvancer != null && lineAdvancer.SeparateHurryUpAndAdvanceControls;

        public void OnDialogueStarted()
        {
            if (enableActions)
            {
                if (hurryUpLineAction != null) { hurryUpLineAction.action.Enable(); }
                if (hurryUpOptionsAction != null) { hurryUpOptionsAction.action.Enable(); }
                if (nextLineAction != null) { nextLineAction.action.Enable(); }
                if (cancelDialogueAction != null) { cancelDialogueAction.action.Enable(); }
            }

            // Register callbacks to run when our actions are performed.
            if (hurryUpLineAction != null) { hurryUpLineAction.action.performed += OnHurryUpLinePerformed; }
            if (hurryUpOptionsAction != null) { hurryUpOptionsAction.action.performed += OnHurryUpOptionsPerformed; }
            if (nextLineAction != null) { nextLineAction.action.performed += OnNextLinePerformed; }
            if (cancelDialogueAction != null) { cancelDialogueAction.action.performed += OnCancelDialoguePerformed; }
        }

        public void OnDialogueComplete()
        {
            // Deregister callbacks
            if (hurryUpLineAction != null) { hurryUpLineAction.action.performed -= OnHurryUpLinePerformed; }
            if (hurryUpOptionsAction != null) { hurryUpOptionsAction.action.performed -= OnHurryUpOptionsPerformed; }
            if (nextLineAction != null) { nextLineAction.action.performed -= OnNextLinePerformed; }
            if (cancelDialogueAction != null) { cancelDialogueAction.action.performed -= OnCancelDialoguePerformed; }
        }

        private void OnHurryUpLinePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (LineAdvancer != null) { LineAdvancer.OnInputHurryUpLines(); }

        }
        private void OnHurryUpOptionsPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (LineAdvancer != null) { LineAdvancer.OnInputHurryUpOptions(); }
        }

        private void OnNextLinePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (LineAdvancer != null) { LineAdvancer.OnInputNextContent(); }
        }
        private void OnCancelDialoguePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (LineAdvancer != null) { LineAdvancer.OnInputCancelDialogue(); }
        }
#endif
    }

}
