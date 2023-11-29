using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Yarn;
using Yarn.Unity;

#nullable enable

#if USE_UNITASK
using Cysharp.Threading.Tasks;
using YarnTask = Cysharp.Threading.Tasks.UniTask;
using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.DialogueOption>;
using YarnLineTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.LocalizedLine>;
#else
using YarnTask = System.Threading.Tasks.Task;
using YarnOptionTask = System.Threading.Tasks.Task<Yarn.Unity.DialogueOption>;
using YarnLineTask = System.Threading.Tasks.Task<Yarn.Unity.LocalizedLine>;
#endif

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public sealed class MemberNotNullAttribute : Attribute
    {
        public MemberNotNullAttribute(params string[] members)
        { }
    }
}

public partial class AsyncDialogueRunner : MonoBehaviour
{
    private Dialogue dialogue;

    [SerializeField] VariableStorageBehaviour variableStorage;

    [SerializeField] YarnProject yarnProject;

    [SerializeField] AsyncLineProvider lineProvider;

    [SerializeField] bool autoStart = true;
    [SerializeField] string startNode = "Start";
    [SerializeField] bool runSelectedOptionAsLine = false;

    [SerializeField] bool canCancelDialogue = false;

    [SerializeField] List<AsyncDialogueViewBase> dialogueViews = new List<AsyncDialogueViewBase>();

    private CancellationTokenSource? dialogueCancellationSource;
    private CancellationTokenSource? currentLineCancellationSource;

    public void Awake()
    {
        dialogue = new Yarn.Dialogue(variableStorage);

        dialogue.LineHandler = OnLineReceived;
        dialogue.OptionsHandler = OnOptionsReceived;
        dialogue.CommandHandler = OnCommandReceived;
        dialogue.NodeStartHandler = OnNodeStarted;
        dialogue.NodeCompleteHandler = OnNodeCompleted;
        dialogue.DialogueCompleteHandler = OnDialogueCompleted;
        dialogue.PrepareForLinesHandler = OnPrepareForLines;
    }

    public void Start()
    {
        if (autoStart)
        {
            StartDialogue(startNode);
        }
    }

    public void OnDestroy()
    {
        CancelDialogue();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CancelCurrentLine();
        }
        if (canCancelDialogue && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelDialogue();
        }
    }

    private void CancelDialogue()
    {
        if (dialogueCancellationSource == null || dialogue.IsActive == false)
        {
            // We're not running dialogue. There's nothing to cancel.
            return;
        }

        // Cancel the current line, if any.
        currentLineCancellationSource?.Cancel();
        currentLineCancellationSource?.Dispose();
        currentLineCancellationSource = null;

        // Cancel the entire dialogue.
        dialogueCancellationSource?.Cancel();
        dialogueCancellationSource?.Dispose();
        dialogueCancellationSource = null;

        // Stop the dialogue. This will cause OnDialogueCompleted to be called.
        dialogue.Stop();
    }

    private void OnPrepareForLines(IEnumerable<string> lineIDs)
    {
        Debug.Log($"OnPrepareForLines: {lineIDs.Count()} lines");
    }

    private void OnDialogueCompleted()
    {
        Debug.Log($"OnDialogueCompleted");
    }

    private void OnNodeCompleted(string completedNodeName)
    {
        Debug.Log($"OnNodeCompleted: {completedNodeName}");
    }

    private void OnNodeStarted(string startedNodeName)
    {
        Debug.Log($"OnNodeStarted: {startedNodeName}");
    }

    private void OnCommandReceived(Command command)
    {
        OnCommandReceivedAsync(command).Forget();
    }

    private async YarnTask OnCommandReceivedAsync(Command command)
    {
        Debug.Log($"Running command <<{command.Text}>>");
        var result = this.CommandDispatcher.DispatchCommand(command.Text, this, out var commandCoroutine);

        var cancellationSource = dialogueCancellationSource ?? new CancellationTokenSource();

        if (commandCoroutine != null)
        {
            Debug.Log($"Command returned a coroutine. Waiting for it...");
            await this.WaitForCoroutine(commandCoroutine, cancellationSource.Token);
            Debug.Log($"Done waiting for <<{command.Text}>>");
        }

        if (cancellationSource.IsCancellationRequested == false)
        {
            dialogue.Continue();
        }
    }

    private void OnLineReceived(Line line)
    {
        OnLineReceivedAsync(line).Forget();
    }

    private async YarnTask OnLineReceivedAsync(Line line)
    {
        var localisedLine = await GetLocalizedLine(line);

        var cancellationSource = dialogueCancellationSource ?? new CancellationTokenSource();

        await RunLocalisedLine(localisedLine);

        if (cancellationSource.IsCancellationRequested == false)
        {
            dialogue.Continue();
        }
    }

    /// <summary>
    /// Runs a localised line on all dialogue views.
    /// </summary>
    /// <remarks>
    /// This method can be called from two places: 1. when a line is being run,
    /// and 2. when an option has been selected and <see
    /// cref="runSelectedOptionAsLine"/> is <see langword="true"/>.
    /// </remarks>
    /// <param name="localisedLine"></param>
    /// <returns></returns>
    private async YarnTask RunLocalisedLine(LocalizedLine localisedLine)
    {
        // Create a new cancellation source for this line, linked to the
        // dialogue cancellation (if we have one). Dispose of the previous one,
        // if we have it.
        currentLineCancellationSource?.Dispose();

        if (dialogueCancellationSource != null)
        {
            currentLineCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(dialogueCancellationSource.Token);
        }
        else
        {
            currentLineCancellationSource = new CancellationTokenSource();
        }

        var pendingTasks = new List<YarnTask>();

        foreach (var view in this.dialogueViews)
        {
            // Legacy support: if this view is an v2-style DialogueViewBase,
            // then set its requestInterrupt delegate to be one that cancels the
            // current line.
            if (view is DialogueViewBase dialogueView)
            {
                dialogueView.requestInterrupt = CancelCurrentLine;
            }

            // Tell all of our views to run this line, and give them a
            // cancellation token they can use to interrupt the line if needed.
            YarnTask task = view.RunLineAsync(localisedLine, currentLineCancellationSource.Token);
            pendingTasks.Add(task);
        }

        // Wait for all line view tasks to finish delivering the line.
        await YarnTask.WhenAll(pendingTasks);

        currentLineCancellationSource.Dispose();
        currentLineCancellationSource = null;
    }

    private void OnOptionsReceived(OptionSet options)
    {
        OnOptionsReceivedAsync(options).Forget();
    }

    private async YarnTask OnOptionsReceivedAsync(OptionSet options)
    {
        DialogueOption[] localisedOptions = new DialogueOption[options.Options.Length];
        for (int i = 0; i < options.Options.Length; i++)
        {
            var opt = options.Options[i];
            localisedOptions[i] = new DialogueOption
            {
                DialogueOptionID = opt.ID,
                IsAvailable = opt.IsAvailable,
                Line = await GetLocalizedLine(opt.Line),
                TextID = opt.Line.ID,
            };
        }

        // Create a cancellation source that represents 'we don't need you to
        // select an option anymore'. Link it to the dialogue cancellation
        // source, so that if dialogue gets cancelled, all options get
        // cancelled.
        CancellationTokenSource optionCancellationSource;
        if (dialogueCancellationSource != null)
        {
            optionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(dialogueCancellationSource.Token);
        }
        else
        {
            optionCancellationSource = new CancellationTokenSource();
        }

        var pendingTasks = new HashSet<YarnOptionTask>();
        foreach (var view in this.dialogueViews)
        {
            YarnOptionTask task = view.RunOptionsAsync(localisedOptions, optionCancellationSource.Token);
            pendingTasks.Add(task);
        }

        DialogueOption? selectedOption = null;

        // Wait for our option views. If any of them return a non-null value,
        // use it (and cancel the rest.)
        while (pendingTasks.Count > 0)
        {
            var completedTask = await YarnTask.WhenAny(pendingTasks);
            if (completedTask.Result == null)
            {
                // An option task completed and returned null. Remove that task
                // from the set of tasks we're waiting for, and wait for the
                // rest.
                pendingTasks.Remove(completedTask);
            }
            else
            {
                // The option task completed with an option. Cancel all other
                // option handlers and stop waiting for them - their input is no
                // longer required.
                selectedOption = completedTask.Result;
                optionCancellationSource.Cancel();
                break;
            }
        }

        if (dialogueCancellationSource?.IsCancellationRequested ?? false)
        {
            // We received a request to cancel dialogue while waiting for a
            // choice. Stop here.
            return;
        }

        else if (selectedOption == null)
        {
            // None of our option views returned an option, and our dialogue
            // wasn't cancelled. That's not allowed, because we don't know what
            // to do next!
            Debug.LogError($"No dialogue view returned an option selection! Hanging here!");
            return;
        }

        dialogue.SetSelectedOption(selectedOption.DialogueOptionID);

        if (runSelectedOptionAsLine)
        {
            // Run the selected option's line content as though we had received
            // it as a line.
            await RunLocalisedLine(selectedOption.Line);
        }

        if (dialogueCancellationSource?.IsCancellationRequested ?? false)
        {
            // Our dialogue has been cancelled. Don't continue the dialogue.
            return;
        }
        else
        {
            // Proceed to the next piece of dialogue content.
            dialogue.Continue();
        }
    }

    private async YarnLineTask GetLocalizedLine(Line line)
    {
        var localisedLine = await lineProvider.GetLocalizedLineAsync(line);

        var text = Dialogue.ExpandSubstitutions(localisedLine.RawText, line.Substitutions);
        dialogue.LanguageCode = lineProvider.LocaleCode;
        localisedLine.Text = this.dialogue.ParseMarkup(text);

        return localisedLine;
    }

    public void StartDialogue(string nodeName)
    {
        dialogueCancellationSource = new CancellationTokenSource();
        lineProvider.YarnProject = yarnProject;
        dialogue.SetProgram(yarnProject.Program);
        dialogue.SetNode(nodeName);
        dialogue.Continue();
    }

    public void CancelCurrentLine()
    {
        if (currentLineCancellationSource == null)
        {
            // We aren't running a line, so there's nothing to cancel.
            return;
        }

        // Cancel the current line. All currently pending tasks, which received
        // a CancellationToken, will be able to respond to the request to
        // cancel.
        currentLineCancellationSource.Cancel();
    }
}
