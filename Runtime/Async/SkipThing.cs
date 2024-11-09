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

// this will become the dialogue request interrupt thing
public class SkipThing : AsyncDialogueViewBase
{
    [MustNotBeNull]
    [SerializeField] DialogueRunner runner;

    [Space]
    public bool multiSoftSkipIsHardSkip = false;

    [ShowIf(nameof(multiSoftSkipIsHardSkip))]
    [Indent]
    [Label("Skips to cancel")]
    // TODO: Update this tooltip once we've settled on our terminology
    [Tooltip("The number of times that a SOFTSKIP occurs before the line is cancelled.")]
    public int skipRequestsBeforeCancellingLine = 2;

    private int numberOfSkipsThisLine = 0;

    public enum InputMode
    {
        InputActions,
        KeyCodes,
        None,
        LegacyInputAxes,
    }

    [Space]
    [MessageBox(sourceMethod: nameof(ValidateInputMode))]
    [SerializeField] InputMode inputMode;

    public MessageBoxAttribute.Message ValidateInputMode()
    {
        if (this.inputMode == InputMode.None)
        {
            return MessageBoxAttribute.Info($"To use this component, call the following methods on it:\n\n" +
                $"- {nameof(this.RequestLineAdvancement)}()\n" +
                $"- {nameof(this.RequestLineCancellation)}()\n" +
                $"- {nameof(this.RequestDialogueCancellation)}()"
            );
        }
#if USE_INPUTSYSTEM
#if !ENABLE_LEGACY_INPUT_MANAGER
        if (this.inputMode == InputMode.LegacyInputAxes)
        {
            return MessageBoxAttribute.Warning("The Input Manager (Old) system is not enabled.\n\nEither change this setting to Input Actions, or enable Input Manager (Old) in Project Settings > Player > Configuration > Active Input Handling.");
        }
#endif
#if !ENABLE_INPUT_SYSTEM
        if (this.inputMode == InputMode.InputActions)
        {
            return MessageBoxAttribute.Warning("The Input System is not enabled.\n\nEither change this setting, or enable Input System in Project Settings > Player > Configuration > Active Input Handling.");
        }
#endif
        return MessageBoxAttribute.NoMessage;
#else
        return MessageBoxAttribute.Warning("Please install the Unity Input System package.");
#endif
    }

#if USE_INPUTSYSTEM
    [ShowIf(nameof(inputMode), InputMode.InputActions)]
    [Indent]
    [SerializeField] UnityEngine.InputSystem.InputActionReference? skipLineAction;

    [ShowIf(nameof(inputMode), InputMode.InputActions)]
    [Indent]
    [SerializeField] UnityEngine.InputSystem.InputActionReference? cancelLineAction;

    [ShowIf(nameof(inputMode), InputMode.InputActions)]
    [Indent]
    [SerializeField] UnityEngine.InputSystem.InputActionReference? cancelDialogueAction;

    [Tooltip("If true, the input actions above will be enabled when a line begins.")]
    [ShowIf(nameof(inputMode), InputMode.InputActions)]
    [Indent]
    [SerializeField] bool enableActions = true;
#endif

    [ShowIf(nameof(inputMode), InputMode.LegacyInputAxes)]
    [Indent]
    [SerializeField] string? skipLineAxis = "Jump";

    [ShowIf(nameof(inputMode), InputMode.LegacyInputAxes)]
    [Indent]
    [SerializeField] string? cancelLineAxis = "Cancel";

    [ShowIf(nameof(inputMode), InputMode.LegacyInputAxes)]
    [Indent]
    [SerializeField] string? cancelDialogueAxis = "";

    [ShowIf(nameof(inputMode), InputMode.KeyCodes)]
    [Indent]
    [SerializeField] KeyCode skipLineKeyCode = KeyCode.Space;

    [ShowIf(nameof(inputMode), InputMode.KeyCodes)]
    [Indent]
    [SerializeField] KeyCode cancelLineKeyCode = KeyCode.Escape;

    [ShowIf(nameof(inputMode), InputMode.KeyCodes)]
    [Indent]
    [SerializeField] KeyCode cancelDialogueKeyCode = KeyCode.None;

#if USE_INPUTSYSTEM
    public void OnSkipLine(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => RequestLineAdvancement();
    public void OnCancelLine(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => RequestLineCancellation();
    public void OnCancelDialogue(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => RequestDialogueCancellation();
#endif

    public override YarnTask OnDialogueStartedAsync()
    {
#if USE_INPUTSYSTEM
        // If we're using the input system, register callbacks to run when our actions are performed.
        if (skipLineAction != null) { skipLineAction.action.performed += OnSkipLine; }
        if (cancelLineAction != null) { cancelLineAction.action.performed += OnCancelLine; }
        if (cancelDialogueAction != null) { cancelDialogueAction.action.performed += OnCancelDialogue; }
#endif

        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        // If we're using the input system, remove the callbacks
#if USE_INPUTSYSTEM
        // If we're using the input system, register callbacks to run when our actions are performed.
        if (skipLineAction != null) { skipLineAction.action.performed -= OnSkipLine; }
        if (cancelLineAction != null) { cancelLineAction.action.performed -= OnCancelLine; }
        if (cancelDialogueAction != null) { cancelDialogueAction.action.performed -= OnCancelDialogue; }
#endif

        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        // A new line has come in, so reset the number of times we've seen a
        // request to skip.
        numberOfSkipsThisLine = 0;

#if USE_INPUTSYSTEM
        if (enableActions)
        {
            if (skipLineAction != null) { skipLineAction.action.Enable(); }
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

        numberOfSkipsThisLine += 1;
        if (multiSoftSkipIsHardSkip && numberOfSkipsThisLine >= skipRequestsBeforeCancellingLine)
        {
            RequestLineCancellation();
        }
        else
        {
            runner.HurryUpCurrentLine();
        }
    }

    public void RequestLineCancellation()
    {
        // Request that the runner cancel the entire line
        runner.CancelCurrentLine();
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
