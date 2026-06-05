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

    /// <summary>
    /// A dialogue presenter that listens for user input and sends requests to a <see
    /// cref="DialogueRunner"/> to advance the presentation of the current line,
    /// either by asking a dialogue runner to hurry up its delivery, advance to
    /// the next line, or cancel the entire dialogue session.
    /// </summary>
    public sealed partial class LineAdvancer : DialoguePresenterBase, IActionMarkupHandler
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

        public bool SeparateHurryUpAndAdvanceControls => separateHurryUpAndAdvanceControls;

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
            [InspectorName("Manual")] None,
            /// <summary>
            /// The line advancer responds to input from the legacy <a
            /// href="https://docs.unity3d.com/Manual/class-InputManager.html">Input
            /// Manager</a>.
            /// </summary>
            LegacyInputAxes,
            [InspectorName("Third-Party")] External,
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

            if (this.inputMode == InputMode.External)
            {
                if (TryGetComponent<ILineAdvancerInput>(out _) == false)
                {
                    return MessageBoxAttribute.Info($"Add an input-handling component that implements the {nameof(ILineAdvancerInput)} interface.");
                }
            }

            return MessageBoxAttribute.NoMessage;
#pragma warning restore CS0162 // Unreachable code detected
        }


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
            }
        }

        public void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += SetupInputMode;
#endif
        }

#if UNITY_EDITOR
        private void SetupInputMode()
        {
            // This method gets called via a delayCall, so by the time this
            // method gets called, this object may no longer exist. Early out if
            // that's the case.
            if (this == null)
            {
                return;
            }

            switch (this.inputMode)
            {
                case InputMode.KeyCodes:
                    SetupInput<LineAdvancerInput.KeyCodes>(i =>
                    {
                        if (hasTransferredLegacyMapping)
                        {
                            return;
                        }

                        i.hurryUpLineKeyCode = legacyHurryUpLineKeyCode;
                        i.nextLineKeyCode = legacyNextLineKeyCode;
                        i.hurryUpOptionsKeyCode = legacyHurryUpOptionsKeyCode;
                        i.cancelDialogueKeyCode = legacyCancelDialogueKeyCode;

                        hasTransferredLegacyMapping = true;
                    });
                    break;
                case InputMode.InputActions:
                    SetupInput<LineAdvancerInput.InputActions>(i =>
                    {
                        if (hasTransferredLegacyMapping)
                        {
                            return;
                        }

                        i.hurryUpLineAction = legacyHurryUpLineAction;
                        i.nextLineAction = legacyNextLineAction;
                        i.hurryUpOptionsAction = legacyHurryUpOptionsAction;
                        i.cancelDialogueAction = legacyCancelDialogueAction;
                        i.enableActions = legacyEnableActions;

                        hasTransferredLegacyMapping = true;
                    });
                    break;

                case InputMode.LegacyInputAxes:
                    SetupInput<LineAdvancerInput.LegacyInputAxes>(i =>
                    {
                        if (hasTransferredLegacyMapping)
                        {
                            return;
                        }

                        i.hurryUpLineAxis = legacyHurryUpLineAxis;
                        i.nextLineAxis = legacyNextLineAxis;
                        i.hurryUpOptionsAxis = legacyHurryUpOptionsAxis;
                        i.cancelDialogueAxis = legacyCancelDialogueAxis;



                        hasTransferredLegacyMapping = true;
                    });
                    break;
                case InputMode.None:
                    foreach (var c in this.GetComponents<MonoBehaviour>())
                    {
                        if (!(c is ILineAdvancerInput))
                        {
                            continue;
                        }

                        DestroyImmediate(c);
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                    break;
                case InputMode.External:
                    foreach (var c in this.GetComponents<ILineAdvancerInput>())
                    {
                        if (c is LineAdvancerInput.KeyCodes k)
                        {
                            DestroyImmediate(k);
                            UnityEditor.EditorUtility.SetDirty(this);
                            continue;
                        }
                        if (c is LineAdvancerInput.InputActions i)
                        {
                            DestroyImmediate(i);
                            UnityEditor.EditorUtility.SetDirty(this);
                            continue;
                        }
                        if (c is LineAdvancerInput.LegacyInputAxes a)
                        {
                            DestroyImmediate(a);
                            UnityEditor.EditorUtility.SetDirty(this);
                            continue;
                        }
                    }
                    break;

            }
        }

        private void SetupInput<T>(System.Action<T>? onLineAdvancerInputComponentAdded) where T : MonoBehaviour, ILineAdvancerInput
        {
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)
                && UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(this.gameObject) == null)
            {
                // We're in a prefab, and we're not in 'prefab edit' mode. Don't modify this object.
                return;
            }

            var components = this.GetComponents<MonoBehaviour>();

            bool needsAdding = true;

            ILineAdvancerInput input;

            foreach (var existing in components)
            {
                if (!(existing is ILineAdvancerInput existingInput))
                {
                    continue;
                }

                if (existing is T)
                {
                    needsAdding = false;
                    input = existingInput;
                    continue;
                }
                else
                {
                    UnityEditor.EditorUtility.SetDirty(this);
                    DestroyImmediate(existing);
                }
            }

            if (needsAdding)
            {
                var newInput = this.gameObject.AddComponent<T>();
                input = newInput;
                input.LineAdvancer = this;
                UnityEditor.EditorUtility.SetDirty(this);

                onLineAdvancerInputComponentAdded?.Invoke(newInput);
            }
        }
#endif

        /// <summary>
        /// Called by a dialogue runner when dialogue starts to add input action
        /// handlers for advancing the line.
        /// </summary>
        /// <returns>A completed task.</returns>
        public override YarnTask OnDialogueStartedAsync()
        {
            if (TryGetComponent<ILineAdvancerInput>(out var input))
            {
                input.OnDialogueStarted();
            }
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
            if (TryGetComponent<ILineAdvancerInput>(out var input))
            {
                input.OnDialogueComplete();
            }
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

        public void OnInputHurryUpLines() => RequestLineHurryUpInternal();
        public void OnInputNextContent() => RequestNextLine();
        public void OnInputHurryUpOptions() => RequestOptionHurryUp();
        public void OnInputCancelDialogue() => RequestDialogueCancellation();

    }
    public interface ILineAdvancerInput
    {
        public LineAdvancer? LineAdvancer { get; set; }
        public void OnDialogueStarted();
        public void OnDialogueComplete();
    }

    // Previous versions of LineAdvancer stored all of their configuration data
    // in the LineAdvancer component itself. To avoid data loss, LineAdvancer
    // keeps these fields around, and moves them into the ILineAdvancerInput
    // components when they're added.
    public partial class LineAdvancer
    {

        [HideInInspector]
        [SerializeField] bool hasTransferredLegacyMapping = false;

#if USE_INPUTSYSTEM
        /// <summary>
        /// The Input Action that triggers a request to advance to the next
        /// piece of content.
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("hurryUpLineAction")]
        [HideInInspector]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? legacyHurryUpLineAction;

        /// <summary>
        /// The Input Action that triggers an instruction to cancel the current
        /// line.
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("nextLineAction")]
        [HideInInspector]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? legacyNextLineAction;

        /// <summary>
        /// The Input Action that triggers an instruction to hurry up presenting the current options
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("hurryUpOptionsAction")]
        [HideInInspector]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? legacyHurryUpOptionsAction;

        /// <summary>
        /// The Input Action that triggers an instruction to cancel the entire
        /// dialogue.
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("cancelDialogueAction")]
        [HideInInspector]
        [SerializeField] UnityEngine.InputSystem.InputActionReference? legacyCancelDialogueAction;

        /// <summary>
        /// If true, the <see cref="hurryUpLineAction"/>, <see
        /// cref="nextLineAction"/> and <see cref="cancelDialogueAction"/> Input
        /// Actions will be enabled when the the dialogue runner signals that a
        /// line is running.
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("enableActions")]
        [HideInInspector]
        [SerializeField] bool legacyEnableActions = true;
#endif
        /// <summary>
        /// The legacy Input Axis that triggers a request to advance to the next
        /// piece of content.
        /// </summary>


        [UnityEngine.Serialization.FormerlySerializedAs("hurryUpLineAxis")]
        [HideInInspector]
        [SerializeField] string? legacyHurryUpLineAxis = "Jump";

        /// <summary>
        /// The legacy Input Axis that triggers an instruction to cancel the
        /// current line.
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("nextLineAxis")]
        [HideInInspector]
        [SerializeField] string? legacyNextLineAxis = "Cancel";

        /// <summary>
        /// The legacy Input Axis that triggers an instruction to hurry up presenting the current options
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("hurryUpOptionsAxis")]
        [HideInInspector]
        [SerializeField] string? legacyHurryUpOptionsAxis = "Jump";

        /// <summary>
        /// The legacy Input Axis that triggers an instruction to cancel the
        /// entire dialogue.
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("cancelDialogueAxis")]
        [HideInInspector]
        [SerializeField] string? legacyCancelDialogueAxis = "";

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers a request to advance to the
        /// next piece of content.
        /// </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("hurryUpLineKeyCode")]
        [HideInInspector]
        [SerializeField] KeyCode legacyHurryUpLineKeyCode = KeyCode.Space;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to cancel the
        /// current line.
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("nextLineKeyCode")]
        [HideInInspector]
        [SerializeField] KeyCode legacyNextLineKeyCode = KeyCode.Escape;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to hurry up presenting options
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("hurryUpOptionsKeyCode")]
        [HideInInspector]
        [SerializeField] KeyCode legacyHurryUpOptionsKeyCode = KeyCode.Space;

        /// <summary>
        /// The <see cref="KeyCode"/> that triggers an instruction to cancel the
        /// entire dialogue.
        /// </summary>

        [UnityEngine.Serialization.FormerlySerializedAs("cancelDialogueKeyCode")]
        [HideInInspector]
        [SerializeField] KeyCode legacyCancelDialogueKeyCode = KeyCode.None;
    }
}
