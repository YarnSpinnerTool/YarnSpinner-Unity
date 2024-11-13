using System.Threading.Tasks;
using UnityEngine;
using Yarn.Unity;

#nullable enable

#if USE_UNITASK
using Cysharp.Threading.Tasks;
using YarnTask = Cysharp.Threading.Tasks.UniTask;
using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.DialogueOption?>;
#else
using System.Threading;
using YarnTask = System.Threading.Tasks.Task;
using YarnOptionTask = System.Threading.Tasks.Task<Yarn.Unity.DialogueOption?>;
#endif

namespace Yarn.Unity
{
    public class LineAdvancer : AsyncDialogueViewBase
    {
        [MustNotBeNull]
        [Tooltip("The dialogue runner that will receive requests to advance or cancel content.")]
        [SerializeField] DialogueRunner runner;

        [Space]
        [Tooltip("Does repeatedly requesting a line advance cancel the line?")]
        public bool multiAdvanceIsCancel = false;

        [ShowIf(nameof(multiAdvanceIsCancel))]
        [Indent]
        [Label("Advance Count")]
        [Tooltip("The number of times that a line advance occurs before the current line is cancelled.")]
        public int advanceRequestsBeforeCancellingLine = 2;

        /// <summary>
        /// The number of times that this object has received an indication that
        /// the line should be advanced.
        /// </summary>
        /// <remarks>
        /// This value is reset to zero when a new line is run. When the line is
        /// advanced, this value is incremented. If this value ever meets or
        /// exceeds <see cref="advanceRequestsBeforeCancellingLine"/>, the line
        /// will be cancelled.
        /// </remarks>
        private int numberOfAdvancesThisLine = 0;

        /// <summary>
        /// The type of input that this line advancer responds to.
        /// </summary>
        public enum InputMode
        {
            /// <summary>
            /// The line advancer responds to Input Actions from the <a
            /// href="https://docs.unity3d.com/Packages/com.unity.inputsystem@latest">Unity
            /// Input System</a>.
            /// </summary>
            InputActions,
            /// <summary>
            /// The line advancer responds to keypresses on the keyboard.
            /// </summary>
            KeyCodes,
            /// <summary>
            /// The line advancer does not respond to any input.
            /// </summary>
            /// <remarks>When a line advancer's <see cref="inputMode"/> is set
            /// to <see cref="None"/>, call the <see
            /// cref="RequestLineAdvancement"/>, <see
            /// cref="RequestLineCancellation"/> and <see
            /// cref="RequestDialogueCancellation"/> methods directly from your
            /// code to control line advancement.
            None,
            /// <summary>
            /// The line advancer responds to input from the legacy <a
            /// href="https://docs.unity3d.com/Manual/class-InputManager.html">Input
            /// Manager</a>.
            /// </summary>
            LegacyInputAxes,
        }

        /// <summary>
        /// The type of input that this line advancer responds to.
        /// </summary>
        /// <seealso cref="InputMode"/>
        [Tooltip("The type of input that this line advancer responds to.")]
        [Space]
        [MessageBox(sourceMethod: nameof(ValidateInputMode))]
        [SerializeField] InputMode inputMode;

        /// <summary>
        /// Validates the current value of <see cref="inputMode"/>, and
        /// potentially returns a message box to display.
        /// </summary>
        private MessageBoxAttribute.Message ValidateInputMode()
        {
#pragma warning disable CS0162 // Unreachable code detected

#if USE_INPUTSYSTEM
            const bool inputSystemInstalled = true;
#else
            const bool inputSystemInstalled = false;
#endif

#if ENABLE_INPUT_SYSTEM
            const bool enableInputSystem = true;
#else
            const bool enableInputSystem = false;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            const bool enableLegacyInput = true;
#else
            const bool enableLegacyInput = false;
#endif

            if (this.inputMode == InputMode.None)
            {
                return MessageBoxAttribute.Info($"To use this component, call the following methods on it:\n\n" +
                    $"- {nameof(this.RequestLineAdvancement)}()\n" +
                    $"- {nameof(this.RequestLineCancellation)}()\n" +
                    $"- {nameof(this.RequestDialogueCancellation)}()"
                );
            }

            if (this.inputMode == InputMode.LegacyInputAxes && !enableLegacyInput)
            {
                return MessageBoxAttribute.Warning("The Input Manager (Old) system is not enabled.\n\nEither change this setting to Input Actions, or enable Input Manager (Old) in Project Settings > Player > Configuration > Active Input Handling.");
            }

            if (this.inputMode == InputMode.InputActions)
            {
                if (inputSystemInstalled == false)
                {
                    return MessageBoxAttribute.Warning("Please install the Unity Input System package.");
                }
                if (!enableInputSystem)
                {
                    return MessageBoxAttribute.Warning("The Input System is not enabled.\n\nEither change this setting, or enable Input System in Project Settings > Player > Configuration > Active Input Handling.");
                }
            }

            return MessageBoxAttribute.NoMessage;
#pragma warning restore CS0162 // Unreachable code detected
        }

#if USE_INPUTSYSTEM
        /// <summary>
        /// The Input Action that triggers a request to advance to the next piece of content.
        /// </summary>
        [ShowIf(nameof(inputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? advanceLineAction;

        /// <summary>
        /// The Input Action that triggers an instruction to cancel the current line.
        /// </summary>
        [ShowIf(nameof(inputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? cancelLineAction;

        /// <summary>
        /// The Input Action that triggers an instruction to cancel the entire dialogue.
        /// </summary>
        [ShowIf(nameof(inputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? cancelDialogueAction;

        /// <summary>
        /// If true, the <see cref="advanceLineAction"/>, <see
        /// cref="cancelLineAction"/> and <see cref="cancelDialogueAction"/>
        /// Input Actions will be enabled when the the dialogue runner signals
        /// that a line is running.
        /// </summary>
        [Tooltip("If true, the input actions above will be enabled when a line begins.")]
        [ShowIf(nameof(inputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] bool enableActions = true;
#endif
        /// <summary>
        /// The legacy Input Axis that triggers a request to advance to the next piece of content.
        /// </summary>
        [ShowIf(nameof(inputMode), InputMode.LegacyInputAxes)]
        [Indent]
        [SerializeField] string? skipLineAxis = "Jump";
        /// <summary>
        /// The legacy Input Axis that triggers an instruction to cancel the current line.
        /// </summary>
        [ShowIf(nameof(inputMode), InputMode.LegacyInputAxes)]
        [Indent]
        [SerializeField] string? cancelLineAxis = "Cancel";
        /// <summary>
        /// The legacy Input Axis that triggers an instruction to cancel the entire dialogue.
        /// </summary>
        [ShowIf(nameof(inputMode), InputMode.LegacyInputAxes)]
        [Indent]
        [SerializeField] string? cancelDialogueAxis = "";


        /// <summary>
        /// The <see cref="KeyCode"/> that triggers a request to advance to the next piece of content.
        /// </summary>
        [ShowIf(nameof(inputMode), InputMode.KeyCodes)]
        [Indent]
        [SerializeField] KeyCode skipLineKeyCode = KeyCode.Space;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to cancel the current line.
        /// </summary>
        [ShowIf(nameof(inputMode), InputMode.KeyCodes)]
        [Indent]
        [SerializeField] KeyCode cancelLineKeyCode = KeyCode.Escape;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to cancel the entire dialogue.
        /// </summary>
        [ShowIf(nameof(inputMode), InputMode.KeyCodes)]
        [Indent]
        [SerializeField] KeyCode cancelDialogueKeyCode = KeyCode.None;

#if USE_INPUTSYSTEM
        private void OnSkipLinePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            RequestLineAdvancement();
        }
        private void OnCancelLinePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            RequestLineCancellation();
        }
        private void OnCancelDialoguePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            RequestDialogueCancellation();
        }
#endif

        /// <inheritdoc/>
        public override YarnTask OnDialogueStartedAsync()
        {
#if USE_INPUTSYSTEM
            if (inputMode == InputMode.InputActions)
            {
                // If we're using the input system, register callbacks to run when our actions are performed.
                if (advanceLineAction != null) { advanceLineAction.action.performed += OnSkipLinePerformed; }
                if (cancelLineAction != null) { cancelLineAction.action.performed += OnCancelLinePerformed; }
                if (cancelDialogueAction != null) { cancelDialogueAction.action.performed += OnCancelDialoguePerformed; }
            }
#endif

            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
#if USE_INPUTSYSTEM
            // If we're using the input system, remove the callbacks.
            if (inputMode == InputMode.InputActions)
            {
                if (advanceLineAction != null) { advanceLineAction.action.performed -= OnSkipLinePerformed; }
                if (cancelLineAction != null) { cancelLineAction.action.performed -= OnCancelLinePerformed; }
                if (cancelDialogueAction != null) { cancelDialogueAction.action.performed -= OnCancelDialoguePerformed; }
            }
#endif

            return YarnTask.CompletedTask;
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            // A new line has come in, so reset the number of times we've seen a
            // request to skip.
            numberOfAdvancesThisLine = 0;

#if USE_INPUTSYSTEM
            if (enableActions)
            {
                if (advanceLineAction != null) { advanceLineAction.action.Enable(); }
                if (cancelLineAction != null) { cancelLineAction.action.Enable(); }
                if (cancelDialogueAction != null) { cancelDialogueAction.action.Enable(); }
            }
#endif

            return YarnTask.CompletedTask;
        }

        public override YarnOptionTask RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            // This line view doesn't take any actions when options are presented.
            return YarnAsync.NoOptionSelected;
        }

        public void RequestLineAdvancement()
        {
            // Increment our counter of line advancements, and depending on the new
            // count, request that the runner 'soft-cancel' the line or cancel the
            // entire line

            numberOfAdvancesThisLine += 1;
            if (multiAdvanceIsCancel && numberOfAdvancesThisLine >= advanceRequestsBeforeCancellingLine)
            {
                RequestLineCancellation();
            }
            else
            {
                runner.RequestHurryUpLine();
            }
        }

        public void RequestLineCancellation()
        {
            // Request that the runner cancel the entire line
            runner.RequestNextLine();
        }

        public void RequestDialogueCancellation()
        {
            // Stop the dialogue runner, which will cancel the current line as well
            // as the entire dialogue.
            runner.Stop();
        }

        public void Update()
        {
            switch (inputMode)
            {
                case InputMode.KeyCodes:
                    if (Input.GetKeyDown(skipLineKeyCode)) { this.RequestLineAdvancement(); }
                    if (Input.GetKeyDown(cancelLineKeyCode)) { this.RequestLineCancellation(); }
                    if (Input.GetKeyDown(cancelDialogueKeyCode)) { this.RequestDialogueCancellation(); }
                    break;
                case InputMode.LegacyInputAxes:
                    if (string.IsNullOrEmpty(skipLineAxis) == false && Input.GetButtonDown(skipLineAxis)) { this.RequestLineAdvancement(); }
                    if (string.IsNullOrEmpty(cancelLineAxis) == false && Input.GetButtonDown(cancelLineAxis)) { this.RequestLineCancellation(); }
                    if (string.IsNullOrEmpty(cancelDialogueAxis) == false && Input.GetButtonDown(cancelDialogueAxis)) { this.RequestDialogueCancellation(); }
                    break;
                default:
                    // Nothing to do; 'None' takes no action, and 'InputActions'
                    // doesn't poll in Update()
                    break;
            }
        }
    }

}
