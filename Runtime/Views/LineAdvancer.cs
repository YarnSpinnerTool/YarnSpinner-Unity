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
    /// cref="DialogueRunner"/> to advance the presentation of the current content,
    /// either by asking a dialogue runner to hurry up its delivery, advance to
    /// the next piece of content, or cancel the entire dialogue session.
    /// </summary>
    /// <remarks>
    /// This class is intended to be used as a basic system for advancing your dialogue content.
    /// While it works fine as is it will never be as good as a dedicated input system with full knowledge of your game.
    /// Where possible we suggest replacing this class with a custom one for your game
    /// </remarks>
    public sealed class LineAdvancer : DialoguePresenterBase, IActionMarkupHandler
    {
        [MustNotBeNull("Line Advancer needs to know which Dialogue Runner should be told to tell it to show the next piece of content.")]
        [Tooltip("The dialogue runner that will receive requests to hurry up, advance, or cancel content.")]
        [SerializeField] DialogueRunner? runner;

        /// <summary>
        /// Should this line advancer use different actions for hurrying up and advancing content?
        /// </summary>
        /// <remarks>
        /// When this is false if the player requests content to hurry up and the content is fully shown the <see cref="DialogueRunner.RequestNextContent"/> method will be called instead of the <see cref="DialogueRunner.RequestHurryUpContent"/> method.
        /// This behaviour is only the case when <see cref="presenter"/> is not null and the presenter is presenting it's content via it's <see cref="DialoguePresenterBase.Typewriter"/> property.
        /// </remarks>
        [SerializeField] private bool separateHurryUpAndAdvanceControls = false;

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
            /// cref="RequestHurryUpContent"/>, <see cref="RequestNextContent"/> and
            /// <see cref="RequestDialogueCancellation"/> methods directly from
            /// your code to control advancement.</remarks>
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
                    $"- {nameof(this.RequestHurryUpContent)}()\n" +
                    $"- {nameof(this.RequestNextContent)}()\n" +
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
        /// current content.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.LegacyInputAxes)]
        [ShowIf(nameof(separateHurryUpAndAdvanceControls))]
        [Indent]
        [SerializeField] string? nextLineAxis = "Cancel";

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
        /// current content.
        /// </summary>
        [ShowIf(nameof(UsedInputMode), InputMode.KeyCodes)]
        [ShowIf(nameof(separateHurryUpAndAdvanceControls))]
        [Indent]
        [SerializeField] KeyCode nextLineKeyCode = KeyCode.Escape;

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
            RequestHurryUpContent();
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
            Unknown, LineBegan, LineWaiting, OptionsBegan, OptionsWaiting, CommandBegan,
        }
        private PresentationStatus status = PresentationStatus.Unknown;

        private void Start()
        {
            // If we have a dialogue presenter configured, register ourselves as
            // a temporal processor, so that we get notified when the line is
            // fully visible. This is so that when a line is fully visible, the
            // 'hurry up' action will instead trigger a 'next line' action,
            // (because there's nothing left to hurry up.)
            if (runner == null)
            {
                Debug.LogError("Line Advancer has been added to scene but has no dialogue runner associated with it. This will not advance your dialogue.");
                return;
            }
            if (!separateHurryUpAndAdvanceControls)
            {
                // we register ourselves as being a temporal processor for all presenters
                foreach (var presenter in runner.DialoguePresenters)
                {
                    if (presenter != null)
                    {                        
                        presenter.Typewriter?.ActionMarkupHandlers.Add(this);
                    }
                }
                // and then add ourselves to that list of presenters also so we can get the higher level messages too
                var listOfPresenters = new List<DialoguePresenterBase?>(runner.DialoguePresenters)
                {
                    this
                };
                runner.DialoguePresenters = listOfPresenters;

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
                if (nextLineAction != null) { nextLineAction.action.Enable(); }
                if (cancelDialogueAction != null) { cancelDialogueAction.action.Enable(); }
            }

            if (UsedInputMode == InputMode.InputActions)
            {
                // If we're using the input system, register callbacks to run
                // when our actions are performed.
                if (hurryUpLineAction != null) { hurryUpLineAction.action.performed += OnHurryUpLinePerformed; }
                if (nextLineAction != null) { nextLineAction.action.performed += OnNextLinePerformed; }
                if (cancelDialogueAction != null) { cancelDialogueAction.action.performed += OnCancelDialoguePerformed; }
            }
#endif

            status = PresentationStatus.Unknown;
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
                if (nextLineAction != null) { nextLineAction.action.performed -= OnNextLinePerformed; }
                if (cancelDialogueAction != null) { cancelDialogueAction.action.performed -= OnCancelDialoguePerformed; }
            }
#endif

            status = PresentationStatus.Unknown;
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Called by a dialogue presenter to signal that a line is running.
        /// </summary>
        /// <inheritdoc cref="LinePresenter.RunLineAsync" path="/param"/>
        /// <returns>A completed task.</returns>
        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
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
            status = PresentationStatus.OptionsBegan;

            frameContentReceived = Time.frameCount;

            return DialogueRunner.NoOptionSelected;
        }

        public override YarnTask RunCommand(Command command, LineCancellationToken cancellationToken)
        {
            status = PresentationStatus.CommandBegan;
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Requests that the dialogue runner hurrys up the current piece of content.
        /// </summary>
        public void RequestHurryUpContent()
        {
            if (runner == null)
            {
                Debug.LogError($"{nameof(LineAdvancer)} dialogue runner is null", this);
                return;
            }

            // if this came in the same frame we got the content we ignore this
            if (frameContentReceived == Time.frameCount)
            {
                return;
            }

            // we have separate controls for both hurrying up and skipping content
            // this is the easiest case, we just call hurry up
            if (separateHurryUpAndAdvanceControls)
            {
                runner.RequestHurryUpContent();
                return;
            }

            // we are in the shared mode
            // which means what we do now needs to change depending on what the type of content it is
            switch (status)
            {
                // we are unknown, this means the user likely clicked the wrong thing, ignoring this
                case PresentationStatus.Unknown:
                    return;
                
                // the line has started but not yet finished showing
                // this means we want to tell it to hurry up showing
                case PresentationStatus.LineBegan:
                    runner.RequestHurryUpContent();
                    return;
                
                // the line has finished showing and is waiting input to advance
                case PresentationStatus.LineWaiting:
                    RequestNextContent();
                    return;
                
                // the option has started showing, so we will request it hurry up
                // next time this occurs it will force skipping the options
                // this is because most of the time option selection will use the same key as hurry up
                // which means if we just straight up skipped we'd never be able to select an option
                case PresentationStatus.OptionsBegan:
                    runner.RequestHurryUpContent();
                    status = PresentationStatus.OptionsWaiting;
                    return;
                
                // we're now either a command and can't really know when we're finished
                // or we're an already presented option
                // either way we just want the next piece of content please
                default:
                    RequestNextContent();
                    return;
            }
        }
        /// <summary>
        /// Requests that the dialogue runner proceeds to the next piece of content.
        /// </summary>
        public void RequestNextContent()
        {
            status = PresentationStatus.Unknown;
            if (runner != null)
            {
                runner.RequestNextContent();
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
            status = PresentationStatus.Unknown;
            // Stop the dialogue runner, which will cancel the current line as
            // well as the entire dialogue.
            if (runner != null)
            {
                runner.Stop().Forget();
            }
        }

        [System.Obsolete("Changes to the dialogue runner mean there is no longer a difference between hurrying up lines and any other content. Please use RequestHurryUpContent instead")]
        public void RequestLineHurryUp()
        {
            RequestHurryUpContent();
        }

        [System.Obsolete("Changes to the dialogue runner mean there is no longer a difference between hurrying up options and any other content. Please use RequestHurryUpContent instead")]
        public void RequestOptionHurryUp()
        {
           RequestHurryUpContent();
        }

        /// <summary>
        /// Requests that the dialogue runner proceeds to the next line.
        /// </summary>
        [System.Obsolete("Changes to the dialogue runner mean there is no longer a difference between skipping lines and any other content. Please use RequestNextContent instead")]
        public void RequestNextLine()
        {
            RequestNextContent();
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
                    if (InputSystemAvailability.GetKeyDown(hurryUpLineKeyCode)) { this.RequestHurryUpContent(); }
                    if (InputSystemAvailability.GetKeyDown(nextLineKeyCode)) { this.RequestNextContent(); }
                    if (InputSystemAvailability.GetKeyDown(cancelDialogueKeyCode)) { this.RequestDialogueCancellation(); }
                    break;
                case InputMode.LegacyInputAxes:
                    if (InputSystemAvailability.GetButtonDown(hurryUpLineAxis)) { this.RequestHurryUpContent(); }
                    if (InputSystemAvailability.GetButtonDown(nextLineAxis)) { this.RequestNextContent(); }
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
