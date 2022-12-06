using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if YARN_LEGACY_ACTIONMANAGER
namespace Yarn.Unity {

    internal class LegacyActionManagerDispatcher : ICommandDispatcher
    {
        public LegacyActionManagerDispatcher(DialogueRunner dialogueRunner) {
            DialogueRunner = dialogueRunner;
        }

        public void SetupForProject(YarnProject yarnProject)
        {
            // Load all of the commands and functions from the assemblies that
            // this project wants to load from.
            ActionManager.AddActionsFromAssemblies(yarnProject.searchAssembliesForActions);

            // Register any new functions that we found as part of doing this.
            ActionManager.RegisterFunctions(DialogueRunner.Dialogue.Library);
        }

        public DialogueRunner.CommandDispatchResult DispatchCommand(string command, out Coroutine commandCoroutine)
        {
            
            // Try looking in the command handlers first
            var dispatchResult = DispatchCommandToRegisteredHandlers(command, out commandCoroutine);

            if (dispatchResult != DialogueRunner.CommandDispatchResult.NotFound)
            {
                // We found the command! We don't need to keep looking. (It may
                // have succeeded or failed; if it failed, it logged something
                // to the console or otherwise communicated to the developer
                // that something went wrong. Either way, we don't need to do
                // anything more here.)
                return dispatchResult;
            }

            // We didn't find it in the comand handlers. Try looking in the
            // game objects. 
            dispatchResult = DispatchCommandToGameObject(command, out commandCoroutine);
            return dispatchResult;
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
        DialogueRunner.CommandDispatchResult DispatchCommandToRegisteredHandlers(Command command, out Coroutine commandCoroutine)
        {
            return DispatchCommandToRegisteredHandlers(command.Text, out commandCoroutine);
        }

        /// <inheritdoc cref="DispatchCommandToRegisteredHandlers(Command,
        /// Action)"/>
        /// <param name="command">The text of the command to
        /// dispatch.</param>
        internal DialogueRunner.CommandDispatchResult DispatchCommandToRegisteredHandlers(string command, out Coroutine commandCoroutine)
        {
            var commandTokens = DialogueRunner.SplitCommandText(command).ToArray();

            if (commandTokens.Length == 0)
            {
                // Nothing to do.
                commandCoroutine = null;
                return DialogueRunner.CommandDispatchResult.NotFound;
            }

            var firstWord = commandTokens[0];

            if (commandHandlers.ContainsKey(firstWord) == false)
            {
                // We don't have a registered handler for this command, but
                // some other part of the game might.
                commandCoroutine = null;
                return DialogueRunner.CommandDispatchResult.NotFound;
            }

            var @delegate = commandHandlers[firstWord];
            var methodInfo = @delegate.Method;

            object[] finalParameters;

            try
            {
                finalParameters = ActionManager.ParseArgs(methodInfo, commandTokens);
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"Can't run command {firstWord}: {e.Message}");
                commandCoroutine = null;
                return DialogueRunner.CommandDispatchResult.Failed;
            }

            if (typeof(Coroutine).IsAssignableFrom(methodInfo.ReturnType))
            {
                // This delegate returns a YieldInstruction of some kind
                // (e.g. a Coroutine). Run it, and wait for it to finish
                // before calling onSuccessfulDispatch.

                commandCoroutine = (Coroutine)@delegate.DynamicInvoke(finalParameters);
                return DialogueRunner.CommandDispatchResult.Success;
            }
            else if (typeof(void) == methodInfo.ReturnType)
            {
                // This method does not return anything. Invoke it and call
                // our completion handler.
                @delegate.DynamicInvoke(finalParameters);
                commandCoroutine = null;
                return DialogueRunner.CommandDispatchResult.Success;
            }
            else
            {
                Debug.LogError($"Cannot run command {firstWord}: the provided delegate does not return a valid type (permitted return types are YieldInstruction or void)");
                commandCoroutine = null;
                return DialogueRunner.CommandDispatchResult.Failed;
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
        internal DialogueRunner.CommandDispatchResult DispatchCommandToGameObject(Command command, out Coroutine commandCoroutine)
        {
            // Call out to the string version of this method, because
            // Yarn.Command's constructor is only accessible from inside
            // Yarn Spinner, but we want to be able to unit test. So, we
            // extract it, and call the underlying implementation, which is
            // testable.
            return DispatchCommandToGameObject(command.Text, out commandCoroutine);
        }

        /// <inheritdoc cref="DispatchCommandToGameObject(Command, Action)"/>
        /// <param name="command">The text of the command to
        /// dispatch.</param>
        internal DialogueRunner.CommandDispatchResult DispatchCommandToGameObject(string command, out Coroutine commandCoroutine)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentException($"'{nameof(command)}' cannot be null or empty.", nameof(command));
            }

            DialogueRunner.CommandDispatchResult commandExecutionResult;
            
            commandExecutionResult = ActionManager.TryExecuteCommand(DialogueRunner.SplitCommandText(command).ToArray(), out object returnValue);
            
            if (commandExecutionResult != DialogueRunner.CommandDispatchResult.Success)
            {
                commandCoroutine = null;
                return commandExecutionResult;
            }

            var enumerator = returnValue as IEnumerator;

            if (enumerator != null)
            {
                // Start the coroutine. When it's done, it will continue execution.
                commandCoroutine = DialogueRunner.StartCoroutine(enumerator);
            }
            else
            {
                // no coroutine, so we're done!
                commandCoroutine = null;
            }
            return DialogueRunner.CommandDispatchResult.Success;
        }

        public DialogueRunner DialogueRunner { get; }

        /// Maps the names of commands to action delegates.
        Dictionary<string, Delegate> commandHandlers = new Dictionary<string, Delegate>();

        #region CommandsAndFunctions
        /// <summary>
        /// Adds a command handler. Dialogue will pause execution after the
        /// command is called.
        /// </summary>
        /// <remarks>
        /// <para>When this command handler has been added, it can be called
        /// from your Yarn scripts like so:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;commandName param1 param2&gt;&gt;
        /// </code>
        ///
        /// <para>If <paramref name="handler"/> is a method that returns a <see
        /// cref="Coroutine"/>, when the command is run, the <see
        /// cref="DialogueRunner"/> will wait for the returned coroutine to stop
        /// before delivering any more content.</para>
        /// </remarks>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="handler">The <see cref="CommandHandler"/> that will be
        /// invoked when the command is called.</param>
        public void AddCommandHandler(string commandName, Delegate handler)
        {
            if (commandHandlers.ContainsKey(commandName))
            {
                Debug.LogError($"Cannot add a command handler for {commandName}: one already exists");
                return;
            }
            commandHandlers.Add(commandName, handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler(string commandName, System.Func<Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1>(string commandName, System.Func<T1, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2>(string commandName, System.Func<T1, T2, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3>(string commandName, System.Func<T1, T2, T3, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Func<T1, T2, T3, T4, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Func<T1, T2, T3, T4, T5, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Func<T1, T2, T3, T4, T5, T6, Coroutine> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler(string commandName, System.Action handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1>(string commandName, System.Action<T1> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2>(string commandName, System.Action<T1, T2> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3>(string commandName, System.Action<T1, T2, T3> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, System.Action<T1, T2, T3, T4> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, System.Action<T1, T2, T3, T4, T5> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
        }

        /// <inheritdoc cref="AddCommandHandler(string, Delegate)"/>
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, System.Action<T1, T2, T3, T4, T5, T6> handler)
        {
            AddCommandHandler(commandName, (Delegate)handler);
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
        /// <para>When this function has been registered, it can be called from
        /// your Yarn scripts like so:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;if myFunction(1, 2) == true&gt;&gt;
        ///     myFunction returned true!
        /// &lt;&lt;endif&gt;&gt;
        /// </code>
        ///
        /// <para>The <c>call</c> command can also be used to invoke the function:</para>
        ///
        /// <code lang="yarn">
        /// &lt;&lt;call myFunction(1, 2)&gt;&gt;
        /// </code>
        /// </remarks>
        /// <param name="implementation">The <see cref="Delegate"/> that
        /// should be invoked when this function is called.</param>
        /// <seealso cref="Library"/>
        public void AddFunction(string name, Delegate implementation)
        {
            if (DialogueRunner.Dialogue.Library.FunctionExists(name))
            {
                Debug.LogError($"Cannot add function {name}: one already exists");
                return;
            }

            DialogueRunner.Dialogue.Library.RegisterFunction(name, implementation);
        }

        /// <inheritdoc cref="AddFunction(string, Delegate)" />
        /// <typeparam name="TResult">The type of the value that the function should return.</typeparam>
        public void AddFunction<TResult>(string name, System.Func<TResult> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult}(string, Func{TResult})" />
        /// <typeparam name="T1">The type of the first parameter to the function.</typeparam>
        public void AddFunction<TResult, T1>(string name, System.Func<TResult, T1> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult,T1}(string, Func{TResult,T1})" />
        /// <typeparam name="T2">The type of the second parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2>(string name, System.Func<TResult, T1, T2> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult,T1,T2}(string, Func{TResult,T1,T2})" />
        /// <typeparam name="T3">The type of the third parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3>(string name, System.Func<TResult, T1, T2, T3> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3}(string, Func{TResult,T1,T2,T3})" />
        /// <typeparam name="T4">The type of the fourth parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3, T4>(string name, System.Func<TResult, T1, T2, T3, T4> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3,T4}(string, Func{TResult,T1,T2,T3,T4})" />
        /// <typeparam name="T5">The type of the fifth parameter to the function.</typeparam>
        public void AddFunction<TResult, T1, T2, T3, T4, T5>(string name, System.Func<TResult, T1, T2, T3, T4, T5> implementation)
        {
            AddFunction(name, (Delegate)implementation);
        }

        /// <inheritdoc cref="AddFunction{TResult,T1,T2,T3,T4,T5}(string, Func{TResult,T1,T2,T3,T4,T5})" />
        /// <typeparam name="T6">The type of the sixth parameter to the function.</typeparam>
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
        public void RemoveFunction(string name) => DialogueRunner.Dialogue.Library.DeregisterFunction(name);

        

        #endregion
    }
}
#endif
