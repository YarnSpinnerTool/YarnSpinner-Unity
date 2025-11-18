/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity.Attributes;

#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = Yarn.Unity.TMPShim;
using TMP_Text = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity
{
    public static class InputSystemAvailability
    {
#if USE_INPUTSYSTEM
        internal const bool inputSystemInstalled = true;
#else
        internal const bool inputSystemInstalled = false;
#endif

#if ENABLE_INPUT_SYSTEM
        internal const bool enableInputSystem = true;
#else
        internal const bool enableInputSystem = false;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        internal const bool enableLegacyInput = true;
#else
        internal const bool enableLegacyInput = false;
#endif

#if !ENABLE_LEGACY_INPUT_MANAGER
        /// <summary>
        /// A dictionary mapping legacy keycodes to Input System keys.
        /// </summary>
        static System.Lazy<Dictionary<KeyCode, UnityEngine.InputSystem.Key>> lookup = new(() =>
        {
            var result = new Dictionary<KeyCode, UnityEngine.InputSystem.Key>();
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                // Attempt to automatically find the equivalent of keyCode by
                // assuming that the string representation of a key (e.g. "Tab")
                // is the same in both enums.
                if (System.Enum.TryParse<UnityEngine.InputSystem.Key>(keyCode.ToString(), true, out var value))
                {
                    result[keyCode] = value;
                }
            }
            // Manually map some remaining keys
            result[KeyCode.Return] = UnityEngine.InputSystem.Key.Enter;
            result[KeyCode.KeypadEnter] = UnityEngine.InputSystem.Key.NumpadEnter;
            return result;
        });
#endif

        /// <summary>
        /// Gets a value indicating whether the key indicated by a <see
        /// cref="KeyCode"/> was pressed this frame.
        /// </summary>
        /// <remarks>
        /// If the Legacy Input Manager is enabled, this method wraps <see
        /// cref="Input.GetKeyDown"/>. Otherwise, it attempts to find the <see
        /// cref="UnityEngine.InputSystem.Key"/> equivalent of <paramref
        /// name="key"/>, and then checks with <see
        /// cref="UnityEngine.InputSystem.Keyboard.current"/> to find the key,
        /// and queries its <see
        /// cref="UnityEngine.InputSystem.Controls.ButtonControl.wasPressedThisFrame"/>
        /// property.
        /// </remarks>
        /// <param name="key">The <see cref="KeyCode"/> to check for the state
        /// of.</param>
        /// <returns>Whether the key was pressed this frame.</returns>
        public static bool GetKeyDown(KeyCode key)
        {
            if (key == KeyCode.None)
            {
                // The 'none' key is never pressed
                return false;
            }
#if  ENABLE_LEGACY_INPUT_MANAGER
            // If we're using Legacy Input, read from it directly
            return Input.GetKeyDown(key);
#else

            if (lookup.Value.TryGetValue(key, out var lookupKey))
            {
                try
                {
                    return UnityEngine.InputSystem.Keyboard.current[lookup.Value[key]].wasPressedThisFrame;

                }
                catch (System.ArgumentOutOfRangeException)
                {
#if DEBUG
                    Debug.LogWarning($"Can't check if {key} is down: found Input System mapping {lookupKey}, but this key is not present in the current keyboard");
#endif
                    return false;
                }
            }
            else
            {
#if DEBUG
                Debug.LogWarning($"Can't check if {key} is down: can't find a mapping from legacy keycode {key} to Unity Input System");
#endif
                return false;
            }
#endif
        }

        public static bool GetButtonDown(string? buttonName)
        {
            if (buttonName == null)
            {
                return false;
            }
#if  ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetButtonUp(buttonName);
#else
            return false;
#endif
        }

        public static float GetAxis(string? axisName)
        {
            if (axisName == null)
            {
                return 0;
            }
#if  ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxis(axisName);
#else
            return 0;
#endif
        }
    }

    /// <summary>
    /// A dialogue presenter that listens for user input and sends requests to a <see
    /// cref="DialogueRunner"/> to advance the presentation of the current line,
    /// either by asking a dialogue runner to hurry up its delivery, advance to
    /// the next line, or cancel the entire dialogue session.
    /// </summary>
    public sealed class LineAdvancer : DialoguePresenterBase, IActionMarkupHandler
    {
        [MustNotBeNull("Line Advancer needs to know which Dialogue Runner should be told to tell it to show the next line.")]
        [Tooltip("The dialogue runner that will receive requests to advance or cancel content.")]
        [SerializeField] DialogueRunner? runner;

        /// <summary>
        /// The <see cref="DialoguePresenterBase"/> that this LineAdvancer should subscribe to for notifications that the line is fully visible.
        /// </summary>
        /// <remarks>When <see cref="RequestLineHurryUp"/> is called, if the line is fully visible, the <see cref="runner"/> object will have its <see cref="DialogueRunner.RequestNextLine"/> method called (instead of its <see cref="DialogueRunner.RequestHurryUpLine"/> method).
        /// This behaviour is only the case when the <see cref="separateHurryUpAndAdvanceControls"/> is set to false.
        ///</remarks>
        [SerializeField] DialoguePresenterBase? presenter;

        /// <summary>
        /// Should this line advancer use different actions for hurrying up a line and advancing a line?
        /// </summary>
        /// <remarks>
        /// When this is false if the player requests a line to hurry up and the line is fully shown the <see cref="DialogueRunner.RequestNextLine"/> method will be called instead of the <see cref="DialogueRunner.RequestHurryUpLine"/> method.
        /// This behaviour is only the case when <see cref="presenter"/> is not null and the presenter is presenting it's line content via it's <see cref="DialoguePresenter.Typewriter"/> property.
        /// </remarks>
        [SerializeField] private bool separateHurryUpAndAdvanceControls = false;

        /// <summary>
        /// If <see langword="true"/>, repeatedly signalling that the line
        /// should be hurried up will cause the line advancer to request that
        /// the next line be shown.
        /// </summary>
        /// <seealso cref="advanceRequestsBeforeCancellingLine"/>
        [Space]
        [Tooltip("Does repeatedly requesting a line advance cancel the line?")]
        public bool multiAdvanceIsCancel = false;

        /// <summary>
        /// The number of times that a 'hurry up' signal occurs before the line
        /// advancer requests that the next line be shown.
        /// </summary>
        /// <seealso cref="multiAdvanceIsCancel"/>
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
            /// <remarks>When a line advancer's <see cref="UsedInputMode"/> is set
            /// to <see cref="None"/>, call the <see
            /// cref="RequestLineHurryUp"/>, <see cref="RequestNextLine"/> and
            /// <see cref="RequestDialogueCancellation"/> methods directly from
            /// your code to control line advancement.</remarks>
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

        // when using the same input for different actions, for example using spacebar to select an option but also spacebar to hurry up lines
        // the action for hurrying up the line will happen the same frame as the action for selection
        // so if a line follows options (very common), that line might well get told to instantly hurry up
        // which isn't ideal, so this tracks the frame that content arrives and hurry up events cannot run the same frame as their content appears
        private int frameContentReceived = 0;

        InputMode UsedInputMode
        {
            get
            {
                const bool inputSystemAvailable = InputSystemAvailability.enableInputSystem && InputSystemAvailability.inputSystemInstalled;

                if (inputMode == InputMode.InputActions && !inputSystemAvailable)
                {
                    // We're configured to use input actions, but the input
                    // system is not enabled. Fall back to key codes.
                    return InputMode.KeyCodes;
                }
                else
                {
                    return inputMode;
                }
            }
        }

        /// <summary>
        /// Validates the current value of <see cref="inputMode"/>, and
        /// potentially returns a message box to display.
        /// </summary>
        private MessageBoxAttribute.Message ValidateInputMode()
        {
#pragma warning disable CS0162 // Unreachable code detected
            if (this.inputMode == InputMode.None)
            {
                return MessageBoxAttribute.Info($"To use this component, call the following methods on it:\n\n" +
                    $"- {nameof(this.RequestLineHurryUp)}()\n" +
                    $"- {nameof(this.RequestNextLine)}()\n" +
                    $"- {nameof(this.RequestOptionHurryUp)}()\n" +
                    $"- {nameof(this.RequestDialogueCancellation)}()"
                );
            }

            if (this.inputMode == InputMode.LegacyInputAxes && !InputSystemAvailability.enableLegacyInput)
            {
                return MessageBoxAttribute.Warning("The Input Manager (Old) system is not enabled.\n\nEither change this setting to Input Actions, or enable Input Manager (Old) in Project Settings > Player > Configuration > Active Input Handling.");
            }

            if (this.inputMode == InputMode.InputActions)
            {
                if (InputSystemAvailability.inputSystemInstalled == false)
                {
                    return MessageBoxAttribute.Warning("Please install the Unity Input System package to use Input Actions.\n\nFalling back to the keyboard in the meantime.");
                }
                if (!InputSystemAvailability.enableInputSystem)
                {
                    return MessageBoxAttribute.Warning("The Unity Input System is not enabled.\n\nEither change this setting, or enable Input System in Project Settings > Player > Configuration > Active Input Handling.\n\nFalling back to the keyboard in the meantime.");
                }
            }

            return MessageBoxAttribute.NoMessage;
#pragma warning restore CS0162 // Unreachable code detected
        }

#if USE_INPUTSYSTEM
        /// <summary>
        /// The Input Action that triggers a request to advance to the next
        /// piece of content.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? hurryUpLineAction;

        /// <summary>
        /// The Input Action that triggers an instruction to cancel the current
        /// line.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.InputActions)]
        [ShowIf(nameof(separateHurryUpAndAdvanceControls))]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? nextLineAction;

        /// <summary>
        /// The Input Action that triggers an instruction to hurry up presenting the current options
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? hurryUpOptionsAction;

        /// <summary>
        /// The Input Action that triggers an instruction to cancel the entire
        /// dialogue.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? cancelDialogueAction;

        /// <summary>
        /// If true, the <see cref="hurryUpLineAction"/>, <see
        /// cref="nextLineAction"/> and <see cref="cancelDialogueAction"/> Input
        /// Actions will be enabled when the the dialogue runner signals that a
        /// line is running.
        /// </summary>
        [Tooltip("If true, the input actions above will be enabled when a line begins.")]
        [ShowIf(nameof(UsedInputMode), InputMode.InputActions)]
        [Indent]
        [SerializeField] bool enableActions = true;
#endif
        /// <summary>
        /// The legacy Input Axis that triggers a request to advance to the next
        /// piece of content.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.LegacyInputAxes)]
        [Indent]
        [SerializeField] string? hurryUpLineAxis = "Jump";

        /// <summary>
        /// The legacy Input Axis that triggers an instruction to cancel the
        /// current line.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.LegacyInputAxes)]
        [ShowIf(nameof(separateHurryUpAndAdvanceControls))]
        [Indent]
        [SerializeField] string? nextLineAxis = "Cancel";

        /// <summary>
        /// The legacy Input Axis that triggers an instruction to hurry up presenting the current options
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.LegacyInputAxes)]
        [Indent]
        [SerializeField] string? hurryUpOptionsAxis = "Jump";

        /// <summary>
        /// The legacy Input Axis that triggers an instruction to cancel the
        /// entire dialogue.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.LegacyInputAxes)]
        [Indent]
        [SerializeField] string? cancelDialogueAxis = "";

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers a request to advance to the
        /// next piece of content.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.KeyCodes)]
        [Indent]
        [SerializeField] KeyCode hurryUpLineKeyCode = KeyCode.Space;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to cancel the
        /// current line.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.KeyCodes)]
        [ShowIf(nameof(separateHurryUpAndAdvanceControls))]
        [Indent]
        [SerializeField] KeyCode nextLineKeyCode = KeyCode.Escape;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to hurry up presenting options
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.KeyCodes)]
        [Indent]
        [SerializeField] KeyCode hurryUpOptionsKeyCode = KeyCode.Space;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to cancel the
        /// entire dialogue.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.KeyCodes)]
        [Indent]
        [SerializeField] KeyCode cancelDialogueKeyCode = KeyCode.None;

#if USE_INPUTSYSTEM
        private void OnHurryUpLinePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            RequestLineHurryUpInternal();
        }
        private void OnHurryUpOptionsPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            RequestOptionHurryUp();
        }

        private void OnNextLinePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            RequestNextLine();
        }
        private void OnCancelDialoguePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            RequestDialogueCancellation();
        }
#endif
        // used to track the status of the presentation
        // you can think of this as a variation on multiple presses to advance a line
        // where if the presenter is awaiting input it is reasonable that pressing hurry up would advance to the next piece of content
        // but the default presenters can't really tell that apart
        // so the line advancer instead will handle this
        // this only works if the line advancer is added as a processor onto the presenters typewriter
        // but that is ok as that is the default
        // as people replace those defaults with more complex views and presenters they will also want to replace the line advancer anyways
        private enum PresentationStatus
        {
            Unknown, LineBegan, LineWaiting, OptionsBegan, OptionsWaiting,
        }
        private PresentationStatus status = PresentationStatus.Unknown;

        private void Start()
        {
            // If we have a dialogue presenter configured, register ourselves as
            // a temporal processor, so that we get notified when the line is
            // fully visible. This is so that when a line is fully visible, the
            // 'hurry up' action will instead trigger a 'next line' action,
            // (because there's nothing left to hurry up.)
            if (runner == null || presenter == null)
            {
                return;
            }
            if (!separateHurryUpAndAdvanceControls)
            {
                var listOfPresenters = new List<DialoguePresenterBase?>(runner.DialoguePresenters)
                {
                    this
                };
                runner.DialoguePresenters = listOfPresenters;
                presenter.Typewriter?.ActionMarkupHandlers.Add(this);

                // last thing is to null out the inputs just in case
                nextLineAxis = null;
                nextLineKeyCode = KeyCode.None;
#if USE_INPUTSYSTEM
                nextLineAction = null;
#endif
            }
        }

        /// <summary>
        /// Called by a dialogue runner when dialogue starts to add input action
        /// handlers for advancing the line.
        /// </summary>
        /// <returns>A completed task.</returns>
        public override YarnTask OnDialogueStartedAsync()
        {
#if USE_INPUTSYSTEM
            if (enableActions)
            {
                if (hurryUpLineAction != null) { hurryUpLineAction.action.Enable(); }
                if (hurryUpOptionsAction != null) { hurryUpOptionsAction.action.Enable(); }
                if (nextLineAction != null) { nextLineAction.action.Enable(); }
                if (cancelDialogueAction != null) { cancelDialogueAction.action.Enable(); }
            }

            if (UsedInputMode == InputMode.InputActions)
            {
                // If we're using the input system, register callbacks to run
                // when our actions are performed.
                if (hurryUpLineAction != null) { hurryUpLineAction.action.performed += OnHurryUpLinePerformed; }
                if (hurryUpOptionsAction != null) { hurryUpOptionsAction.action.performed += OnHurryUpOptionsPerformed; }
                if (nextLineAction != null) { nextLineAction.action.performed += OnNextLinePerformed; }
                if (cancelDialogueAction != null) { cancelDialogueAction.action.performed += OnCancelDialoguePerformed; }
            }
#endif

            ResetLineTracking();
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Called by a dialogue runner when dialogue ends to remove the input
        /// action handlers.
        /// </summary>
        /// <returns>A completed task.</returns>
        public override YarnTask OnDialogueCompleteAsync()
        {
#if USE_INPUTSYSTEM
            // If we're using the input system, remove the callbacks.
            if (UsedInputMode == InputMode.InputActions)
            {
                if (hurryUpLineAction != null) { hurryUpLineAction.action.performed -= OnHurryUpLinePerformed; }
                if (hurryUpOptionsAction != null) { hurryUpOptionsAction.action.performed -= OnHurryUpOptionsPerformed; }
                if (nextLineAction != null) { nextLineAction.action.performed -= OnNextLinePerformed; }
                if (cancelDialogueAction != null) { cancelDialogueAction.action.performed -= OnCancelDialoguePerformed; }
            }
#endif

            ResetLineTracking();
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Called by a dialogue presenter to signal that a line is running.
        /// </summary>
        /// <inheritdoc cref="LinePresenter.RunLineAsync" path="/param"/>
        /// <returns>A completed task.</returns>
        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            // A new line has come in, so reset the number of times we've seen a
            // request to skip.
            ResetLineTracking();
            status = PresentationStatus.LineBegan;

            frameContentReceived = Time.frameCount;

            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Called by a dialogue presenter to signal that options are running.
        /// </summary>
        /// <inheritdoc cref="LinePresenter.RunOptionsAsync" path="/param"/>
        /// <returns>A completed task indicating that no option was selected by
        /// this view.</returns>
        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            ResetLineTracking();
            status = PresentationStatus.OptionsBegan;

            frameContentReceived = Time.frameCount;

            return DialogueRunner.NoOptionSelected;
        }

        private void ResetLineTracking()
        {
            numberOfAdvancesThisLine = 0;
            status = PresentationStatus.Unknown;
        }


        private void RequestLineHurryUpInternal()
        {
            if (frameContentReceived == Time.frameCount)
            {
                return;
            }

            // in this mode we NEED to be in a state where a line showing, regardless of it's completion state
            if (!separateHurryUpAndAdvanceControls)
            {
                if (!(status == PresentationStatus.LineBegan || status == PresentationStatus.LineWaiting))
                {
                    return;
                }
            }

            // Increment our counter of line advancements, and depending on the
            // new count, request that the runner 'soft-cancel' the line or
            // cancel the entire line
            // this is true regardless of if we are the hurry up mode or not

            numberOfAdvancesThisLine += 1;

            if (multiAdvanceIsCancel && numberOfAdvancesThisLine >= advanceRequestsBeforeCancellingLine)
            {
                RequestNextLine();
            }
            else
            {
                // at this stage we want to hurry up if we are in multiAdvanceIsCancel
                // and either hurry up or skip the line depending on the state 
                if (separateHurryUpAndAdvanceControls)
                {
                    if (runner != null)
                    {
                        runner.RequestHurryUpLine();
                    }
                    else
                    {
                        Debug.LogError($"{nameof(LineAdvancer)} dialogue runner is null", this);
                    }
                }
                else
                {
                    if (status == PresentationStatus.LineWaiting)
                    {
                        RequestNextLine();
                    }
                    else
                    {
                        if (runner != null)
                        {
                            runner.RequestHurryUpLine();
                        }
                        else
                        {
                            Debug.LogError($"{nameof(LineAdvancer)} dialogue runner is null", this);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Requests that the line be hurried up.
        /// </summary>
        /// <remarks>If this method has been called more times for a single line
        /// than <see cref="numberOfAdvancesThisLine"/>, this method requests
        /// that the dialogue runner proceed to the next line. Otherwise, it
        /// requests that the dialogue runner instruct all line views to hurry
        /// up their presentation of the current line.
        /// </remarks>
        public void RequestLineHurryUp()
        {
            // Increment our counter of line advancements, and depending on the
            // new count, request that the runner 'soft-cancel' the line or
            // cancel the entire line

            numberOfAdvancesThisLine += 1;

            if (multiAdvanceIsCancel && numberOfAdvancesThisLine >= advanceRequestsBeforeCancellingLine)
            {
                RequestNextLine();
            }
            else
            {
                if (runner != null)
                {
                    runner.RequestHurryUpLine();
                }
                else
                {
                    Debug.LogError($"{nameof(LineAdvancer)} dialogue runner is null", this);
                }
            }
        }

        public void RequestOptionHurryUp()
        {
            if (frameContentReceived == Time.frameCount)
            {
                return;
            }

            if (runner == null)
            {
                Debug.LogError($"Unable to hurry up options, {nameof(LineAdvancer)} dialogue runner is null", this);
                return;
            }

            if (!separateHurryUpAndAdvanceControls)
            {
                 if (status == PresentationStatus.OptionsBegan || status == PresentationStatus.OptionsWaiting)
                {
                    runner.RequestHurryUpOption();
                }
            }
            else
            {
                runner.RequestHurryUpOption();
            }
        }

        /// <summary>
        /// Requests that the dialogue runner proceeds to the next line.
        /// </summary>
        public void RequestNextLine()
        {
            ResetLineTracking();
            if (runner != null)
            {
                runner.RequestNextLine();
            }
            else
            {
                Debug.LogError($"{nameof(LineAdvancer)} dialogue runner is null", this);
            }
        }

        /// <summary>
        /// Requests that the dialogue runner to instruct all line views to
        /// dismiss their content, and then stops the dialogue.
        /// </summary>
        public void RequestDialogueCancellation()
        {
            ResetLineTracking();
            // Stop the dialogue runner, which will cancel the current line as
            // well as the entire dialogue.
            if (runner != null)
            {
                runner.Stop().Forget();
            }
        }

        /// <summary>
        /// Called by Unity every frame to check to see if, depending on <see
        /// cref="UsedInputMode"/>, the <see cref="LineAdvancer"/> should take
        /// action.
        /// </summary>
        private void Update()
        {
            switch (UsedInputMode)
            {
                case InputMode.KeyCodes:
                    if (InputSystemAvailability.GetKeyDown(hurryUpLineKeyCode)) { this.RequestLineHurryUpInternal(); }
                    if (InputSystemAvailability.GetKeyDown(hurryUpOptionsKeyCode)) { this.RequestOptionHurryUp(); }
                    if (InputSystemAvailability.GetKeyDown(nextLineKeyCode)) { this.RequestNextLine(); }
                    if (InputSystemAvailability.GetKeyDown(cancelDialogueKeyCode)) { this.RequestDialogueCancellation(); }
                    break;
                case InputMode.LegacyInputAxes:
                    if (InputSystemAvailability.GetButtonDown(hurryUpLineAxis)) { this.RequestLineHurryUpInternal(); }
                    if (InputSystemAvailability.GetButtonDown(hurryUpOptionsAxis)) { this.RequestOptionHurryUp(); }
                    if (InputSystemAvailability.GetButtonDown(nextLineAxis)) { this.RequestNextLine(); }
                    if (InputSystemAvailability.GetButtonDown(cancelDialogueAxis)) { this.RequestDialogueCancellation(); }
                    break;
                default:
                    // Nothing to do; 'None' takes no action, and 'InputActions'
                    // doesn't poll in Update()
                    break;
            }
        }

        public void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
        {
            return;
        }

        public void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
        {
            return;
        }

        public YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken)
        {
            return YarnTask.CompletedTask;
        }

        public void OnLineDisplayComplete()
        {
            if (status == PresentationStatus.LineBegan)
            {
                status = PresentationStatus.LineWaiting;
            }
            else if (status == PresentationStatus.OptionsBegan)
            {
                status = PresentationStatus.OptionsWaiting;
            }
        }

        public void OnLineWillDismiss()
        {
            return;
        }
    }
}
