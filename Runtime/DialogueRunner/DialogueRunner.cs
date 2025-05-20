/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Yarn.Unity.Attributes;
using Yarn.Unity.Legacy;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// A Line Cancellation Token stores information about whether a dialogue
    /// presenter should stop its delivery.
    /// </summary>
    /// <remarks>
    /// <para>Dialogue presenters receive Line Cancellation Tokens as a parameter to
    /// <see cref="DialoguePresenterBase.RunLineAsync"/>. Line Cancellation
    /// Tokens indicate whether the user has requested that the line's delivery
    /// should be hurried up, and whether the dialogue presenter should stop showing
    /// the current line.</para>
    /// </remarks>
    public struct LineCancellationToken
    {
        /// <summary>
        /// A <see cref="CancellationToken"/> that becomes cancelled when a <see
        /// cref="DialogueRunner"/> wishes all dialogue presenters to stop running
        /// the current line. For example, on-screen UI should be dismissed, and
        /// any ongoing audio playback should be stopped.
        /// </summary>
        public CancellationToken NextLineToken;


        // this token will ALWAYS be a dependant token on the above 

        /// <summary>
        /// A <see cref="CancellationToken"/> that becomes cancelled when a <see
        /// cref="DialogueRunner"/> wishes all dialogue presenters to speed up their
        /// delivery of their line, if appropriate. For example, UI animations
        /// should be played faster or skipped.
        /// </summary>
        /// <remarks>This token is linked to <see cref="NextLineToken"/>: if the
        /// next line token is cancelled, then this token will become cancelled
        /// as well.</remarks>
        public CancellationToken HurryUpToken;

        /// <summary>
        /// Gets a value indicating whether the dialogue runner has requested
        /// that the next line be shown.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value is <see langword="true"/>, dialogue presenters should
        /// presenting the current line, so that the next piece of content can
        /// be shown to the user.
        /// </para>
        /// <para>
        /// If this property is <see langword="true"/>, then <see
        /// cref="IsHurryUpRequested"/> will also be true.</para>
        /// </remarks>
        public readonly bool IsNextLineRequested => NextLineToken.IsCancellationRequested;

        /// <summary>
        /// Gets a value indicating whether the user has requested that the line
        /// be hurried up.
        /// </summary>
        /// <remarks><para>If this value is <see langword="true"/>, Dialogue
        /// presenters should speed up any ongoing delivery of the line, such as
        /// on-screen animations, but are not required to finish delivering the
        /// line entirely (that is, UI elements may remain on screen).</para>
        /// <para>If <see cref="IsNextLineRequested"/> is <see
        /// langword="true"/>, then this property will also be <see
        /// langword="true"/>.</para>
        /// </remarks>
        ///
        public readonly bool IsHurryUpRequested => HurryUpToken.IsCancellationRequested;
    }

    /// <summary>
    /// A <see cref="UnityEvent"/> that takes a single <see langword="string"/>
    /// parameter.
    /// </summary>
    [System.Serializable]
    public class UnityEventString : UnityEvent<string> { }

    [HelpURL("https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-runner")]
    public sealed partial class DialogueRunner : MonoBehaviour
    {
        private Dialogue? dialogue;

        /// <summary>
        /// Gets the internal <see cref="Dialogue"/> object that reads and
        /// executes the Yarn script.
        /// </summary>
        public Dialogue Dialogue
        {
            get
            {
                if (dialogue == null)
                {
                    dialogue = new Yarn.Dialogue(VariableStorage);

                    dialogue.LineHandler = OnLineReceived;
                    dialogue.OptionsHandler = OnOptionsReceived;
                    dialogue.CommandHandler = OnCommandReceived;
                    dialogue.NodeStartHandler = OnNodeStarted;
                    dialogue.NodeCompleteHandler = OnNodeCompleted;
                    dialogue.DialogueCompleteHandler = OnDialogueCompleted;
                    dialogue.PrepareForLinesHandler = OnPrepareForLines;

                    if (yarnProject != null)
                    {
                        Dialogue.SetProgram(yarnProject.Program);
                    }
                }
                return dialogue;
            }
        }

        enum SaliencyStrategy
        {
            RandomBestLeastRecentlyViewed,
            FirstBestLeastRecentlyViewed,
            Best,
            First,
            Custom
        }

        /// <summary>
        /// The Yarn Project containing the nodes that this Dialogue Runner
        /// runs.
        /// </summary>
        [SerializeField] internal YarnProject? yarnProject;

        /// <summary>
        /// The object that manages the Yarn variables used by this Dialogue Runner.
        /// </summary>
        [SerializeField] internal VariableStorageBehaviour? variableStorage;

        /// <summary>
        /// Gets the <see cref="YarnProject"/> asset that this dialogue runner uses.
        /// </summary>
        /// <seealso cref="SetProject(YarnProject)"/>
        public YarnProject? YarnProject => yarnProject;

        /// <summary>
        /// Gets the VariableStorage that this dialogue runner uses to store and
        /// access Yarn variables.
        /// </summary>
        public VariableStorageBehaviour VariableStorage
        {
            get
            {
                // If we don't  have a variable storage, create an in
                // InMemoryVariableStorage and use that.
                if (variableStorage == null)
                {
                    this.variableStorage = gameObject.AddComponent<InMemoryVariableStorage>();
                }
                if (this.variableStorage.Program == null && this.YarnProject != null)
                {
                    this.variableStorage.Program = this.YarnProject.Program;
                }
                return variableStorage;
            }
            set => variableStorage = value;
        }

        [SerializeReference] internal LineProviderBehaviour? lineProvider;

        [SerializeField] private SaliencyStrategy saliencyStrategy = SaliencyStrategy.RandomBestLeastRecentlyViewed;

        /// <summary>
        ///  Gets the <see cref="ILineProvider"/> that this dialogue runner uses
        ///  to fetch localized line content.
        /// </summary>
        public ILineProvider LineProvider
        {
            get
            {
                if (lineProvider == null)
                {
                    // No line provider was created. We'll need to create one.
                    if (yarnProject != null && yarnProject.localizationType == LocalizationType.Unity)
                    {
                        // The Yarn Project uses Unity Localisation; without an
                        // appropriately configured line provider, we can't show
                        // any lines.
                        Debug.LogWarning($"Yarn Project {yarnProject.name} uses Unity Localization, but the " +
                            $"Dialogue Runner \"{this.name}\" isn't set up to use a {nameof(Yarn.Unity.UnityLocalization.UnityLocalisedLineProvider)}. " +
                            $"Line text and assets will not be available.", this);
                    }

                    lineProvider = gameObject.AddComponent<BuiltinLocalisedLineProvider>();
                    lineProvider.YarnProject = yarnProject;
                }
                return lineProvider;
            }
        }

        /// <summary>
        /// The list of dialogue presenters that the dialogue runner delivers content
        /// to.
        /// </summary>
        [Space]
        [UnityEngine.Serialization.FormerlySerializedAs("dialogueViews")]
        [SerializeField] List<DialoguePresenterBase?> dialoguePresenters = new List<DialoguePresenterBase?>();

        /// <summary>
        /// Gets a value that indicates if the dialogue is actively
        /// running.
        /// </summary>
        public bool IsDialogueRunning => Dialogue.IsActive;

        /// <summary>
        /// Whether the dialogue runner will immediately start running dialogue
        /// after loading.
        /// </summary>
        [Group("Behaviour")]
        [Label("Start Automatically")]
        public bool autoStart = false;

        /// <summary>
        /// The name of the node that will start running immediately after
        /// loading.
        /// </summary>
        /// <remarks>This value must be the name of a node present in <see
        /// cref="YarnProject"/>.</remarks>
        /// <seealso cref="YarnProject"/>
        /// <seealso cref="StartDialogue(string)"/>
        [Group("Behaviour")]
        [ShowIf(nameof(autoStart))]
        [Indent(1)]
        [YarnNode(nameof(yarnProject))]
        public string startNode = "Start";

        /// <summary>
        /// If this value is set, when an option is selected, the line contained
        /// in it (<see cref="OptionSet.Option.Line"/>) will be delivered to the
        /// dialogue runner's dialogue presenters as though it had been written as a
        /// separate line.
        /// </summary>
        /// <remarks>
        /// This allows a Yarn script to
        /// </remarks>
        [Group("Behaviour")]
        public bool runSelectedOptionAsLine = false;

        /// <summary>
        /// A Unity event that is called when a node starts running.
        /// </summary>
        /// <remarks>
        /// This event receives as a parameter the name of the node that is
        /// about to start running.
        /// </remarks>
        /// <seealso cref="Dialogue.NodeStartHandler"/>
        [Group("Events", foldOut: true)]
        public UnityEventString? onNodeStart;

        /// <summary>
        /// A Unity event that is called when a node is complete.
        /// </summary>
        /// <remarks>
        /// This event receives as a parameter the name of the node that
        /// just finished running.
        /// </remarks>
        /// <seealso cref="Dialogue.NodeCompleteHandler"/>
        [Group("Events", foldOut: true)]
        public UnityEventString? onNodeComplete;

        /// <summary>
        /// A Unity event that is called when the dialogue starts running.
        /// </summary>
        [Group("Events", foldOut: true)]
        public UnityEvent? onDialogueStart;

        /// <summary>
        /// A Unity event that is called once the dialogue has completed.
        /// </summary>
        /// <seealso cref="Dialogue.DialogueCompleteHandler"/>
        [Group("Events", foldOut: true)]
        public UnityEvent? onDialogueComplete;

        /// <summary>
        /// A <see cref="UnityEventString"/> that is called when a <see
        /// cref="Command"/> is received and no command handler was able to
        /// handle it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this method to dispatch a command to other parts of your game.
        /// This method is only called if the <see cref="Command"/> has not been
        /// handled by a command handler that has been added to the <see
        /// cref="DialogueRunner"/>, or by a method on a <see
        /// cref="MonoBehaviour"/> in the scene with the attribute <see
        /// cref="YarnCommandAttribute"/>.
        /// </para>
        /// <para style="hint">
        /// When a command is delivered in this way, the <see
        /// cref="DialogueRunner"/> will not pause execution. If you want a
        /// command to make the DialogueRunner pause execution, see <see
        /// cref="AddCommandHandler(string, Delegate)"/>.
        /// </para>
        /// <para>
        /// This method receives the full text of the command, as it appears
        /// between the <c>&lt;&lt;</c> and <c>&gt;&gt;</c> markers.
        /// </para>
        /// </remarks>
        /// <seealso cref="AddCommandHandler(string, Delegate)"/>
        /// <seealso cref="YarnCommandAttribute"/>
        [Group("Events", foldOut: true)]
        [UnityEngine.Serialization.FormerlySerializedAs("onCommand")]
        public UnityEventString? onUnhandledCommand;

        /// <summary>
        /// Gets or sets the collection of dialogue presenters attached to this
        /// dialogue runner.
        /// </summary>
        /// <remarks>This property is deprecated. Use <see
        /// cref="DialoguePresenters"/> instead.</remarks>
        [Obsolete("Use " + nameof(DialoguePresenters))]
        public IEnumerable<DialoguePresenterBase?> DialogueViews
        {
            get => DialoguePresenters;
            set => DialoguePresenters = value;
        }

        /// <summary>
        /// Gets or sets the collection of dialogue presenters attached to this
        /// dialogue runner.
        /// </summary>
        public IEnumerable<DialoguePresenterBase?> DialoguePresenters
        {
            get => dialoguePresenters;
            set => dialoguePresenters = value.ToList();
        }

        /// <summary>
        /// Gets a completed <see cref="YarnTask{DialogueOption}"/> that
        /// contains a <see langword="null"/> value.
        /// </summary>
        /// <remarks>Dialogue presenters can return this value from their <see
        /// cref="DialoguePresenterBase.RunOptionsAsync(DialogueOption[],
        /// CancellationToken)" method to indicate that no option was selected.
        /// />
        public static YarnTask<DialogueOption?> NoOptionSelected
        {
            get
            {
                return YarnTask.FromResult<DialogueOption?>(null);
            }
        }


        private CancellationTokenSource? dialogueCancellationSource;
        private CancellationTokenSource? currentLineCancellationSource;
        private CancellationTokenSource? currentLineHurryUpSource;
        private YarnTaskCompletionSource? dialogueCompletionSource;

        internal ICommandDispatcher CommandDispatcher
        {
            get
            {
                EnsureCommandDispatcherReady();

                if (_commandDispatcher != null)
                {
                    return _commandDispatcher;
                }
                else
                {
                    throw new InvalidOperationException($"{nameof(EnsureCommandDispatcherReady)} failed to set up command dispatcher");
                }
            }
        }

        private void EnsureCommandDispatcherReady()
        {
            if (_commandDispatcher == null)
            {
                var actions = new Actions(this, Dialogue.Library);
                _commandDispatcher = actions;
                actions.RegisterActions();
            }
        }

        private ICommandDispatcher? _commandDispatcher;

        /// <summary>
        /// Called by Unity to set up the object.
        /// </summary>
        private void Awake()
        {
            if (this.VariableStorage != null && this.YarnProject != null)
            {
                this.VariableStorage.Program = this.YarnProject.Program;
            }

            if (this.LineProvider != null && this.YarnProject != null)
            {
                this.LineProvider.YarnProject = this.YarnProject;
            }
        }

        /// <summary>
        /// Sets the saliency strategy based on the value set inside of the Inspector.
        /// This is called when StartDialogue is run.
        /// This does no checking and will obliterate any custom strategies if one has been set and you change the value.
        /// </summary>
        private void ApplySaliencyStrategy()
        {
            // if we don't have any dialogue then we can't apply a saliency strategy
            if (this.dialogue == null)
            {
                return;
            }
            // likewise for variable storage
            if (this.VariableStorage == null)
            {
                return;
            }

            switch (this.saliencyStrategy)
                {
                    case SaliencyStrategy.RandomBestLeastRecentlyViewed:
                        this.dialogue.ContentSaliencyStrategy = new Saliency.RandomBestLeastRecentlyViewedSaliencyStrategy(this.VariableStorage);
                        return;
                    case SaliencyStrategy.FirstBestLeastRecentlyViewed:
                        this.dialogue.ContentSaliencyStrategy = new Yarn.Saliency.BestLeastRecentlyViewedSaliencyStrategy(this.VariableStorage);
                        return;
                    case SaliencyStrategy.Best:
                        this.dialogue.ContentSaliencyStrategy = new Yarn.Saliency.BestSaliencyStrategy();
                        return;
                    case SaliencyStrategy.First:
                        this.dialogue.ContentSaliencyStrategy = new Yarn.Saliency.FirstSaliencyStrategy();
                        return;
                }
        }

        /// <summary>
        /// Called by Unity to start running dialogue if <see cref="autoStart"/>
        /// is enabled.
        /// </summary>
        private void Start()
        {
            if (autoStart)
            {
                StartDialogue(startNode);
            }
        }

        /// <summary>
        /// Stops the dialogue immediately, and cancels any currently running
        /// dialogue presenters.
        /// </summary>
        public void Stop()
        {
            CancelDialogue();
        }

        /// <summary>
        /// Called by Unity to cancel the current dialogue when the Dialogue
        /// Runner is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            CancelDialogue();
        }

        /// <summary>
        /// Gets a <see cref="YarnTask"/> that completes when the dialogue
        /// runner finishes its dialogue.
        /// </summary>
        /// <remarks>
        /// If the dialogue is not currently running when this property is
        /// accessed, the property returns a task that is already complete.
        /// </remarks>
        public YarnTask DialogueTask
        {
            get
            {
                if (dialogueCompletionSource == null)
                {
                    return YarnTask.CompletedTask;
                }

                return dialogueCompletionSource.Task;
            }
        }

        private void CancelDialogue()
        {
            if (dialogueCancellationSource == null || Dialogue.IsActive == false)
            {
                // We're not running dialogue. There's nothing to cancel.
                return;
            }

            // Cancel the current line, if any.
            currentLineCancellationSource?.Cancel();

            // Cancel the entire dialogue.
            dialogueCancellationSource?.Cancel();

            // Stop the dialogue. This will cause OnDialogueCompleted to be called.
            Dialogue.Stop();
        }

        private void OnPrepareForLines(IEnumerable<string> lineIDs)
        {
            this.LineProvider.PrepareForLinesAsync(lineIDs, CancellationToken.None).Forget();
        }

        private void OnDialogueCompleted()
        {
            OnDialogueCompleteAsync().Forget();
        }
        private async YarnTask OnDialogueCompleteAsync()
        {
            // cleaning up the old cancellation token
            currentLineCancellationSource?.Dispose();
            currentLineCancellationSource = null;
            currentLineHurryUpSource?.Dispose();
            currentLineHurryUpSource = null;

            var pendingTasks = new HashSet<YarnTask>();
            foreach (var view in this.dialoguePresenters)
            {
                if (view == null)
                {
                    // The view doesn't exist. Skip it.
                    continue;
                }

                // Tell all of our views that the dialogue has finished
                async YarnTask RunCompletion()
                {
                    try
                    {
                        await view.OnDialogueCompleteAsync();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e, view);
                    }
                }

                YarnTask task = RunCompletion();

                pendingTasks.Add(task);
            }

            // Wait for all views to finish doing their clean up
            await YarnTask.WhenAll(pendingTasks);

            // Finally, notify that dialogue is complete.
            dialogueCompletionSource?.TrySetResult();
            onDialogueComplete?.Invoke();
        }

        private void OnNodeCompleted(string completedNodeName)
        {
            onNodeComplete?.Invoke(completedNodeName);
        }

        private void OnNodeStarted(string startedNodeName)
        {
            onNodeStart?.Invoke(startedNodeName);
        }

        private void OnCommandReceived(Command command)
        {
            OnCommandReceivedAsync(command).Forget();
        }

        private async YarnTask OnCommandReceivedAsync(Command command)
        {
            CommandDispatchResult dispatchResult = this.CommandDispatcher.DispatchCommand(command.Text, this);

            var parts = SplitCommandText(command.Text);
            string commandName = parts.ElementAtOrDefault(0);

            switch (dispatchResult.Status)
            {
                case CommandDispatchResult.StatusType.Succeeded:
                    // The command succeeded. Wait for it to complete. (In the
                    // case of commands that complete synchronously, this task
                    // will be Task.Completed, so this 'await' will return
                    // immediately.)
                    await dispatchResult.Task;
                    break;
                case CommandDispatchResult.StatusType.NoTargetFound:
                    Debug.LogError($"Can't call command <<{command.Text}>>: failed to find a game object named {parts.ElementAtOrDefault(1)}", this);
                    break;
                case CommandDispatchResult.StatusType.TargetMissingComponent:
                    Debug.LogError($"Can't call command <<{command.Text}>>, because {parts.ElementAtOrDefault(1)} doesn't have the correct component");
                    break;
                case CommandDispatchResult.StatusType.InvalidParameterCount:
                    Debug.LogError($"Can't call command <<{command.Text}>>: {dispatchResult.Message ?? "incorrect number of parameters"}");
                    break;
                case CommandDispatchResult.StatusType.CommandUnknown:
                    // Attempt a last-ditch dispatch by invoking our 'onCommand'
                    // Unity Event.
                    if (onUnhandledCommand != null && onUnhandledCommand.GetPersistentEventCount() > 0)
                    {
                        // We can invoke the event!
                        onUnhandledCommand.Invoke(command.Text);
                    }
                    else
                    {
                        // We're out of ways to handle this command! Log this as an
                        // error.
                        Debug.LogError($"No Command \"{commandName}\" was found. Did you remember to use the YarnCommand attribute or AddCommandHandler() function in C#?");
                    }
                    return;
                default:
                    throw new ArgumentOutOfRangeException($"Internal error: Unknown command dispatch result status {dispatchResult}");
            }

            // Continue the Dialogue, unless dialogue cancellation was requested.
            if (dialogueCancellationSource?.IsCancellationRequested ?? false)
            {
                return;
            }

            Dialogue.Continue();
        }

        private void OnLineReceived(Line line)
        {
            OnLineReceivedAsync(line).Forget();
        }

        private async YarnTask OnLineReceivedAsync(Line line)
        {
            var localisedLine = await LineProvider.GetLocalizedLineAsync(line, dialogueCancellationSource?.Token ?? CancellationToken.None);

            if (localisedLine == LocalizedLine.InvalidLine)
            {
                Debug.LogError($"Failed to get a localised line for {line.ID}!");
            }

            await RunLocalisedLine(localisedLine);

            if (dialogueCancellationSource?.IsCancellationRequested == false)
            {
                Dialogue.Continue();
            }
        }

        /// <summary>
        /// Runs a localised line on all dialogue presenters.
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
            currentLineHurryUpSource?.Dispose();

            if (dialogueCancellationSource != null)
            {
                currentLineCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(dialogueCancellationSource.Token);
            }
            else
            {
                currentLineCancellationSource = new CancellationTokenSource();
            }

            // now we make a new dependant hurry up cancellation token
            currentLineHurryUpSource = CancellationTokenSource.CreateLinkedTokenSource(currentLineCancellationSource.Token);
            var metaToken = new LineCancellationToken
            {
                NextLineToken = currentLineCancellationSource.Token,
                HurryUpToken = currentLineHurryUpSource.Token,
            };

            var pendingTasks = new HashSet<YarnTask>();

            foreach (var view in this.dialoguePresenters)
            {
                if (view == null)
                {
                    // The view doesn't exist. Skip it.
                    continue;
                }

                if (view.enabled == false)
                {
                    // The view is not enabled. Skip it.
                    continue;
                }

                // Legacy support: if this view is an v2-style DialogueViewBase,
                // then set its requestInterrupt delegate to be one that stops
                // the current line.
#pragma warning disable CS0618 // 'construct' is obsolete
                if (view is DialogueViewBase dialogueView)
                {
                    dialogueView.requestInterrupt = RequestNextLine;
                }
#pragma warning restore CS0618 // 'construct' is obsolete

                // Tell all of our views to run this line, and give them a
                // cancellation token they can use to interrupt the line if needed.

                async YarnTask RunLineAndInvokeCompletion(DialoguePresenterBase view, LocalizedLine line, LineCancellationToken token)
                {
                    try
                    {
                        // Run the line and wait for it to finish
                        await view.RunLineAsync(localisedLine, token);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e, view);
                    }
                }

                YarnTask task = RunLineAndInvokeCompletion(view, localisedLine, metaToken);

                pendingTasks.Add(task);
            }

            // Wait for all line view tasks to finish delivering the line.
            await YarnTask.WhenAll(pendingTasks);

            // We're done; dispose of the cancellation sources. (Null-check them because if we're leaving play mode, then these references may no longer be valid.)

            currentLineCancellationSource?.Dispose();
            currentLineCancellationSource = null;

            currentLineHurryUpSource?.Dispose();
            currentLineHurryUpSource = null;
        }

        private void OnOptionsReceived(OptionSet options)
        {
            OnOptionsReceivedAsync(options).Forget();
        }

        private async YarnTask OnOptionsReceivedAsync(OptionSet options)
        {
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

            DialogueOption[] localisedOptions = new DialogueOption[options.Options.Length];
            for (int i = 0; i < options.Options.Length; i++)
            {
                var opt = options.Options[i];
                LocalizedLine localizedLine = await LineProvider.GetLocalizedLineAsync(opt.Line, optionCancellationSource.Token);

                if (localizedLine == LocalizedLine.InvalidLine)
                {
                    Debug.LogError($"Failed to get a localised line for line {opt.Line.ID} (option {i + 1})!");
                }

                localisedOptions[i] = new DialogueOption
                {
                    DialogueOptionID = opt.ID,
                    IsAvailable = opt.IsAvailable,
                    Line = localizedLine,
                    TextID = opt.Line.ID,
                };
            }

            var dialogueSelectionTCS = new YarnTaskCompletionSource<DialogueOption?>();

            async YarnTask WaitForOptionsView(DialoguePresenterBase? view)
            {
                if (view == null)
                {
                    return;
                }
                var result = await view.RunOptionsAsync(localisedOptions, optionCancellationSource.Token);
                if (result != null)
                {
                    // We no longer need the other views, so tell them to stop
                    // by cancelling the option selection.
                    optionCancellationSource.Cancel();
                    dialogueSelectionTCS.TrySetResult(result);
                }
            }

            var pendingTasks = new List<YarnTask>();
            foreach (var view in this.dialoguePresenters)
            {
                pendingTasks.Add(WaitForOptionsView(view));
            }
            await YarnTask.WhenAll(pendingTasks);

            // at this point now every view has finished their handling of the options
            // the first one to return a non-null value will be the one that is chosen option
            // or if everyone returned null that's an error
            DialogueOption? selectedOption;

            try
            {
                selectedOption = await dialogueSelectionTCS.Task;
            }
            catch (Exception e)
            {
                // If a view threw an exception while getting the option,
                // propagate it
                Debug.LogException(e);
                return;
                // throw;
            }

            optionCancellationSource.Dispose();

            if (dialogueCancellationSource?.IsCancellationRequested ?? false)
            {
                // We received a request to cancel dialogue while waiting for a
                // choice. Stop here, and do not provide it to the Dialogue.
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

            Dialogue.SetSelectedOption(selectedOption.DialogueOptionID);

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
                Dialogue.Continue();
            }
        }

        /// <summary>
        /// Sets the dialogue runner's Yarn Project.
        /// </summary>
        /// <remarks>
        /// If the dialogue runner is currently running (that is, <see
        /// cref="IsDialogueRunning"/> is <see langword="true"/>), an <see
        /// cref="InvalidOperationException"/> is thrown.
        /// </remarks>
        /// <param name="project">The new <see cref="YarnProject"/> to be
        /// used.</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting
        /// to set a new project while a dialogue is currently
        /// running.</exception>
        public void SetProject(YarnProject project)
        {
            if (this.IsDialogueRunning)
            {
                // Can't change project if we're already running.
                throw new InvalidOperationException("Can't set project, because dialogue is currently running.");
            }
            this.yarnProject = project;

            Dialogue.SetProgram(project.Program);
        }

        /// <summary>
        /// Starts running a node of dialogue.
        /// </summary>
        /// <remarks><paramref name="nodeName"/> must be the name of a node in
        /// <see cref="YarnProject"/>.</remarks>
        /// <param name="nodeName">The name of the node to run.</param>
        public void StartDialogue(string nodeName)
        {
            if (yarnProject == null)
            {
                Debug.LogError($"Can't start dialogue: no Yarn Project has been configured.", this);
                return;
            }

            if (yarnProject.Program == null)
            {
                // The Yarn Project asset reference is valid, but it doesn't
                // have a program, likely due to a compiler error.
                Debug.LogError($"Can't start dialogue: Yarn Project doesn't contain a valid program (possibly due to errors in the Yarn scripts?)", this);
                return;
            }

            dialogueCancellationSource?.Dispose();

            dialogueCompletionSource = new YarnTaskCompletionSource();

            dialogueCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.destroyCancellationToken);
            LineProvider.YarnProject = yarnProject;

            EnsureCommandDispatcherReady();

            Dialogue.SetProgram(yarnProject.Program);
            Dialogue.SetNode(nodeName);

            ApplySaliencyStrategy();

            onDialogueStart?.Invoke();

            StartDialogueAsync().Forget();

            async YarnTask StartDialogueAsync()
            {
                var tasks = new List<YarnTask>();
                foreach (var view in DialoguePresenters)
                {
                    if (view == null)
                    {
                        continue;
                    }
                    tasks.Add(view.OnDialogueStartedAsync());
                }
                await YarnTask.WhenAll(tasks);
                Dialogue.Continue();
            }
        }

        /// <summary>
        /// Requests that all dialogue presenters stop showing the current line, and
        /// prepare to show the next piece of content.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The specific behaviour of what happens when this method is called
        /// depends on the implementation of the Dialogue Runner's current
        /// dialogue presenters.
        /// </para>
        /// <para>
        /// If the dialogue runner is not currently running a line (for example,
        /// if it is running options, or is not running dialogue at all), this
        /// method has no effect.
        /// </para>
        /// </remarks>
        public void RequestNextLine()
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

        /// <summary>
        /// Requests that all dialogue presenters speed up their delivery of the
        /// current line.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The specific behaviour of what happens when this method is called
        /// depends on the implementation of the Dialogue Runner's current
        /// dialogue presenters.
        /// </para>
        /// <para>
        /// If the dialogue runner is not currently running a line (for example,
        /// if it is running options, or is not running dialogue at all), this
        /// method has no effect.
        /// </para>
        /// </remarks>
        public void RequestHurryUpLine()
        {
            if (currentLineCancellationSource == null)
            {
                // We aren't running a line, so there's nothing to cancel.
                return;
            }
            if (currentLineHurryUpSource == null)
            {
                // we are running a line but don't have a hurry up token
                // is this a bug..?
                return;
            }

            currentLineHurryUpSource.Cancel();
        }
    }
}
