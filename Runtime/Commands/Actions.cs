using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    using Converter = System.Func<string, object>;

    public class Actions : ICommandDispatcher
    {
        private class CommandRegistration
        {
            public CommandRegistration(string name, Delegate @delegate)
            {
                Name = name;
                Delegate = @delegate;

                if (@delegate.Method.IsStatic == false
                    && typeof(Component).IsAssignableFrom(@delegate.Method.DeclaringType) == false)
                {
                    // This is an instance method, but the delegate's delcaring
                    // type is not a Component, which means we won't be able to
                    // look up a target.
                    throw new ArgumentException($"Cannot register command {@delegate.Method.Name} as a command: instance methods must declared on {nameof(Component)} classes.");
                }

                Converters = CreateConverters(@delegate.Method);
            }

            public string Name { get; set; }
            public Delegate Delegate { get; set; }
            public Type DeclaringType => Delegate.Method.DeclaringType;
            public Type ReturnType => Delegate.Method.ReturnType;
            public bool IsStatic => Delegate.Method.IsStatic;

            public readonly Converter[] Converters;

            public CommandType Type
            {
                get
                {
                    Type returnType = ReturnType;

                    if (typeof(void).IsAssignableFrom(returnType))
                    {
                        return CommandType.IsVoid;
                    }
                    if (typeof(IEnumerator).IsAssignableFrom(returnType))
                    {
                        return CommandType.IsCoroutine;
                    }
                    if (typeof(Coroutine).IsAssignableFrom(returnType))
                    {
                        return CommandType.ReturnsCoroutine;
                    }
                    return CommandType.Invalid;
                }
            }

            public enum CommandType
            {
                IsVoid,
                ReturnsCoroutine,
                IsCoroutine,
                Invalid,
            }

            /// <summary>
            /// Attempt to parse the arguments with cached converters.
            /// </summary>
            /// <param name="method">The method to parse args for.</param>
            /// <param name="converters">Converters to use. Will be assumed that
            /// the converters correctly correspond to <paramref name="method"/>.
            /// </param>
            /// <param name="args">The raw list of arguments, including command and
            /// instance name.</param>
            /// <param name="isStatic">Should we treat this function as static?
            /// </param>
            /// <returns>The parsed arguments.</returns>
            public object[] ParseArgs(string[] args)
            {
                var parameters = Delegate.Method.GetParameters();
                int optional = 0;
                foreach (var parameter in parameters)
                {
                    if (parameter.IsOptional)
                    {
                        optional += 1;
                    }
                }

                int required = parameters.Length - optional;
                var count = args.Length;

                if (optional > 0)
                {
                    if (count < required || count > parameters.Length)
                    {
                        throw new ArgumentException(
                            $"{this.Name} requires between {required} and {parameters.Length} parameters, but {count} " +
                            $"{(count == 1 ? "was" : "were")} provided.");
                    }
                }
                else if (count != required)
                {
                    var requiredParameterTypeNames = new List<string>();

                    foreach (var p in parameters)
                    {
                        if (!p.IsOptional)
                        {
                            requiredParameterTypeNames.Add(p.ParameterType.ToString());
                        }
                    }

                    throw new ArgumentException($"{this.Name} requires {required} parameters ({string.Join(", ", requiredParameterTypeNames)}), but {count} " +
                        $"{(count == 1 ? "was" : "were")} provided.");
                }

                var finalArgs = new object[parameters.Length];

                for (int i = 0; i < count; i++)
                {
                    string arg = args[i];
                    finalArgs[i] = Converters[i] == null ? arg : Converters[i].Invoke(arg);
                }
                for (int i = count; i < finalArgs.Length; i++)
                {
                    finalArgs[i] = System.Type.Missing;
                }
                return finalArgs;
            }
        }

        private Dictionary<string, CommandRegistration> Commands = new Dictionary<string, CommandRegistration>();

        public Library Library { get; }
        public DialogueRunner DialogueRunner { get; }

        public Actions(DialogueRunner dialogueRunner, Library library)
        {
            Library = library;
            DialogueRunner = dialogueRunner;
        }

        public void AddCommandHandler(string commandName, Delegate handler)
        {
            bool added = Commands.TryAdd(commandName, new CommandRegistration(commandName, handler));

            if (!added)
            {
                Debug.LogError($"Failed to register command {commandName}: a command by this name has already been registered.");
            }
        }

        public void AddFunction(string name, Delegate implementation)
        {
            if (Library.FunctionExists(name))
            {
                Debug.LogError($"Cannot add function {name}: one already exists");
                return;
            }
            Library.RegisterFunction(name, implementation);
        }

        public void AddCommandHandler(string commandName, Func<Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1>(string commandName, Func<T1, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1, T2>(string commandName, Func<T1, T2, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Func<T1, T2, T3, T4, T5, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Func<T1, T2, T3, T4, T5, T6, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler(string commandName, Action handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1>(string commandName, Action<T1> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1, T2>(string commandName, Action<T1, T2> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1, T2, T3>(string commandName, Action<T1, T2, T3> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Action<T1, T2, T3, T4> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Action<T1, T2, T3, T4, T5> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Action<T1, T2, T3, T4, T5, T6> handler) => AddCommandHandler(commandName, (Delegate)handler);

        public void AddFunction<TResult>(string name, Func<TResult> implementation) => AddFunction(name, (Delegate)implementation);

        public void AddFunction<TResult, T1>(string name, Func<TResult, T1> implementation) => AddFunction(name, (Delegate)implementation);

        public void AddFunction<TResult, T1, T2>(string name, Func<TResult, T1, T2> implementation) => AddFunction(name, (Delegate)implementation);

        public void AddFunction<TResult, T1, T2, T3>(string name, Func<TResult, T1, T2, T3> implementation) => AddFunction(name, (Delegate)implementation);

        public void AddFunction<TResult, T1, T2, T3, T4>(string name, Func<TResult, T1, T2, T3, T4> implementation) => AddFunction(name, (Delegate)implementation);

        public void AddFunction<TResult, T1, T2, T3, T4, T5>(string name, Func<TResult, T1, T2, T3, T4, T5> implementation) => AddFunction(name, (Delegate)implementation);

        public void AddFunction<TResult, T1, T2, T3, T4, T5, T6>(string name, Func<TResult, T1, T2, T3, T4, T5, T6> implementation) => AddFunction(name, (Delegate)implementation);

        public void RemoveCommandHandler(string commandName)
        {
            throw new NotImplementedException();
        }

        public void RemoveFunction(string name)
        {
            if (Library.FunctionExists(name) == false)
            {
                Debug.LogError($"Cannot remove function {name}: no function with that name exists in the library");
                return;
            }
            Library.DeregisterFunction(name);
        }

        public void SetupForProject(YarnProject yarnProject)
        {
            // no-op
        }

        DialogueRunner.CommandDispatchResult ICommandDispatcher.DispatchCommand(string command, out Coroutine? commandCoroutine)
        {
            commandCoroutine = null;

            var commandPieces = new List<string>(DialogueRunner.SplitCommandText(command));

            if (commandPieces.Count == 0)
            {
                return DialogueRunner.CommandDispatchResult.NotFound;
            }

            var commandName = commandPieces[0];


            var found = Commands.TryGetValue(commandPieces[0], out var registration);
            if (!found)
            {
                return DialogueRunner.CommandDispatchResult.NotFound;
            }

            commandPieces.RemoveAt(0);
            var parameters = commandPieces;

            object? target = null;

            if (registration.IsStatic == false)
            {
                // This is an instance method.

                if (parameters.Count == 0)
                {
                    Debug.LogError($"Can't call command {commandName}: not enough parameters");
                    return DialogueRunner.CommandDispatchResult.Failed;
                }

                // First parameter is the name of a game object that has the
                // component we're trying to call.

                var gameObjectName = parameters[0];

                parameters.RemoveAt(0);

                var gameObject = GameObject.Find(gameObjectName);

                if (gameObject == null)
                {
                    Debug.LogError($"Can't call command {commandName}: failed to find a game object named {gameObjectName}");
                    return DialogueRunner.CommandDispatchResult.Failed;
                }

                var targetComponent = gameObject.GetComponent(registration.DeclaringType);

                if (targetComponent == null)
                {
                    Debug.LogError($"Can't call command {commandName}, because it doesn't have a {registration.DeclaringType.Name} component");
                    return DialogueRunner.CommandDispatchResult.Failed;
                }

                target = targetComponent;
            }

            object[] finalParameters = registration.ParseArgs(parameters.ToArray());

            var returnValue = registration.Delegate.Method.Invoke(target, finalParameters);

            if (returnValue is Coroutine coro)
            {
                commandCoroutine = coro;
            }
            else if (returnValue is IEnumerator enumerator)
            {
                commandCoroutine = DialogueRunner.StartCoroutine(enumerator);
            }
            else
            {
                commandCoroutine = null;
            }
            return DialogueRunner.CommandDispatchResult.Success;

        }

        private static Converter[] CreateConverters(MethodInfo method)
        {
            ParameterInfo[] parameterInfos = method.GetParameters();

            Converter[] result = (Func<string, object>[])Array.CreateInstance(typeof(Func<string, object>), parameterInfos.Length);

            int i = 0;

            foreach (var parameterInfo in parameterInfos)
            {
                result[i] = CreateConverter(parameterInfo, i);
                i++;
            }
            return result;
        }

        private static Converter CreateConverter(ParameterInfo parameter, int index)
        {
            var targetType = parameter.ParameterType;

            // well, I mean...
            if (targetType == typeof(string)) { return arg => arg; }

            // find the GameObject.
            if (typeof(GameObject).IsAssignableFrom(targetType))
            {
                return GameObject.Find;
            }

            // find components of the GameObject with the component, if available
            if (typeof(Component).IsAssignableFrom(targetType))
            {
                return arg =>
                {
                    GameObject gameObject = GameObject.Find(arg);
                    if (gameObject == null)
                    {
                        return null!;
                    }
                    return gameObject.GetComponentInChildren(targetType);
                };
            }

            // bools can take "true" or "false", or the parameter name.
            if (typeof(bool).IsAssignableFrom(targetType))
            {
                return arg =>
                {
                    if (arg.Equals(parameter.Name, StringComparison.InvariantCultureIgnoreCase)) { return true; }
                    if (bool.TryParse(arg, out bool res)) { return res; }
                    throw new ArgumentException(
                        $"Can't convert the given parameter at position {index + 1} (\"{arg}\") to parameter " +
                        $"{parameter.Name} of type {typeof(bool).FullName}.");
                };
            }

            // try converting using IConvertible.
            return arg =>
            {
                try
                {
                    return Convert.ChangeType(arg, targetType, CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(
                        $"Can't convert the given parameter at position {index + 1} (\"{arg}\") to parameter " +
                        $"{parameter.Name} of type {targetType.FullName}: {e}");
                }
            };
        }
    }
}
