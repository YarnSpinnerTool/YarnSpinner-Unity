﻿/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

*/

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace Yarn.Unity
{
    /// <summary>
    /// The [DialogueRunner]({{|ref
    /// "/docs/unity/components/dialogue-runner.md"|}}) component acts as
    /// the interface between your game and Yarn Spinner.
    /// </summary>
    [AddComponentMenu("Scripts/Yarn Spinner/Dialogue Runner"), HelpURL("https://yarnspinner.dev/docs/unity/components/dialogue-runner/")]
    public class DialogueRunner : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="YarnProject"/> asset that should be loaded on
        /// scene start.
        /// </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("yarnProgram")]
        public YarnProject yarnProject;

        /// <summary>
        /// The variable storage object.
        /// </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("variableStorage")]
        [SerializeField] internal VariableStorageBehaviour _variableStorage;

        /// <inheritdoc cref="_variableStorage"/>
        public VariableStorageBehaviour VariableStorage
        {
            get => _variableStorage; 
            set
            {
                _variableStorage = value;
                if (_dialogue != null)
                {
                    _dialogue.VariableStorage = value;
                }
            }
        }

        /// <summary>
        /// The View classes that will present the dialogue to the user.
        /// </summary>
        public DialogueViewBase[] dialogueViews;

        /// <summary>The name of the node to start from.</summary>
        /// <remarks>
        /// This value is used to select a node to start from when <see
        /// cref="startAutomatically"/> is called.
        /// </remarks>
        public string startNode = Yarn.Dialogue.DefaultStartNodeName;

        /// <summary>
        /// Whether the DialogueRunner should automatically start running
        /// dialogue after the scene loads.
        /// </summary>
        /// <remarks>
        /// The node specified by <see cref="startNode"/> will be used.
        /// </remarks>
        public bool startAutomatically = true;

        /// <summary>
        /// Whether the DialogueRunner should automatically proceed to the
        /// next line once a line has been finished.
        /// </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("continueNextLineOnLineFinished")]
        public bool automaticallyContinueLines;

        /// <summary>
        /// If true, when an option is selected, it's as though it were a
        /// line.
        /// </summary>
        public bool runSelectedOptionAsLine;

        public LineProviderBehaviour lineProvider;

        /// <summary>
        /// If true, will print Debug.Log messages every time it enters a
        /// node, and other frequent events.
        /// </summary>
        [Tooltip("If true, will print Debug.Log messages every time it enters a node, and other frequent events")]
        public bool verboseLogging = true;

        /// <summary>
        /// Gets a value that indicates if the dialogue is actively
        /// running.
        /// </summary>
        public bool IsDialogueRunning { get; set; }

        /// <summary>
        /// A type of <see cref="UnityEvent"/> that takes a single string
        /// parameter. 
        /// </summary>
        /// <remarks>
        /// A concrete subclass of <see cref="UnityEvent"/> is needed in
        /// order for Unity to serialise the type correctly.
        /// </remarks>
        [Serializable]
        public class StringUnityEvent : UnityEvent<string> { }

        /// <summary>
        /// A Unity event that is called when a node starts running.
        /// </summary>
        /// <remarks>
        /// This event receives as a parameter the name of the node that is
        /// about to start running.
        /// </remarks>
        /// <seealso cref="Dialogue.NodeStartHandler"/>
        public StringUnityEvent onNodeStart;

        /// <summary>
        /// A Unity event that is called when a node is complete.
        /// </summary>
        /// <remarks>
        /// This event receives as a parameter the name of the node that
        /// just finished running.
        /// </remarks>
        /// <seealso cref="Dialogue.NodeCompleteHandler"/>
        public StringUnityEvent onNodeComplete;

        /// <summary>
        /// A Unity event that is called once the dialogue has completed.
        /// </summary>
        /// <seealso cref="Dialogue.DialogueCompleteHandler"/>
        public UnityEvent onDialogueComplete;

        /// <summary>
        /// A <see cref="StringUnityEvent"/> that is called when a <see
        /// cref="Command"/> is received.
        /// </summary>
        /// <remarks>
        /// Use this method to dispatch a command to other parts of your
        /// game. This method is only called if the <see cref="Command"/>
        /// has not been handled by a command handler that has been added
        /// to the <see cref="DialogueRunner"/>, or by a method on a <see
        /// cref="MonoBehaviour"/> in the scene with the attribute <see
        /// cref="YarnCommandAttribute"/>. {{|note|}} When a command is
        /// delivered in this way, the <see cref="DialogueRunner"/> will
        /// not pause execution. If you want a command to make the
        /// DialogueRunner pause execution, see <see
        /// cref="AddCommandHandler(string, CommandHandler)"/>. {{|/note|}}
        ///
        /// This method receives the full text of the command, as it
        /// appears between the `<![CDATA[<<]]>` and `<![CDATA[>>]]>`
        /// markers.
        /// </remarks>
        /// <seealso cref="AddCommandHandler(string, CommandHandler)"/>
        /// <seealso cref="AddCommandHandler(string, CommandHandler)"/>
        /// <seealso cref="YarnCommandAttribute"/>
        public StringUnityEvent onCommand;

        /// <summary>
        /// Gets the name of the current node that is being run.
        /// </summary>
        /// <seealso cref="Dialogue.currentNode"/>
        public string CurrentNodeName => Dialogue.CurrentNode;

        /// <summary>
        /// Gets the underlying <see cref="Dialogue"/> object that runs the
        /// Yarn code.
        /// </summary>
        public Dialogue Dialogue => _dialogue ?? (_dialogue = CreateDialogueInstance());

        /// <summary>
        /// The collection of registered YarnCommand-tagged methods.
        /// Populated in the <see cref="InitializeClass"/> method.
        /// </summary>
        private static Dictionary<string, MethodInfo> _yarnCommands = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// A flag used to detect if an options handler attempts to set the
        /// selected option on the same frame that options were provided.
        /// </summary>
        /// <remarks>
        /// This field is set to false by <see
        /// cref="HandleOptions(OptionSet)"/> immediately before calling
        /// <see cref="DialogueViewBase.RunOptions(DialogueOption[],
        /// Action{int})"/> on all objects in <see cref="dialogueViews"/>,
        /// and set to true immediately after. If a call to <see
        /// cref="DialogueViewBase.RunOptions(DialogueOption[],
        /// Action{int})"/> calls its completion hander on the same frame,
        /// an error is generated.
        /// </remarks>
        private bool IsOptionSelectionAllowed = false;

        /// <summary>
        /// Finds all MonoBehaviour types in the loaded assemblies, and
        /// looks for all methods that are tagged with YarnCommand.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        static void InitializeClass()
        {

            // Find all assemblies
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            // In each assembly, find all types that descend from
            // MonoBehaviour
            foreach (var assembly in allAssemblies)
            {
                foreach (var type in assembly.GetLoadableTypes().Where(t => t.IsSubclassOf(typeof(MonoBehaviour))))
                {

                    // We only care about MonoBehaviours
                    if (typeof(MonoBehaviour).IsAssignableFrom(type) == false)
                    {
                        continue;
                    }

                    // Find all methods on each type that have the
                    // YarnCommand attribute
                    foreach (var method in type.GetMethods())
                    {
                        if (method.DeclaringType != type)
                        {
                            // This method was not declared in this class,
                            // and was inherited from a parent class. Don't
                            // attempt to register this method, because
                            // we'll get a conflict.
                            continue;
                        }

                        var attributes = new List<YarnCommandAttribute>(method.GetCustomAttributes<YarnCommandAttribute>(false));

                        if (attributes.Count > 0)
                        {
                            // This method has the YarnCommand attribute!
                            // The compiler enforces a single attribute of
                            // this type on each members, so if we have n >
                            // 0, n == 1.
                            var att = attributes[0];

                            var name = att.CommandString;

                            try
                            {
                                // Cache the methodinfo
                                _yarnCommands.Add(name, method);
                            }
                            catch (ArgumentException)
                            {
                                MethodInfo existingDefinition = _yarnCommands[name];
                                Debug.LogError($"Can't add {method.DeclaringType.FullName}.{method.Name} for command {name} because it's already defined on {existingDefinition.DeclaringType.FullName}.{existingDefinition.Name}");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Replaces this DialogueRunner's yarn project with the provided
        /// project.
        /// </summary>        
        public void SetProject(YarnProject newProject)
        {
            Dialogue.SetProgram(newProject.YarnProgram);
            lineProvider.YarnProject = newProject;
        }

        /// <summary>
        /// Start the dialogue from a specific node.
        /// </summary>
        /// <param name="startNode">The name of the node to start running
        /// from.</param>
        public void StartDialogue(string startNode)
        {
            // If the dialogue is currently executing instructions, then
            // calling ContinueDialogue() at the end of this method will
            // cause confusing results. Report an error and stop here.
            if (Dialogue.IsActive) {
                Debug.LogError($"Can't start dialogue from node {startNode}: the dialogue is currently in the middle of running. Stop the dialogue first.");
                return;
            }

            // Stop any processes that might be running already
            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null || dialogueView.enabled == false) {
                    continue;
                }

                dialogueView.StopAllCoroutines();
            }

            // Get it going

            // Mark that we're in conversation.
            IsDialogueRunning = true;

            // Signal that we're starting up.
            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null || dialogueView.enabled == false) continue;

                dialogueView.DialogueStarted();
            }

            // Request that the dialogue select the current node. This
            // will prepare the dialogue for running; as a side effect,
            // our prepareForLines delegate may be called.
            Dialogue.SetNode(startNode);

            if (lineProvider.LinesAvailable == false)
            {
                // The line provider isn't ready to give us our lines
                // yet. We need to start a coroutine that waits for
                // them to finish loading, and then runs the dialogue.
                StartCoroutine(ContinueDialogueWhenLinesAvailable());
            }
            else
            {
                ContinueDialogue();
            }
        }

        private IEnumerator ContinueDialogueWhenLinesAvailable()
        {
            // Wait until lineProvider.LinesAvailable becomes true
            while (lineProvider.LinesAvailable == false)
            {
                yield return null;
            }

            // And then run our dialogue.
            ContinueDialogue();
        }

        /// <summary>
        /// Starts running the dialogue again.
        /// </summary>
        /// <remarks>
        /// If <paramref name="nodeName"/> is null, the node specified by
        /// <see cref="startNode"/> is attempted, followed the currently
        /// running node. If none of these options are available, an <see
        /// cref="ArgumentNullException"/> is thrown.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when a node to
        /// restart the dialogue from cannot be found.</exception>
        [Obsolete("Use " + nameof(StartDialogue) + "(nodeName) instead.")]
        public void ResetDialogue(string nodeName = null)
        {
            nodeName = nodeName ?? startNode ?? CurrentNodeName ?? throw new ArgumentNullException($"Cannot reset dialogue: couldn't figure out a node to restart the dialogue from.");

            StartDialogue(nodeName);
        }

        /// <summary>
        /// Unloads all nodes from the <see cref="Dialogue"/>.
        /// </summary>
        public void Clear()
        {
            Assert.IsFalse(IsDialogueRunning, "You cannot clear the dialogue system while a dialogue is running.");
            Dialogue.UnloadAll();
        }

        /// <summary>
        /// Stops the <see cref="Dialogue"/>.
        /// </summary>
        public void Stop()
        {
            IsDialogueRunning = false;
            Dialogue.Stop();
        }

        /// <summary>
        /// Returns `true` when a node named `nodeName` has been loaded.
        /// </summary>
        /// <param name="nodeName">The name of the node.</param>
        /// <returns>`true` if the node is loaded, `false`
        /// otherwise/</returns>
        public bool NodeExists(string nodeName) => Dialogue.NodeExists(nodeName);

        /// <summary>
        /// Returns the collection of tags that the node associated with
        /// the node named `nodeName`.
        /// </summary>
        /// <param name="nodeName">The name of the node.</param>
        /// <returns>The collection of tags associated with the node, or
        /// `null` if no node with that name exists.</returns>
        public IEnumerable<string> GetTagsForNode(String nodeName) => Dialogue.GetTagsForNode(nodeName);

        /// <summary>
        /// Adds a command handler. Dialogue will pause execution after the
        /// command is called.
        /// </summary>
        /// <remarks>
        /// When this command handler has been added, it can be called from
        /// your Yarn scripts like so:
        ///
        /// <![CDATA[```yarn <<commandName param1 param2>> ```]]>
        ///
        /// When this command handler is called, the <see
        /// cref="DialogueRunner"/> will stop executing code. To make the
        /// <see cref="DialogueRunner"/> resume execution, call the
        /// onComplete action that the <see cref="CommandHandler"/>
        /// receives. 
        /// </remarks>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="handler">The <see cref="CommandHandler"/> that
        /// will be invoked when the command is called.</param>
        private void AddCommandHandler(string commandName, Delegate handler)
        {
            if (commandHandlers.ContainsKey(commandName))
            {
                Debug.LogError($"Cannot add a command handler for {commandName}: one already exists");
                return;
            }
            commandHandlers.Add(commandName, handler);
        }

        public void AddCommandHandler(string commandHandler, System.Func<Coroutine> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1>(string commandHandler, System.Func<T1, Coroutine> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1, T2>(string commandHandler, System.Func<T1, T2, Coroutine> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1, T2, T3>(string commandHandler, System.Func<T1, T2, T3, Coroutine> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1, T2, T3, T4>(string commandHandler, System.Func<T1, T2, T3, T4, Coroutine> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandHandler, System.Func<T1, T2, T3, T4, T5, Coroutine> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandHandler, System.Func<T1, T2, T3, T4, T5, T6, Coroutine> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler(string commandHandler, System.Action handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1>(string commandHandler, System.Action<T1> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1, T2>(string commandHandler, System.Action<T1, T2> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1, T2, T3>(string commandHandler, System.Action<T1, T2, T3> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1, T2, T3, T4>(string commandHandler, System.Action<T1, T2, T3, T4> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandHandler, System.Action<T1, T2, T3, T4, T5> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandHandler, System.Action<T1, T2, T3, T4, T5, T6> handler)
        {
            AddCommandHandler(commandHandler, (Delegate)handler);
        }

        /// <summary>
        /// Removes a command handler.
        /// </summary>
        /// <param name="commandName">The name of the command to
        /// remove.</param>
        public void RemoveCommandHandler(string commandName)
        {
            commandHandlers.Remove(commandName);
        }

        /// <summary>
        /// Add a new function that returns a value, so that it can be
        /// called from Yarn scripts.
        /// </summary>        
        /// <remarks>
        /// When this function has been registered, it can be called from
        /// your Yarn scripts like so:
        ///
        /// <![CDATA[```yarn <<if myFunction(1, 2) == true>> myFunction
        /// returned true! <<endif>> ```]]>
        ///
        /// The `call` command can also be used to invoke the function:
        ///
        /// <![CDATA[```yarn <<call myFunction(1, 2)>> ```]]>    
        /// </remarks>
        /// <param name="implementation">The <see cref="Delegate"/> that
        /// should be invoked when this function is called.</param>
        /// <seealso cref="Library"/> 
        private void AddFunction(string name, Delegate implementation)
        {
            if (Dialogue.Library.FunctionExists(name))
            {
                Debug.LogError($"Cannot add function {name}: one already exists");
                return;
            }

            Dialogue.Library.RegisterFunction(name, implementation);
        }

        public void AddFunction<TResult>(string name, System.Func<TResult> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        public void AddFunction<TResult, T1>(string name, System.Func<TResult, T1> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        public void AddFunction<TResult, T1, T2>(string name, System.Func<TResult, T1, T2> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        public void AddFunction<TResult, T1, T2, T3>(string name, System.Func<TResult, T1, T2, T3> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        public void AddFunction<TResult, T1, T2, T3, T4>(string name, System.Func<TResult, T1, T2, T3, T4> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        public void AddFunction<TResult, T1, T2, T3, T4, T5>(string name, System.Func<TResult, T1, T2, T3, T4, T5> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        public void AddFunction<TResult, T1, T2, T3, T4, T5, T6>(string name, System.Func<TResult, T1, T2, T3, T4, T5, T6> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <summary>
        /// Remove a registered function.
        /// </summary>
        /// <remarks>
        /// After a function has been removed, it cannot be called from
        /// Yarn scripts.
        /// </remarks>
        /// <param name="name">The name of the function to remove.</param>
        /// <seealso cref="AddFunction{TResult}(string, Func{TResult})"/>
        public void RemoveFunction(string name) => Dialogue.Library.DeregisterFunction(name);

        /// <summary>
        /// Sets the dialogue views and makes sure the callback <see cref="DialogueViewBase.MarkLineComplete"/>
        /// will respond correctly.
        /// </summary>
        /// <param name="views">The array of views to be assigned.</param>
        public void SetDialogueViews(DialogueViewBase[] views)
        {
            dialogueViews = views;

            Action continueAction = OnViewUserIntentNextLine;
            foreach (var dialogueView in dialogueViews) {
                if (dialogueView == null) {
                    Debug.LogWarning("The 'Dialogue Views' field contains a NULL element.", gameObject);
                    continue;
                }

                dialogueView.onUserWantsLineContinuation = continueAction;
            }
        }

        #region Private Properties/Variables/Procedures

        /// <summary>
        /// The <see cref="LocalizedLine"/> currently being displayed on
        /// the dialogue views.
        /// </summary>
        internal LocalizedLine CurrentLine { get; private set; }

        /// <summary>
        ///  The collection of dialogue views that are currently either
        ///  delivering a line, or dismissing a line from being on screen.
        /// </summary>
        private readonly HashSet<DialogueViewBase> ActiveDialogueViews = new HashSet<DialogueViewBase>();

        Action<int> selectAction;

        /// Maps the names of commands to action delegates.
        Dictionary<string, Delegate> commandHandlers = new Dictionary<string, Delegate>();

        /// <summary>
        /// The underlying object that executes Yarn instructions
        /// and provides lines, options and commands.
        /// </summary>
        /// <remarks>
        /// Automatically created on first access.
        /// </remarks>
        
        private Dialogue _dialogue;

        /// <summary>
        /// The current set of options that we're presenting.
        /// </summary>
        /// <remarks>
        /// This value is <see langword="null"/> when the <see
        /// cref="DialogueRunner"/> is not currently presenting options.
        /// </remarks>
        private OptionSet currentOptions;

        void Awake()
        {
            if (lineProvider == null)
            {
                // If we don't have a line provider, create a
                // TextLineProvider and make it use that.

                // Create the temporary line provider and the line database
                lineProvider = gameObject.AddComponent<TextLineProvider>();

                // Let the user know what we're doing.
                if (verboseLogging)
                {
                    Debug.Log($"Dialogue Runner has no LineProvider; creating a {nameof(TextLineProvider)}.", this);
                }
            }

            
        }

        /// Start the dialogue
        void Start()
        {
            if (dialogueViews.Length == 0)
            {
                Debug.LogWarning($"Dialogue Runner doesn't have any dialogue views set up. No lines or options will be visible.");
            }

            // Give each dialogue view the continuation action, which
            // they'll call to pass on the user intent to move on to the
            // next line (or interrupt the current one).
            System.Action continueAction = OnViewUserIntentNextLine;
            foreach (var dialogueView in dialogueViews)
            {
                // Skip any null or disabled dialogue views
                if (dialogueView == null || dialogueView.enabled == false)
                {
                    continue;
                }

                dialogueView.onUserWantsLineContinuation = continueAction;
            }

            if (yarnProject != null)
            {
                if (Dialogue.IsActive)
                {
                    Debug.LogError($"DialogueRunner wanted to load a Yarn Project in its Start method, but the Dialogue was already running one. The Dialogue Runner may not behave as you expect.");
                }

                Dialogue.SetProgram(yarnProject.YarnProgram);

                lineProvider.YarnProject = yarnProject;

                if (startAutomatically)
                {
                    StartDialogue(startNode);
                }
            }
        }

        Dialogue CreateDialogueInstance()
        {
            if (VariableStorage == null)
            {
                // If we don't have a variable storage, create an
                // InMemoryVariableStorage and make it use that.

                VariableStorage = gameObject.AddComponent<InMemoryVariableStorage>();

                // Let the user know what we're doing.
                if (verboseLogging)
                {
                    Debug.Log($"Dialogue Runner has no Variable Storage; creating a {nameof(InMemoryVariableStorage)}", this);
                }
            }

            // Create the main Dialogue runner, and pass our
            // variableStorage to it
            var dialogue = new Yarn.Dialogue(VariableStorage)
            {

                // Set up the logging system.
                LogDebugMessage = delegate (string message)
                {
                    if (verboseLogging)
                    {
                        Debug.Log(message);
                    }
                },
                LogErrorMessage = delegate (string message)
                {
                    Debug.LogError(message);
                },

                LineHandler = HandleLine,
                CommandHandler = HandleCommand,
                OptionsHandler = HandleOptions,
                NodeStartHandler = (node) =>
                {
                    onNodeStart?.Invoke(node);
                },
                NodeCompleteHandler = (node) =>
                {
                    onNodeComplete?.Invoke(node);
                },
                DialogueCompleteHandler = HandleDialogueComplete,
                PrepareForLinesHandler = PrepareForLines,
            };

            // Yarn Spinner defines two built-in commands: "wait", and
            // "stop". Stop is defined inside the Virtual Machine (the
            // compiler traps it and makes it a special case.) Wait is
            // defined here in Unity.
            AddCommandHandler("wait", (float duration) => StartCoroutine(DoHandleWait(duration)));

            selectAction = SelectedOption;

            return dialogue;
        }

        IEnumerator DoHandleWait(float duration)
        {
            yield return new WaitForSeconds(duration);

        }

        void HandleOptions(OptionSet options)
        {
            currentOptions = options;

            DialogueOption[] optionSet = new DialogueOption[options.Options.Length];
            for (int i = 0; i < options.Options.Length; i++)
            {

                // Localize the line associated with the option
                var localisedLine = lineProvider.GetLocalizedLine(options.Options[i].Line);
                var text = Dialogue.ExpandSubstitutions(localisedLine.RawText, options.Options[i].Line.Substitutions);
                localisedLine.Text = Dialogue.ParseMarkup(text);

                optionSet[i] = new DialogueOption
                {
                    TextID = options.Options[i].Line.ID,
                    DialogueOptionID = options.Options[i].ID,
                    Line = localisedLine,
                    IsAvailable = options.Options[i].IsAvailable,
                };
            }
            
            // Don't allow selecting options on the same frame that we
            // provide them
            IsOptionSelectionAllowed = false;

            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null || dialogueView.enabled == false) continue;

                dialogueView.RunOptions(optionSet, selectAction);
            }

            IsOptionSelectionAllowed = true;
        }

        void HandleDialogueComplete()
        {
            IsDialogueRunning = false;
            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null || dialogueView.enabled == false) continue;

                dialogueView.DialogueComplete();
            }
            onDialogueComplete.Invoke();
        }

        void HandleCommand(Command command)
        {
            bool wasValidCommand;

            // Try looking in the command handlers first
            wasValidCommand = DispatchCommandToRegisteredHandlers(command, ContinueDialogue);

            if (wasValidCommand)
            {
                // This was a valid command.
                return;
            }

            // We didn't find it in the comand handlers. Try looking in the
            // game objects. If it is, continue dialogue.
            wasValidCommand = DispatchCommandToGameObject(command, ContinueDialogue);

            if (wasValidCommand)
            {
                // We found an object and method to invoke as a Yarn
                // command. 
                return;
            }

            // We didn't find a method in our C# code to invoke. Try
            // invoking on the publicly exposed UnityEvent.
            onCommand?.Invoke(command.Text);

            if ( onCommand == null || onCommand.GetPersistentEventCount() == 0 ) {
                Debug.LogError($"No Command <<{command.Text}>> was found. Did you remember to use the YarnCommand attribute or AddCommandHandler() function in C#?");
            }

            ContinueDialogue();
        
            return;
        }

        /// Forward the line to the dialogue UI.
        void HandleLine(Line line)
        {
            // Get the localized line from our line provider
            CurrentLine = lineProvider.GetLocalizedLine(line);

            // Expand substitutions
            var text = Dialogue.ExpandSubstitutions(CurrentLine.RawText, CurrentLine.Substitutions);

            if (text == null)
            {
                Debug.LogWarning($"Dialogue Runner couldn't expand substitutions in Yarn Project [{ yarnProject.name }] node [{ CurrentNodeName }] with line ID [{ CurrentLine.TextID }]. "
                    + "This usually happens because it couldn't find text in the Localization. The line may not be tagged properly. "
                    + "Try re-importing this Yarn Program. "
                    + "For now, Dialogue Runner will swap in CurrentLine.RawText.");
                text = CurrentLine.RawText;
            }

            // Render the markup
            CurrentLine.Text = Dialogue.ParseMarkup(text);

            CurrentLine.Status = LineStatus.Presenting;

            // Clear the set of active dialogue views, just in case
            ActiveDialogueViews.Clear();

            // the following is broken up into two stages because otherwise if the 
            // first view happens to finish first once it calls dialogue complete
            // it will empty the set of active views resulting in the line being considered
            // finished by the runner despite there being a bunch of views still waiting
            // so we do it over two loops.
            // the first finds every active view and flags it as such
            // the second then goes through them all and gives them the line

            // Mark this dialogue view as active
            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null || dialogueView.enabled == false) continue;

                ActiveDialogueViews.Add(dialogueView);
            }
            // Send line to all active dialogue views
            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null || dialogueView.enabled == false) continue;

                dialogueView.RunLine(CurrentLine,
                    () => DialogueViewCompletedDelivery(dialogueView));
            }
        }

        /// <summary>
        /// Indicates to the DialogueRunner that the user has selected an
        /// option
        /// </summary>
        /// <param name="optionIndex">The index of the option that was
        /// selected.</param>
        /// <exception cref="InvalidOperationException">Thrown when the
        /// <see cref="IsOptionSelectionAllowed"/> field is <see
        /// langword="true"/>, which is the case when <see
        /// cref="DialogueViewBase.RunOptions(DialogueOption[],
        /// Action{int})"/> is in the middle of being called.</exception>
        void SelectedOption(int optionIndex)
        {
            if (IsOptionSelectionAllowed == false) {
                throw new InvalidOperationException("Selecting an option on the same frame that options are provided is not allowed. Wait at least one frame before selecting an option.");
            }
            
            // Mark that this is the currently selected option in the
            // Dialogue
            Dialogue.SetSelectedOption(optionIndex);

            if (runSelectedOptionAsLine)
            {
                foreach (var option in currentOptions.Options)
                {
                    if (option.ID == optionIndex)
                    {
                        HandleLine(option.Line);
                        return;
                    }
                }

                Debug.LogError($"Can't run selected option ({optionIndex}) as a line: couldn't find the option's associated {nameof(Line)} object");
                ContinueDialogue();
            }
            else
            {
                ContinueDialogue();
            }

        }


        /// <summary>
        /// Parses the command string inside <paramref name="command"/>,
        /// attempts to find a suitable handler from <see
        /// cref="commandHandlers"/>, and invokes it if found.
        /// </summary>
        /// <param name="command">The <see cref="Command"/> to run.</param>
        /// <param name="onSuccessfulDispatch">A method to run if a command
        /// was successfully dispatched to a game object. This method is
        /// not called if a registered command handler is not
        /// found.</param>
        /// <returns>True if the command was dispatched to a game object;
        /// false otherwise.</returns>
        bool DispatchCommandToRegisteredHandlers(Command command, Action onSuccessfulDispatch)
        {
            return DispatchCommandToRegisteredHandlers(command.Text, onSuccessfulDispatch);
        }

        /// <inheritdoc cref="DispatchCommandToRegisteredHandlers(Command,
        /// Action)"/>
        /// <param name="command">The text of the command to
        /// dispatch.</param>
        internal bool DispatchCommandToRegisteredHandlers(String command, Action onSuccessfulDispatch)
        {
            List<string> commandTokens = new List<string>(SplitCommandText(command));

            if (commandTokens.Count == 0)
            {
                // Nothing to do
                return false;
            }

            var firstWord = commandTokens[0];

            if (commandHandlers.ContainsKey(firstWord) == false)
            {
                // We don't have a registered handler for this command, but
                // some other part of the game might.
                return false;
            }

            // Get all tokens after the name of the command
            var remainingWords = new string[commandTokens.Count - 1];

            var @delegate = commandHandlers[firstWord];
            var methodInfo = @delegate.Method;

            
            // Copy everything except the first word from the array
            commandTokens.CopyTo(1, remainingWords, 0, remainingWords.Length);

            // Take the list of words, and prepend the onComplete delegate
            // we were given - it's always the first parameter
            var rawParameters = new List<string>(remainingWords);

            object[] finalParameters;

            try
            {
                finalParameters = GetPreparedParametersForMethod(rawParameters.ToArray(), methodInfo);
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"Can't run command {firstWord}: {e.Message}");
                return false;
            }

            if (typeof(YieldInstruction).IsAssignableFrom(methodInfo.ReturnType))
            {
                // This delegate returns a YieldInstruction of some kind
                // (e.g. a Coroutine). Run it, and wait for it to finish
                // before calling onSuccessfulDispatch.
                StartCoroutine(WaitForYieldInstruction(@delegate, finalParameters, onSuccessfulDispatch));
            }
            else if (typeof(void) == methodInfo.ReturnType)
            {
                // This method does not return anything. Invoke it and call
                // our completion handler.
                @delegate.DynamicInvoke(finalParameters);

                onSuccessfulDispatch();
            }
            else
            {
                Debug.LogError($"Cannot run command {firstWord}: the provided delegate does not return a valid type (permitted return types are YieldInstruction or void)");
                return false;
            }

            return true;

            static IEnumerator WaitForYieldInstruction(Delegate @theDelegate, object[] finalParametersToUse, Action onSuccessfulDispatch)
            {
                var yieldInstruction = @theDelegate.DynamicInvoke(finalParametersToUse);
                yield return yieldInstruction;
                onSuccessfulDispatch();
            }

        }

        /// <summary>
        /// Parses the command string inside <paramref name="command"/>,
        /// attempts to locate a suitable method on a suitable game object,
        /// and the invokes the method.
        /// </summary>
        /// <param name="command">The <see cref="Command"/> to run.</param>
        /// <param name="onSuccessfulDispatch">A method to run if a command
        /// was successfully dispatched to a game object. This method is
        /// not called if a registered command handler is not
        /// found.</param>
        /// <returns><see langword="true"/> if the command was successfully
        /// dispatched to a game object; <see langword="false"/> if no game
        /// object was registered as a handler for the command.</returns>
        internal bool DispatchCommandToGameObject(Command command, Action onSuccessfulDispatch)
        {
            // Call out to the string version of this method, because
            // Yarn.Command's constructor is only accessible from inside
            // Yarn Spinner, but we want to be able to unit test. So, we
            // extract it, and call the underlying implementation, which is
            // testable.
            return DispatchCommandToGameObject(command.Text, onSuccessfulDispatch);
        }

        /// <inheritdoc cref="DispatchCommandToGameObject(Command, Action)"/>
        /// <param name="command">The text of the command to
        /// dispatch.</param>
        internal bool DispatchCommandToGameObject(string command, System.Action onSuccessfulDispatch)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentException($"'{nameof(command)}' cannot be null or empty.", nameof(command));
            }

            if (onSuccessfulDispatch is null)
            {
                throw new ArgumentNullException(nameof(onSuccessfulDispatch));
            }

            // Start by splitting our command string by spaces.
            var words = new List<string>(SplitCommandText(command));

            // We need 2 parameters in order to have both a command name,
            // and the name of an object to find.
            if (words.Count < 2)
            {
                // Don't log an error, because the dialogue views might
                // handle this command.
                return false;
            }

            // Get our command name and object name.
            var commandName = words[0];
            var objectName = words[1];

            if (_yarnCommands.ContainsKey(commandName) == false)
            {
                // We didn't find a MethodInfo to invoke for this command,
                // so we can't dispatch it. Don't log an error for it,
                // because this command may be handled by our
                // DialogueViews.
                return false;
            }

            // Attempt to find the object with this name.
            var sceneObject = GameObject.Find(objectName);

            if (sceneObject == null)
            {
                // If we can't find an object, we can't dispatch a command.
                // Log an error here, because this command has been
                // registered with the YarnCommand system, but the object
                // the script calls for doesn't exist.
                Debug.LogError($"Can't run command {commandName} on {objectName}: an object with that name doesn't exist in the scene.");

                return false;
            }

            var methodInfo = _yarnCommands[commandName];

            // If sceneObject has a component whose type matches the
            // methodInfo, we can invoke that method on it.
            var target = sceneObject.GetComponent(methodInfo.DeclaringType) as MonoBehaviour;

            if (target == null)
            {
                Debug.LogError($"Can't run command {commandName} on {objectName}: the command is only defined on {methodInfo.DeclaringType.FullName} components, but {objectName} doesn't have one.");
                return false;
            }

            List<string> parameters = new List<string>(words);

            // Do we have any parameters? Parameters are any words in the
            // command after the first two (which are the command name and
            // the object name); we need to remove these two from the start
            // of the list.
            if (words.Count() >= 2)
            {
                parameters.RemoveRange(0, 2);
            }

            // Convert the parameters from strings to the necessary types
            // that this method expects            
            object[] finalParameters;
            try
            {
                finalParameters = GetPreparedParametersForMethod(parameters.ToArray(), methodInfo);
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"Can't run command {commandName}: {e.Message}");
                return false;
            }

            // We're finally ready to invoke the method on the object!

            // Before we invoke it, we need to know if this is a coroutine.
            // It's a coroutine if the method returns an IEnumerator.

            var isCoroutine = methodInfo.ReturnType == typeof(IEnumerator);

            if (isCoroutine)
            {
                // Start the coroutine. When it's done, it will continue
                // execution.
                StartCoroutine(DoYarnCommand(target, methodInfo, finalParameters, onSuccessfulDispatch));
                return true;
            }
            else
            {
                // Invoke it directly.
                methodInfo.Invoke(target, finalParameters);

                // Continue execution immediately after calling it.
                onSuccessfulDispatch();

                return true;
            }

            IEnumerator DoYarnCommand(MonoBehaviour component,
                                            MethodInfo method,
                                            object[] localParameters,
                                            Action onSuccessfulDispatch)
            {
                // Wait for this command coroutine to complete
                yield return StartCoroutine((IEnumerator)method.Invoke(component, localParameters));

                // And then signal that we're done
                onSuccessfulDispatch();
            }
        }

        /// <summary>
        /// Converts a list of <paramref name="parameters"/> in string form
        /// to an array of objects of the type expected by the method
        /// described by <paramref name="methodInfo"/>.
        /// </summary>
        /// <param name="parameters">The parameters to convert.</param>
        /// <param name="methodInfo">The method used to determine the
        /// appropriate types to convert to.</param>
        /// <returns>An array of objects of the appropriate type.</returns>
        /// <throws cref="ArgumentException">Thrown when the number of
        /// parameters is not appropriate for the method, or if any of the
        /// parameters cannot be converted to the correct type.</throws>
        private object[] GetPreparedParametersForMethod(string[] parameters, MethodInfo methodInfo)
        {
            ParameterInfo[] methodParameters = methodInfo.GetParameters();

            // First test
            var requiredParameters = 0;
            var optionalParameters = 0;

            // How many optional and non-optional parameters does the
            // method have?
            foreach (var parameter in methodParameters)
            {
                if (parameter.IsOptional)
                {
                    optionalParameters += 1;
                }
                else
                {
                    requiredParameters += 1;
                }
            }

            bool anyOptional = optionalParameters > 0;

            // We can't run the command if we didn't supply the right
            // number of parameters.
            if (anyOptional)
            {
                if (parameters.Length < requiredParameters || parameters.Length > (requiredParameters + optionalParameters))
                {
                    throw new ArgumentException($"{methodInfo.Name} requires between {requiredParameters} and {requiredParameters + optionalParameters} parameters, but {parameters.Length} {(parameters.Length == 1 ? "was" : "were")} provided.");
                }
            }
            else
            {
                if (parameters.Length != requiredParameters)
                {
                    throw new ArgumentException($"{methodInfo.Name} requires {requiredParameters} parameters, but {parameters.Length} {(parameters.Length == 1 ? "was" : "were")} provided.");
                }
            }

            // Make a list of objects that we'll supply as parameters to
            // the method when we invoke it.
            var finalParameters = new object[requiredParameters + optionalParameters];

            // Final check: convert each supplied parameter from a string
            // to the expected type.
            for (int i = 0; i < finalParameters.Length; i++)
            {

                if (i >= parameters.Length)
                {
                    // We didn't supply a parameter here, so supply
                    // Type.Missing to make it use the default value
                    // instead.
                    finalParameters[i] = System.Type.Missing;
                    continue;
                }

                var parameterName = methodParameters[i].Name;
                var expectedType = methodParameters[i].ParameterType;

                // We handle three special cases:
                // - if the method expects a GameObject, attempt to locate
                //   that game object by the provided name. The game object
                //   must be active. If this lookup fails, provide null.
                // - if the method expects a Component, or a
                //   Component-derived type, locate that object and supply
                //   it. The object, or the object the desired component is
                //   on, must be active. If this fails, supply null.
                // - if the method expects a Boolean, and the parameter is
                //   a string that case-insensitively matches the name of
                //   the parameter, act as though the parameter had been
                //   "true". This allows us to write a command like
                //   "Move(bool wait)", and invoke it as "<<move wait>>",
                //   instead of having to say "<<move true>>". If it's
                //   false, provide false; if it's any other string, throw
                //   an error.
                if (typeof(GameObject).IsAssignableFrom(expectedType))
                {
                    finalParameters[i] = GameObject.Find(parameters[i]);
                }
                else if (typeof(Component).IsAssignableFrom(expectedType))
                {
                    // Find the game object with the component we're
                    // looking for
                    var go = GameObject.Find(parameters[i]);
                    if (go != null)
                    {
                        // Find the component on this game object (or null)
                        var c = go.GetComponentInChildren(expectedType);
                        finalParameters[i] = c;
                    }
                }
                else if (typeof(bool).IsAssignableFrom(expectedType))
                {
                    // If it's a bool, and the parameter was the name of
                    // the parameter, act as though they had written
                    // 'true'.
                    if (parameters[i].Equals(parameterName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        finalParameters[i] = true;
                    }
                    else
                    {
                        // It wasn't the name of the parameter, so attempt
                        // to parse it as a boolean (i.e. the strings
                        // "true" or "false"), and throw an exception if
                        // that failed
                        try
                        {
                            finalParameters[i] = Convert.ToBoolean(parameters[i]);
                        }
                        catch (Exception e)
                        {
                            throw new ArgumentException($"can't convert parameter {i + 1} (\"{parameters[i]}\") to parameter {parameterName} ({expectedType}): {e}");
                        }
                    }
                }
                else
                {
                    // Attempt to perform a straight conversion, using the
                    // invariant culture. The parameter type must implement
                    // IConvertible.
                    try
                    {
                        finalParameters[i] = Convert.ChangeType(parameters[i], expectedType, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException($"can't convert parameter {i + 1} (\"{parameters[i]}\") to parameter {parameterName} ({expectedType}): {e}");
                    }

                }
            }
            return finalParameters;
        }

        private void PrepareForLines(IEnumerable<string> lineIDs)
        {
            lineProvider.PrepareForLines(lineIDs);
        }

        /// <summary>
        /// Called when a <see cref="DialogueViewBase"/> has finished
        /// delivering its line. When all views in <see
        /// cref="ActiveDialogueViews"/> have called this method, the
        /// line's status will change to <see
        /// cref="LineStatus.FinishedPresenting"/>.
        /// </summary>
        /// <param name="dialogueView">The view that finished delivering
        /// the line.</param>
        private void DialogueViewCompletedDelivery(DialogueViewBase dialogueView)
        {
            // A dialogue view just completed its delivery. Remove it from
            // the set of active views.
            ActiveDialogueViews.Remove(dialogueView);

            // Have all of the views completed? 
            if (ActiveDialogueViews.Count == 0)
            {
                UpdateLineStatus(CurrentLine, LineStatus.FinishedPresenting);

                // Should the line automatically become Ended as soon as
                // it's Delivered?
                if (automaticallyContinueLines)
                {
                    // Go ahead and notify the views. 
                    UpdateLineStatus(CurrentLine, LineStatus.Dismissed);

                    // Additionally, tell the views to dismiss the line
                    // from presentation. When each is done, it will notify
                    // this dialogue runner to call
                    // DialogueViewCompletedDismissal; when all have
                    // finished, this dialogue runner will tell the
                    // Dialogue to Continue() when all lines are done
                    // dismissing the line.
                    DismissLineFromViews(dialogueViews);
                }
            }
        }

        /// <summary>
        /// Updates a <see cref="LocalizedLine"/>'s status to <paramref
        /// name="newStatus"/>, and notifies all dialogue views that the
        /// line about the state change.
        /// </summary>
        /// <param name="line">The line whose state is changing.</param>
        /// <param name="newStatus">The <see cref="LineStatus"/> that the
        /// line now has.</param>
        private void UpdateLineStatus(LocalizedLine line, LineStatus newStatus)
        {
            // Update the state of the line and let the views know.
            line.Status = newStatus;

            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null || dialogueView.enabled == false) continue;

                dialogueView.OnLineStatusChanged(line);
            }
        }

        void ContinueDialogue()
        {
            CurrentLine = null;
            Dialogue.Continue();
        }

        /// <summary>
        /// Called by a <see cref="DialogueViewBase"/> derived class from
        /// <see cref="dialogueViews"/> to inform the <see
        /// cref="DialogueRunner"/> that the user intents to proceed to the
        /// next line.
        /// </summary>
        public void OnViewUserIntentNextLine()
        {

            if (CurrentLine == null)
            {
                // There's no active line, so there's nothing that can be
                // done here.
                Debug.LogWarning($"{nameof(OnViewUserIntentNextLine)} was called, but no line was running.");
                return;
            }

            switch (CurrentLine.Status)
            {
                case LineStatus.Presenting:
                    // The line has been Interrupted. Dialogue views should
                    // proceed to finish the delivery of the line as
                    // quickly as they can. (When all views have finished
                    // their expedited delivery, they call their completion
                    // handler as normal, and the line becomes Delivered.)
                    UpdateLineStatus(CurrentLine, LineStatus.Interrupted);
                    break;
                case LineStatus.Interrupted:
                    // The line was already interrupted, and the user has
                    // requested the next line again. We interpret this as
                    // the user being insistent. This means the line is now
                    // Ended, and the dialogue views must dismiss the line
                    // immediately.
                    UpdateLineStatus(CurrentLine, LineStatus.Dismissed);
                    break;
                case LineStatus.FinishedPresenting:
                    // The line had finished delivery (either normally or
                    // because it was Interrupted), and the user has
                    // indicated they want to proceed to the next line. The
                    // line is therefore Ended.
                    UpdateLineStatus(CurrentLine, LineStatus.Dismissed);
                    break;
                case LineStatus.Dismissed:
                    // The line has already been ended, so there's nothing
                    // further for the views to do. (This will only happen
                    // during the interval of time between a line becoming
                    // Ended and the next line appearing.)
                    break;
            }

            if (CurrentLine.Status == LineStatus.Dismissed)
            {
                // This line is Ended, so we need to tell the dialogue
                // views to dismiss it. 
                DismissLineFromViews(dialogueViews);
            }

        }

        private void DismissLineFromViews(IEnumerable<DialogueViewBase> dialogueViews)
        {
            ActiveDialogueViews.Clear();

            foreach (var dialogueView in dialogueViews)
            {
                // Skip any dialogueView that is null or not enabled
                if (dialogueView == null || dialogueView.enabled == false)
                {
                    continue;
                }

                // we do this in two passes - first by adding each
                // dialogueView into ActiveDialogueViews, then by asking
                // them to dismiss the line - because calling
                // view.DismissLine might immediately call its completion
                // handler (which means that we'd be repeatedly returning
                // to zero active dialogue views, which means
                // DialogueViewCompletedDismissal will mark the line as
                // entirely done)
                ActiveDialogueViews.Add(dialogueView);
            }

            foreach (var dialogueView in dialogueViews)
            {
                if (dialogueView == null || dialogueView.enabled == false) continue;

                dialogueView.DismissLine(() => DialogueViewCompletedDismissal(dialogueView));
            }
        }

        private void DialogueViewCompletedDismissal(DialogueViewBase dialogueView)
        {
            // A dialogue view just completed dismissing its line. Remove
            // it from the set of active views.
            ActiveDialogueViews.Remove(dialogueView);

            // Have all of the views completed dismissal? 
            if (ActiveDialogueViews.Count == 0)
            {
                // Then we're ready to continue to the next piece of
                // content.
                ContinueDialogue();
            }
        }
        #endregion

        /// <summary>
        /// Splits input into a number of non-empty sub-strings, separated
        /// by whitespace, and grouping double-quoted strings into a single
        /// sub-string.
        /// </summary>
        /// <param name="input">The string to split.</param>
        /// <returns>A collection of sub-strings.</returns>
        /// <remarks>
        /// This method behaves similarly to the <see
        /// cref="string.Split(char[], StringSplitOptions)"/> method with
        /// the <see cref="StringSplitOptions"/> parameter set to <see
        /// cref="StringSplitOptions.RemoveEmptyEntries"/>, with the
        /// following differences:
        ///
        /// <list type="bullet">
        /// <item>Text that appears inside a pair of double-quote
        /// characters will not be split.</item>
        ///
        /// <item>Text that appears after a double-quote character and
        /// before the end of the input will not be split (that is, an
        /// unterminated double-quoted string will be treated as though it
        /// had been terminated at the end of the input.)</item>
        ///
        /// <item>When inside a pair of double-quote characters, the string
        /// <c>\\</c> will be converted to <c>\</c>, and the string
        /// <c>\"</c> will be converted to <c>"</c>.</item>
        /// </list>
        /// </remarks>
        public static IEnumerable<string> SplitCommandText(string input)
        {
            var reader = new System.IO.StringReader(input.Normalize());

            int c;

            var results = new List<string>();
            var currentComponent = new System.Text.StringBuilder();

            while ((c = reader.Read()) != -1)
            {
                if (char.IsWhiteSpace((char)c))
                {
                    if (currentComponent.Length > 0)
                    {
                        // We've reached the end of a run of visible
                        // characters. Add this run to the result list and
                        // prepare for the next one.
                        results.Add(currentComponent.ToString());
                        currentComponent.Clear();
                    }
                    else
                    {
                        // We encountered a whitespace character, but
                        // didn't have any characters queued up. Skip this
                        // character.
                    }

                    continue;
                }
                else if (c == '\"')
                {
                    // We've entered a quoted string!
                    while (true)
                    {
                        c = reader.Read();
                        if (c == -1)
                        {
                            // Oops, we ended the input while parsing a
                            // quoted string! Dump our current word
                            // immediately and return.
                            results.Add(currentComponent.ToString());
                            return results;
                        }
                        else if (c == '\\')
                        {
                            // Possibly an escaped character!
                            var next = reader.Peek();
                            if (next == '\\' || next == '\"')
                            {
                                // It is! Skip the \ and use the character after it.
                                reader.Read();
                                currentComponent.Append((char)next);
                            }
                            else
                            {
                                // Oops, an invalid escape. Add the \ and
                                // whatever is after it.
                                currentComponent.Append((char)c);
                            }
                        }
                        else if (c == '\"')
                        {
                            // The end of a string!
                            break;
                        }
                        else
                        {
                            // Any other character. Add it to the buffer.
                            currentComponent.Append((char)c);
                        }
                    }

                    results.Add(currentComponent.ToString());
                    currentComponent.Clear();
                }
                else
                {
                    currentComponent.Append((char)c);
                }
            }

            if (currentComponent.Length > 0)
            {
                results.Add(currentComponent.ToString());
            }

            return results;
        }

        
    }
}
