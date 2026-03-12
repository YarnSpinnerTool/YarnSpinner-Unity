/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Yarn.Unity.Attributes;

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
        /// A <see cref="CancellationToken"/> that becomes cancelled when a <see cref="DialogueRunner"/> wishes all dialogue presenters to stop running the current content. For example, on-screen UI should be dismissed, and any ongoing audio playback should be stopped.
        /// </summary>
        public CancellationToken NextContentToken;

        /// <summary>
        /// A <see cref="CancellationToken"/> that becomes cancelled when a <see cref="DialogueRunner"/> wishes all dialogue presenters to speed up their delivery of their content, if appropriate. For example, UI animations should be played faster or skipped.
        /// </summary>
        /// <remarks>
        /// This token is linked to <see cref="NextContentToken"/>: if the next content token is cancelled, then this token will become cancelled as well.
        /// </remarks>
        public CancellationToken HurryUpToken;

        /// <summary>
        /// Gets a value indicating whether the dialogue runner has requested that the next content be shown.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value is <see langword="true"/>, dialogue presenters should skip presenting the current content, so that the next piece of content can be shown to the user.
        /// </para>
        /// <para>
        /// If this property is <see langword="true"/>, then <see cref="IsHurryUpRequested"/> will also be <see langword="true"/>.
        /// </para>
        /// </remarks>
        public readonly bool IsNextContentRequested => NextContentToken.IsCancellationRequested;

        /// <summary>
        /// Gets a value indicating whether the user has requested that the content be hurried up.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value is <see langword="true"/>, Dialogue presenters should speed up any ongoing delivery of the content, such as on-screen animations, but are not required to finish delivering the line entirely (that is, UI elements may remain on screen)
        /// </para>
        /// <para>
        /// If <see cref="IsNextContentRequested"/> is <see langword="true"/>, then this property will also be <see langword="true"/>.
        /// </para>
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
    public sealed partial class DialogueRunner : MonoBehaviour, IDialogueResponder
    {
        private AsyncDialogue? dialogue;
        public AsyncDialogue Dialogue
        {
            get
            {
                if (dialogue == null)
                {
                    dialogue = new Yarn.AsyncDialogue(VariableStorage);

                    dialogue.Responder = this;

                    dialogue.LogDebugMessage = delegate (string message)
                    {
                        if (verboseLogging)
                        {
                            Debug.Log(message, this);
                        }
                    };
                    dialogue.LogErrorMessage = delegate (string message)
                    {
                        Debug.LogError(message, this);
                    };

                    if (yarnProject != null)
                    {
                        dialogue.Program = yarnProject.Program;
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
                    if (verboseLogging)
                    {
                        Debug.Log($"Dialogue Runner has no Variable Storage; creating a {nameof(InMemoryVariableStorage)}", this);
                    }
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

                    if (verboseLogging)
                    {
                        Debug.Log($"Dialogue Runner has no LineProvider; creating a {nameof(BuiltinLocalisedLineProvider)}.", this);
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
        /// If true, will print Debug.Log messages every time it enters a
        /// node, and other frequent events.
        /// </summary>
        [Tooltip("If true, will print Debug.Log messages every time it enters a node, and other frequent events")]
        public bool verboseLogging = false;

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
        /// <seealso cref="RunDialogue(string)"/>
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

        [SerializeField] private bool allowOptionFallthrough = true;

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
        private CancellationTokenSource? currentContentCancellationSource;
        private CancellationTokenSource? currentContentHurryUpSource;
        private YarnTaskCompletionSource? dialogueCancellationCompletion;

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

        BasicFunctionLibrary lib = new();

        private void EnsureCommandDispatcherReady()
        {
            if (_commandDispatcher == null)
            {
                var actions = new Actions(this, lib);
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
        private async void Start()
        {
            if (autoStart)
            {
                try
                {
                    // there are numerous situations where kicking off dialogue immediately from Start causes annoying issues around timing of different game objects
                    // while these can all be fixed it it much easier in our case to just wait one frame before starting.
                    // This still has the same feel of the older start automatically but just simplifies so many things.
                    // For situations where you absolutely must start immediately call RunDialogue yourself.
                    await YarnTask.Yield();
                    await RunDialogue(startNode);
                }
                catch (System.OperationCanceledException ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// Stops the dialogue immediately, and cancels any currently running
        /// dialogue presenters.
        /// </summary>
        public async YarnTask Stop()
        {
            // if dialogue isn't running we can't stop it
            if (dialogueCancellationSource == null)
            {
                return;
            }
            // if we are in the process of cancelling we also can't stop the content
            if (dialogueCancellationCompletion != null)
            {
                return;
            }

            dialogueCancellationCompletion = new YarnTaskCompletionSource();

            // this calls handle completion callbacks which does the other clean up
            await Dialogue.Stop();

            await dialogueCancellationCompletion.Task;
            dialogueCancellationCompletion = null;
        }

        /// <summary>
        /// Called by Unity to cancel the current dialogue when the Dialogue
        /// Runner is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            dialogueCancellationSource?.Cancel();
        }

        public async ValueTask PrepareForLines(List<string> lineIDs, CancellationToken token)
        {
            await this.LineProvider.PrepareForLinesAsync(lineIDs, token);
        }

        public async ValueTask HandleDialogueComplete()
        {
            // cleaning up the old cancellation token
            currentContentCancellationSource?.Dispose();
            currentContentCancellationSource = null;
            currentContentHurryUpSource?.Dispose();
            currentContentHurryUpSource = null;

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

            // finally we flag the cancellation as done
            // this lets stop know that all views have been informed as to the cancellation
            dialogueCancellationCompletion?.TrySetResult();
        }

        public async ValueTask HandleNodeComplete(string node, CancellationToken token)
        {
            foreach (var presenter in dialoguePresenters)
            {
                if (presenter == null)
                {
                    continue;
                }

                if (presenter.enabled == false)
                {
                    continue;
                }

                await presenter.OnNodeExit(node, token);
            }
        }

        public async ValueTask HandleNodeStart(string node, CancellationToken token)
        {
            foreach (var presenter in dialoguePresenters)
            {
                if (presenter == null)
                {
                    continue;
                }

                if (presenter.enabled == false)
                {
                    continue;
                }

                await presenter.OnNodeEnter(node, token);
            }
        }

        // this one should be fundamentally changed IMO
        // if only to be able to give commands a cancellation token
        // but for now it just does what the current one does
        public async ValueTask HandleCommand(Command command, CancellationToken token)
        {
            // if we have an existing cancellation source we will still dispose of it before creating a new one linked to the dialogue token
            // but this is HIGHLY likely indicative of an issue inside the dialogue runner
            if (currentContentCancellationSource != null)
            {
                Debug.LogWarning("Encountered a non-null current content cancellation token during Handle Command, this is likely a bug.");
            }
            currentContentCancellationSource?.Dispose();
            currentContentHurryUpSource?.Dispose();

            var mainToken = dialogueCancellationSource?.Token ?? CancellationToken.None;
            currentContentCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(mainToken, token);

            // now we make a new dependant hurry up cancellation token
            currentContentHurryUpSource = CancellationTokenSource.CreateLinkedTokenSource(currentContentCancellationSource.Token);
            var metaToken = new LineCancellationToken
            {
                NextContentToken = currentContentCancellationSource.Token,
                HurryUpToken = currentContentHurryUpSource.Token,
            };

            CommandDispatchResult dispatchResult = this.CommandDispatcher.DispatchCommand(command.Text, this, metaToken);

            var parts = SplitCommandText(command.Text);
            string commandName = parts.ElementAtOrDefault(0);

            switch (dispatchResult.Status)
            {
                case CommandDispatchResult.StatusType.Succeeded:
                    // The command has successfully dispatched, but has not
                    // yet finished running. Wait for it to finish.
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
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Internal error: Unknown command dispatch result status {dispatchResult}");
            }

            // finally we need to clean up our tokens for the next piece of content
            currentContentCancellationSource?.Dispose();
            currentContentHurryUpSource?.Dispose();
            currentContentCancellationSource = null;
            currentContentHurryUpSource = null;
        }

        private async YarnTask PresentLine(LocalizedLine line, CancellationToken token)
        {
            // if we have an existing cancellation source for this line we will still dispose of it before creating a new one linked to the dialogue token
            // but this is HIGHLY likely indicative of an issue inside the dialogue runner
            if (currentContentCancellationSource != null)
            {
                Debug.LogWarning("Encountered a non-null current content cancellation token during Present Line, this is likely a bug.");
            }

            currentContentCancellationSource?.Dispose();
            currentContentHurryUpSource?.Dispose();

            currentContentCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            // now we make a new dependant hurry up cancellation token
            currentContentHurryUpSource = CancellationTokenSource.CreateLinkedTokenSource(currentContentCancellationSource.Token);
            var metaToken = new LineCancellationToken
            {
                NextContentToken = currentContentCancellationSource.Token,
                HurryUpToken = currentContentHurryUpSource.Token,
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

                // Tell all of our views to run this line, and give them a
                // cancellation token they can use to interrupt the line if needed.
                async YarnTask RunLineAndInvokeCompletion(DialoguePresenterBase view, LocalizedLine line, LineCancellationToken token)
                {
                    try
                    {
                        // Run the line and wait for it to finish
                        await view.RunLineAsync(line, token);
                    }
                    catch (System.OperationCanceledException)
                    {
                        // The line presenter cancelled (rather than returning.)
                        // This probably wasn't intended - they should clean up
                        // and return null.
                        Debug.LogWarning($"Dialogue presenter {view.name} threw an {nameof(System.OperationCanceledException)} when running its {nameof(DialoguePresenterBase.RunLineAsync)} method. Dialogue presenters should not throw this exception; instead, clean up any needed user-facing content, and return.", view);
                    }
                    catch (System.Exception e)
                    {
                        // If a dialogue presenter throws an exception, we need
                        // to return, because the dialogue runner is waiting for
                        // our task to complete. We'll log the exception so that
                        // it's not lost, and exit here.
                        Debug.LogException(e, view);
                    }
                }

                YarnTask task = RunLineAndInvokeCompletion(view, line, metaToken);

                pendingTasks.Add(task);
            }

            // Wait for all line view tasks to finish delivering the line.
            await YarnTask.WhenAll(pendingTasks);

            // We're done; dispose of the cancellation sources. (Null-check them because if we're leaving play mode, then these references may no longer be valid.)
            currentContentCancellationSource?.Dispose();
            currentContentHurryUpSource?.Dispose();
            currentContentCancellationSource = null;
            currentContentHurryUpSource = null;
        }

        public ValueTask<IConvertible> thunk(string functionName, IConvertible[] parameters, CancellationToken token)
        {
            return lib.Invoke(functionName, parameters, token);
        }

        public bool TryGetFunctionDefinition(string name, out FunctionDefinition function)
        {
            return lib.TryGetFunctionDefinition(name, out function);
        }

        public Dictionary<string, FunctionDefinition> allDefinitions => lib.allDefinitions;

        public void DeregisterFunction(string name)
        {
            lib.DeregisterFunction(name);
        }


        public async ValueTask HandleLine(Line line, CancellationToken token)
        {
            var metaTokenSource = CancellationTokenSource.CreateLinkedTokenSource(dialogueCancellationSource?.Token ?? CancellationToken.None, token);

            if (this == null || metaTokenSource.IsCancellationRequested)
            {
                return;
            }

            var localisedLine = await LineProvider.GetLocalizedLineAsync(line, metaTokenSource.Token);
            localisedLine.Source = this;

            if (localisedLine == LocalizedLine.InvalidLine)
            {
                Debug.LogError($"Failed to get a localised line for {line.ID}!");
            }

            await PresentLine(localisedLine, metaTokenSource.Token);
        }

        public async ValueTask<int> HandleOptions(OptionSet options, CancellationToken token)
        {
            // if we have an existing cancellation source for this option group we will still dispose of it before creating a new one linked to the dialogue token
            // but this is HIGHLY likely indicative of an issue inside the dialogue runner
            if (currentContentCancellationSource != null)
            {
                Debug.LogWarning("Encountered a non-null current content cancellation token during Handle Options, this is likely a bug.");
            }

            currentContentCancellationSource?.Dispose();
            currentContentHurryUpSource?.Dispose();

            var metaTokenSource = CancellationTokenSource.CreateLinkedTokenSource(dialogueCancellationSource?.Token ?? CancellationToken.None, token);
            currentContentCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(metaTokenSource.Token);

            // now we make a new dependant hurry up cancellation token
            currentContentHurryUpSource = CancellationTokenSource.CreateLinkedTokenSource(currentContentCancellationSource.Token);
            var metaToken = new LineCancellationToken
            {
                NextContentToken = currentContentCancellationSource.Token,
                HurryUpToken = currentContentHurryUpSource.Token,
            };

            DialogueOption[] localisedOptions = new DialogueOption[options.Options.Length];
            for (int i = 0; i < options.Options.Length; i++)
            {
                var opt = options.Options[i];
                LocalizedLine localizedLine = await LineProvider.GetLocalizedLineAsync(opt.Line, currentContentCancellationSource.Token);
                localizedLine.Source = this;

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

            DialogueOption? selectedOption = null;
            async YarnTask WaitForOptionsView(DialoguePresenterBase view)
            {
                try
                {
                    var result = await view.RunOptionsAsync(localisedOptions, metaToken);
                    if (result != null)
                    {
                        // We no longer need the other views, so tell them to stop
                        // by cancelling the option selection.
                        currentContentCancellationSource.Cancel();
                        selectedOption = result;
                    }
                }
                catch (System.OperationCanceledException)
                {
                    // The options presenter cancelled (rather than returning
                    // null.) This probably wasn't intended - they should clean
                    // up and return null.
                    Debug.LogWarning($"Dialogue presenter {view.name} threw an {nameof(System.OperationCanceledException)} when running its {nameof(DialoguePresenterBase.RunOptionsAsync)} method. Dialogue presenters should not throw this exception; instead, clean up any needed user-facing content, and return null.", view);
                }
                catch (System.Exception ex)
                {
                    // If a dialogue presenter throws an exception, we still
                    // need to return a value, because the dialogue runner is
                    // waiting for our task to complete. We'll log the exception
                    // so that it's not lost, and exit here.
                    Debug.LogException(ex, view);
                    return;
                }
            }

            var pendingTasks = new List<YarnTask>();
            foreach (var view in this.dialoguePresenters)
            {
                if (view == null)
                {
                    continue;
                }
                if (!view.enabled)
                {
                    continue;
                }
                pendingTasks.Add(WaitForOptionsView(view));
            }
            await YarnTask.WhenAll(pendingTasks);

            // at this point now every view has finished their handling of the options
            // the first one to return a non-null value will be the one that is chosen option
            // or if everyone returned null that's an error/it is to be skipped

            // at this point the option is "finished" contentwise and selection needs to happen back on the VM side
            // so we can clear up our cancellation tokens
            currentContentCancellationSource?.Dispose();
            currentContentHurryUpSource?.Dispose();
            currentContentCancellationSource = null;
            currentContentHurryUpSource = null;

            if (selectedOption == null)
            {
                if (allowOptionFallthrough)
                {
                    return Yarn.AsyncDialogue.NoOptionSelected;
                }
                else
                {
                    // there are two situations now because it is possible we were cancelled during this moment
                    // in that case we throw the operation cancelled exception knowing that the VM will know how to handle it
                    if (dialogueCancellationSource != null && dialogueCancellationSource.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                    else
                    {    
                        // None of our option views returned an option, and our dialogue wasn't cancelled, and we've said we don't want to do fallthrough.
                        // That's not allowed, because we don't know what to do next!
                        Debug.LogError($"All presenters have returned from {nameof(DialoguePresenterBase.RunOptionsAsync)} but none returned an option, and fallthrough is disabled. This is not allowed.");
                        throw new InvalidOperationException($"All presenters have returned from {nameof(DialoguePresenterBase.RunOptionsAsync)} but none returned an option, and fallthrough is disabled. This is not allowed.");
                    }
                }
            }
            else
            {
                if (runSelectedOptionAsLine)
                {
                    await PresentLine(selectedOption.Line, metaTokenSource.Token);
                }
                return selectedOption.DialogueOptionID;
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

            Dialogue.Program = project.Program;
        }

        [System.Obsolete("This method kicks off dialogue but doesn't await it's completion. Prefer RunDialogue where possible")]
        public void StartDialogue(string nodeName)
        {
            RunDialogue(nodeName).Forget();
        }

        /// <summary>
        /// Starts running a node of dialogue.
        /// </summary>
        /// <remarks><paramref name="nodeName"/> must be the name of a node in
        /// <see cref="YarnProject"/>.</remarks>
        /// <param name="nodeName">The name of the node to run.</param>
        public async YarnTask RunDialogue(string nodeName)
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

            // need to check here also, this should be null
            if (dialogueCancellationSource != null)
            {
                Debug.LogError($"Can't start dialogue: there is dialogue already being run on this dialogue runner", this);
                return;
            }

            dialogueCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.destroyCancellationToken);
            LineProvider.YarnProject = yarnProject;

            EnsureCommandDispatcherReady();

            Dialogue.Program = yarnProject.Program;
            await Dialogue.SetNode(nodeName);

            ApplySaliencyStrategy();

            var tasks = new List<YarnTask>();
            foreach (var view in DialoguePresenters)
            {
                if (view == null)
                {
                    continue;
                }
                if (view.enabled == false)
                {
                    continue;
                }
                tasks.Add(view.OnDialogueStartedAsync());
            }
            await YarnTask.WhenAll(tasks);

            await Dialogue.Start();

            // cleaning up the main token
            // we need this nulled out for the purposes of external entities asking if dialogue is running
            // and for our own early outing to avoid double starting dialogue
            dialogueCancellationSource?.Dispose();
            dialogueCancellationSource = null;
        }


        public void RequestNextContent()
        {
            currentContentCancellationSource?.Cancel();
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
        [Obsolete("This method has been superceded by RequestNextContent and now forwards calls to that")]
        public void RequestNextLine()
        {
            RequestNextContent();
        }

        public void RequestHurryUpContent()
        {
            currentContentHurryUpSource?.Cancel();
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
        [Obsolete("This method has been superceded by RequestHurryUpContent and now forwards calls to that")]
        public void RequestHurryUpLine()
        {
            RequestHurryUpContent();
        }

        [Obsolete("This method has been superceded by RequestHurryUpContent and now forwards calls to that")]
        public void RequestHurryUpOption()
        {
            RequestHurryUpContent();
        }
    }
}
