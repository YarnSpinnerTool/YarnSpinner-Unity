using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;


namespace Yarn.Unity
{
    using Converter = System.Func<string, object>;

    public interface ICommand {
        string Name { get; }
    }

    internal static class DiagnosticUtility {
        public static string EnglishPluraliseNounCount(int count, string name, bool prefixCount = false) {
            string result;
            if (count == 1) {
                result = name;
            } else {
                result = name + "s";
            }
            if (prefixCount) {
                return count.ToString() + " " + result;
            } else {
                return result;
            }
        }

        public static string EnglishPluraliseWasVerb(int count) {
            if (count == 1) {
                return "was";
            } else {
                return "were";
            }
        }
    }

    

    public class Actions : ICommandDispatcher
    {
        internal class CommandRegistration : ICommand
        {
            public CommandRegistration(string name, Delegate @delegate) {
                Name = name;
                Method = @delegate.Method;
                Target = @delegate.Target;
                Converters = CreateConverters(Method);
                DynamicallyFindsTarget = false;
            }

            public CommandRegistration(string name, MethodInfo method)
            {
                if (method.IsStatic)
                {
                    DynamicallyFindsTarget = false;
                }
                else if (typeof(Component).IsAssignableFrom(method.DeclaringType))
                {
                    // This method is an instance method on a Component (or one
                    // of its subclasses). We'll dynamically find a target to
                    // invoke the method on at runtime.
                    DynamicallyFindsTarget = true;
                }
                else
                {
                    // The instance method's declaring type is not a Component,
                    // which means we won't be able to look up a target.
                    throw new ArgumentException($"Cannot register method {GetFullMethodName(method)} as a command: instance methods must declared on {nameof(Component)} classes.");
                }

                Name = name;
                Method = method;
                Target = null;

                Converters = CreateConverters(method);
            }

            public string Name { get; set; }
            public MethodInfo Method { get; set; }
            private object Target { get; set; }

            public Type DeclaringType => Method.DeclaringType;
            public Type ReturnType => Method.ReturnType;
            public bool IsStatic => Method.IsStatic;

            public readonly Converter[] Converters;

            /// <summary>
            /// Gets a value indicating that this command finds a target to
            /// invoke its method on by name, each time it is invoked.
            /// </summary>
            private bool DynamicallyFindsTarget { get; }

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
                /// <summary>
                /// The method returns <see cref="void"/>.
                /// </summary>
                IsVoid,
                /// <summary>
                /// The method returns a <see cref="Coroutine"/> object.
                /// </summary>
                /// <remarks>
                ReturnsCoroutine,
                /// <summary>
                /// The method returns <see cref="IEnumerator"/> (that is, it is
                /// a coroutine).
                /// </summary>
                /// <remarks>
                /// Code that invokes this command should use <see
                /// cref="MonoBehaviour.StartCoroutine(IEnumerator)"/> to begin
                /// the coroutine.
                /// </remarks>
                IsCoroutine,
                /// <summary>
                /// The method is not a valid command (that is, it does not
                /// return <see cref="void"/>, <see cref="Coroutine"/>, or <see
                /// cref="IEnumerator"/>.)
                /// </summary>
                Invalid,
            }

            /// <summary>
            /// Attempt to parse the arguments with cached converters.
            /// </summary>
            public bool TryParseArgs(string[] args, out object[] result, out string message)
            {
                var parameters = Method.GetParameters();

                var (min, max) = ParameterCount;

                int argumentCount = args.Length;
                if (argumentCount < min || argumentCount > max) {
                    // Wrong number of arguments.
                    string requirementDescription;
                    if (min == 0) {
                        requirementDescription = $"at most {max} {DiagnosticUtility.EnglishPluraliseNounCount(max, "parameter")}";
                    } else if (min != max) {
                        requirementDescription = $"between {min} and {max} {DiagnosticUtility.EnglishPluraliseNounCount(max, "parameter")}";
                    } else {
                        requirementDescription = $"{min} {DiagnosticUtility.EnglishPluraliseNounCount(max, "parameter")}";
                    }
                    message = $"{this.Name} requires {requirementDescription}, but {argumentCount} {DiagnosticUtility.EnglishPluraliseWasVerb(argumentCount)} provided.";
                    result = default;
                    return false;
                }

                var finalArgs = new object[parameters.Length];

                for (int i = 0; i < argumentCount; i++)
                {
                    string arg = args[i];
                    if (Converters[i] == null) {
                        finalArgs[i] = arg;
                    } else {
                        try {
                            finalArgs[i] = Converters[i].Invoke(arg);
                        } catch (Exception e) {
                            message = $"Can't convert parameter {i} to {parameters[i].ParameterType.Name}: {e.Message}";
                            result = default;
                            return false;
                        }
                    }
                }
                for (int i = argumentCount; i < finalArgs.Length; i++)
                {
                    finalArgs[i] = System.Type.Missing;
                }
                result = finalArgs;
                message = default;
                return true;
            }

            private (int Min, int Max) ParameterCount {
                get {
                    var parameters = Method.GetParameters();
                    int optional = 0;
                    foreach (var parameter in parameters)
                    {
                        if (parameter.IsOptional)
                        {
                            optional += 1;
                        }
                    }

                    int min = parameters.Length - optional;
                    int max = parameters.Length;
                    return (min, max);
                }
            }

            internal CommandDispatchResult Invoke(DialogueRunner dispatcher, List<string> parameters, out Coroutine commandCoroutine)
            {
                object target;

                if (DynamicallyFindsTarget)
                {
                    // We need to find a target to call this method on.

                    if (parameters.Count == 0)
                    {
                        // We need at least one parameter, which is the
                        // component to look for
                        commandCoroutine = default;
                        return new CommandDispatchResult
                        {
                            Message = $"{this.Name} needs a target, but none was specified",
                            Status = CommandDispatchResult.StatusType.InvalidParameterCount
                        };
                    }

                    // First parameter is the name of a game object that has the
                    // component we're trying to call.

                    var gameObjectName = parameters[0];

                    parameters.RemoveAt(0);

                    var gameObject = GameObject.Find(gameObjectName);

                    if (gameObject == null)
                    {
                        // We couldn't find a target with this name.
                        commandCoroutine = default;
                        return new CommandDispatchResult
                        {
                            Message = $"No game object named \"{gameObjectName}\" exists",
                            Status = CommandDispatchResult.StatusType.TargetMissingComponent
                        };
                    }

                    // We've found a target.  Does it have a component that's
                    // the right type of object to call the method on?
                    var targetComponent = gameObject.GetComponent(this.DeclaringType);

                    if (targetComponent == null)
                    {
                        commandCoroutine = default;
                        return new CommandDispatchResult
                        {
                            Message = $"{this.Name} can't be called on {gameObjectName}, because it doesn't have a {this.DeclaringType.Name}",
                            Status = CommandDispatchResult.StatusType.TargetMissingComponent
                        };
                    }

                    target = targetComponent;
                } else if (Method.IsStatic) {
                    // The method is static; it therefore doesn't need a target.
                    target = null;
                } else if (Target != null) {
                    // The method is an instance method, so use the target we've
                    // stored.
                    target = Target;
                } else {
                    // We don't know what to call this method on.
                    throw new InvalidOperationException($"Internal error: {nameof(CommandRegistration)} \"{this.Name}\" has no {nameof(Target)}, but method is not static and ${DynamicallyFindsTarget} is false");
                }

                if (this.TryParseArgs(parameters.ToArray(), out var finalParameters, out var errorMessage) == false) {
                    commandCoroutine = default;
                    return new CommandDispatchResult
                    {
                        Status = CommandDispatchResult.StatusType.InvalidParameterCount,
                        Message = errorMessage,
                };
                }

                var returnValue = this.Method.Invoke(target, finalParameters);

                if (returnValue is Coroutine coro)
                {
                    commandCoroutine = coro;
                    return new CommandDispatchResult
                    {
                        Status = CommandDispatchResult.StatusType.SucceededAsync
                    };
                }
                else if (returnValue is IEnumerator enumerator)
                {
                    commandCoroutine = dispatcher.StartCoroutine(enumerator);
                    return new CommandDispatchResult
                    {
                        Status = CommandDispatchResult.StatusType.SucceededAsync
                    };
                }
                else
                {
                    commandCoroutine = null;
                    return new CommandDispatchResult
                    {
                        Status = CommandDispatchResult.StatusType.SucceededSync
                    };
                }
            }

            public string UsageString {
                get {
                    var components = new List<string>();

                    components.Add(Name);

                    if (DynamicallyFindsTarget) {
                        var declaringTypeName = DeclaringType.Name;
                        components.Add($"target <i>({declaringTypeName})</i>");
                    }

                    foreach (var parameter in Method.GetParameters()) {
                        var type = parameter.ParameterType;
                        string typeName;

                        if (TypeFriendlyNames.TryGetValue(type, out typeName) == false) {
                            typeName = type.Name;
                        }

                        string displayName = $"{parameter.Name} <i>({typeName})</i>";

                        if (parameter.IsOptional) {
                            displayName = $"[{displayName} = {parameter.DefaultValue}]";
                            
                        }

                        components.Add(displayName);
                    }

                    return string.Join(" ", components);
                }
            }

            readonly Dictionary<Type, string> TypeFriendlyNames = new Dictionary<Type, string> {
                { typeof(int), "number" },
                { typeof(float), "number" },
                { typeof(double), "number" },
                { typeof(Decimal), "number" },
                { typeof(string), "string" },
                { typeof(bool), "bool" },
            };

        }

        private Dictionary<string, CommandRegistration> _commands = new Dictionary<string, CommandRegistration>();

        public Library Library { get; }
        public DialogueRunner DialogueRunner { get; }

        public IEnumerable<ICommand> Commands => _commands.Values;

        public Actions(DialogueRunner dialogueRunner, Library library)
        {
            Library = library;
            DialogueRunner = dialogueRunner;
        }

        private static string GetFullMethodName(MethodInfo method) {
            return $"{method.DeclaringType.FullName}.{method.Name}";
        }
 
        public void RegisterActions() {
            foreach (var registrationFunction in ActionRegistrationMethods) {
                registrationFunction.Invoke(DialogueRunner);
            }
        }

        public void AddCommandHandler(string commandName, Delegate handler)
        {
            if (_commands.ContainsKey(commandName)) {
                Debug.LogError($"Failed to register command {commandName}: a command by this name has already been registered.");
                return;
            } else {
                _commands.Add(commandName, new CommandRegistration(commandName, handler));
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

        public void AddCommandHandler(string commandName, MethodInfo methodInfo) {
            if (_commands.ContainsKey(commandName)) {
                Debug.LogError($"Failed to register command {commandName}: a command by this name has already been registered.");
                return;
            } else {
                _commands.Add(commandName, new CommandRegistration(commandName, methodInfo));
            }
        }

        public void AddCommandHandler(string commandName, Func<Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

        // GYB1 START
        public void AddCommandHandler<T1>(string commandName, Func<T1, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2>(string commandName, Func<T1, T2, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Func<T1, T2, T3, T4, T5, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Func<T1, T2, T3, T4, T5, T6, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
        // GYB1 END

        public void AddCommandHandler(string commandName, Action handler) => AddCommandHandler(commandName, (Delegate)handler);

        // GYB2 START
        public void AddCommandHandler<T1>(string commandName, Action<T1> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2>(string commandName, Action<T1, T2> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3>(string commandName, Action<T1, T2, T3> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Action<T1, T2, T3, T4> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Action<T1, T2, T3, T4, T5> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Action<T1, T2, T3, T4, T5, T6> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(string commandName, Action<T1, T2, T3, T4, T5, T6, T7> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(string commandName, Action<T1, T2, T3, T4, T5, T6, T7, T8> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string commandName, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> handler) => AddCommandHandler(commandName, (Delegate)handler);
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string commandName, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> handler) => AddCommandHandler(commandName, (Delegate)handler);
        // GYB2 END

        public void AddFunction<TResult>(string name, Func<TResult> implementation) => AddFunction(name, (Delegate)implementation);

        // GYB3 START
        public void AddFunction<T1, TResult>(string name, Func<T1, TResult> implementation) => AddFunction(name, (Delegate)implementation);
        public void AddFunction<T1, T2, TResult>(string name, Func<T1, T2, TResult> implementation) => AddFunction(name, (Delegate)implementation);
        public void AddFunction<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> implementation) => AddFunction(name, (Delegate)implementation);
        public void AddFunction<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> implementation) => AddFunction(name, (Delegate)implementation);
        public void AddFunction<T1, T2, T3, T4, T5, TResult>(string name, Func<T1, T2, T3, T4, T5, TResult> implementation) => AddFunction(name, (Delegate)implementation);
        public void AddFunction<T1, T2, T3, T4, T5, T6, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, TResult> implementation) => AddFunction(name, (Delegate)implementation);
        public void AddFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, T7, TResult> implementation) => AddFunction(name, (Delegate)implementation);
        public void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> implementation) => AddFunction(name, (Delegate)implementation);
        public void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> implementation) => AddFunction(name, (Delegate)implementation);
        public void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> implementation) => AddFunction(name, (Delegate)implementation);
        // GYB3 END

        public void RemoveCommandHandler(string commandName)
        {
            if (_commands.Remove(commandName) == false) {
                Debug.LogError($"Can't remove command {commandName}, because no command with this name is currently registered.");
            }
            
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

        CommandDispatchResult ICommandDispatcher.DispatchCommand(string command, out Coroutine commandCoroutine)
        {
            var commandPieces = new List<string>(DialogueRunner.SplitCommandText(command));

            if (commandPieces.Count == 0)
            {
                // No text was found inside the command, so we won't be able to
                // find it.
                commandCoroutine = default;
                return new CommandDispatchResult
                {
                    Status = CommandDispatchResult.StatusType.CommandUnknown
                };
            }

            if (_commands.TryGetValue(commandPieces[0], out var registration))
            {
                // The first part of the command is the command name itself. Remove
                // it to get the collection of parameters that were passed to the
                // command.
                commandPieces.RemoveAt(0);

                return registration.Invoke(DialogueRunner, commandPieces, out commandCoroutine);
            } else {
                commandCoroutine = default;
                return new CommandDispatchResult
                {
                    Status = CommandDispatchResult.StatusType.CommandUnknown
                };
            }
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
                        return null;
                    }
                    return gameObject.GetComponentInChildren(targetType);
                };
            }

            // bools can take "true" or "false", or the parameter name.
            if (typeof(bool).IsAssignableFrom(targetType))
            {
                return arg =>
                {
                    // If the argument is the name of the parameter, interpret
                    // the argument as 'true'.
                    if (arg.Equals(parameter.Name, StringComparison.InvariantCultureIgnoreCase)) 
                    {
                        return true;
                    }

                    // If the argument can be parsed as boolean true or false,
                    // return that result.
                    if (bool.TryParse(arg, out bool res))
                    {
                        return res;
                    }

                    // We can't parse the argument.
                    throw new ArgumentException(
                        $"Can't convert the given parameter at position {index + 1} (\"{arg}\") to parameter " +
                        $"{parameter.Name} of type {typeof(bool).FullName}.");
                };
            }

            // Fallback: try converting using IConvertible.
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
                        $"{parameter.Name} of type {targetType.FullName}: {e}", e);
                }
            };
        }


        internal static HashSet<System.Action<IActionRegistration>> ActionRegistrationMethods = new HashSet<Action<IActionRegistration>>();


        public static void AddRegistrationMethod(Action<IActionRegistration> registerActions)
        {
            ActionRegistrationMethods.Add(registerActions);
        }

        public static Yarn.Library GetLibrary()
        {
            var library = new Yarn.Library();

            var proxy = new LibraryRegistrationProxy(library);

            foreach (var registrationMethod in ActionRegistrationMethods)
            {
                registrationMethod.Invoke(proxy);
            }

            return library;
        }

        public void AddCommandHandler(string commandName, Func<IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }

        // GYB4 START
        public void AddCommandHandler<T1>(string commandName, Func<T1, IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
        public void AddCommandHandler<T1, T2>(string commandName, Func<T1, T2, IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
        public void AddCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
        public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
        public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Func<T1, T2, T3, T4, T5, IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Func<T1, T2, T3, T4, T5, T6, IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
        public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IEnumerator> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
        // GYB4 END

        /// <summary>
        /// A helper class that registers functions into a <see
        /// cref="Yarn.Library"/>.
        /// </summary>
        private class LibraryRegistrationProxy : IActionRegistration
        {
            private Library library;

            public LibraryRegistrationProxy(Library library)
            {
                this.library = library;
            }

            public void AddCommandHandler(string commandName, Delegate handler)
            {
                // No action; this class does not handle commands, only functions.
                return;
            }

            public void AddCommandHandler(string commandName, Func<Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);

            public void AddCommandHandler(string commandName, MethodInfo methodInfo) => AddCommandHandler(commandName, (Delegate)null);

            // GYB5 START
            public void AddCommandHandler<T1>(string commandName, Func<T1, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2>(string commandName, Func<T1, T2, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Func<T1, T2, T3, T4, T5, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Func<T1, T2, T3, T4, T5, T6, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, Coroutine> handler) => AddCommandHandler(commandName, (Delegate)handler);
            // GYB5 END

            public void AddCommandHandler(string commandName, Func<IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);

            // GYB6 START
            public void AddCommandHandler<T1>(string commandName, Func<T1, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2>(string commandName, Func<T1, T2, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3>(string commandName, Func<T1, T2, T3, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Func<T1, T2, T3, T4, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Func<T1, T2, T3, T4, T5, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Func<T1, T2, T3, T4, T5, T6, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string commandName, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, IEnumerator> handler) => AddCommandHandler(commandName, (Delegate)handler);
            // GYB6 END

            public void AddCommandHandler(string commandName, Action handler) => AddCommandHandler(commandName, (Delegate)handler);

            // GYB7 START
            public void AddCommandHandler<T1>(string commandName, Action<T1> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2>(string commandName, Action<T1, T2> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3>(string commandName, Action<T1, T2, T3> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4>(string commandName, Action<T1, T2, T3, T4> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5>(string commandName, Action<T1, T2, T3, T4, T5> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6>(string commandName, Action<T1, T2, T3, T4, T5, T6> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7>(string commandName, Action<T1, T2, T3, T4, T5, T6, T7> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8>(string commandName, Action<T1, T2, T3, T4, T5, T6, T7, T8> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string commandName, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> handler) => AddCommandHandler(commandName, (Delegate)handler);
            public void AddCommandHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string commandName, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> handler) => AddCommandHandler(commandName, (Delegate)handler);
            // GYB7 END

            public void AddFunction(string name, Delegate implementation) {
                // Check to see if the function already exists in our Library,
                // and error out if it does
                if (library.FunctionExists(name)) {
                    throw new ArgumentException($"Cannot register function {name}: a function with this name already exists");
                }
                // Register this function in the library
                library.RegisterFunction(name, implementation);
            }

            public void AddFunction<TResult>(string name, Func<TResult> implementation) => AddFunction(name, (Delegate)implementation);

            // GYB8 START
            public void AddFunction<T1, TResult>(string name, Func<T1, TResult> implementation) => AddFunction(name, (Delegate)implementation);
            public void AddFunction<T1, T2, TResult>(string name, Func<T1, T2, TResult> implementation) => AddFunction(name, (Delegate)implementation);
            public void AddFunction<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> implementation) => AddFunction(name, (Delegate)implementation);
            public void AddFunction<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> implementation) => AddFunction(name, (Delegate)implementation);
            public void AddFunction<T1, T2, T3, T4, T5, TResult>(string name, Func<T1, T2, T3, T4, T5, TResult> implementation) => AddFunction(name, (Delegate)implementation);
            public void AddFunction<T1, T2, T3, T4, T5, T6, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, TResult> implementation) => AddFunction(name, (Delegate)implementation);
            public void AddFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, T7, TResult> implementation) => AddFunction(name, (Delegate)implementation);
            public void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> implementation) => AddFunction(name, (Delegate)implementation);
            public void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> implementation) => AddFunction(name, (Delegate)implementation);
            public void AddFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> implementation) => AddFunction(name, (Delegate)implementation);
            // GYB8 END

            public void RemoveCommandHandler(string commandName) => throw new InvalidOperationException("This class does not support removing actions.");

            public void RemoveFunction(string name) => throw new InvalidOperationException("This class does not support removing actions.");
        }
    }
}
