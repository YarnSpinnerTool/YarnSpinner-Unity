/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Yarn.Shared;

#nullable enable

namespace Yarn.Unity
{
    public static class ActionExtensions
    {
        public static NamedType ToNamedType(this Type type)
        {
            bool isUnityComponent = typeof(Component).IsAssignableFrom(type);
            var nt = new NamedType($"global::{type.FullName}", type.Name, type.FullName, YarnSpecialType.Other, isUnityComponent);
            return nt;
        }
        public static string GlobalMethodName(this MethodInfo methodInfo) // I think this is right, will have to double check that
        {
            return $"global::{methodInfo.FullMethodName()}";
        }
        public static string FullMethodName(this MethodInfo method)
        {
            return $"{method.DeclaringType.FullName}.{method.Name}";
        }
        public static YarnSpecialType ToSpecialType(this Type type)
        {
            Dictionary<Type, YarnSpecialType> basicReturns = new()
            {
                {typeof(string), YarnSpecialType.String},
                {typeof(bool), YarnSpecialType.Boolean},
                {typeof(int), YarnSpecialType.Int32},
                {typeof(uint), YarnSpecialType.UInt32},
                {typeof(short), YarnSpecialType.Int16},
                {typeof(ushort), YarnSpecialType.UInt16},
                {typeof(byte), YarnSpecialType.Byte},
                {typeof(sbyte), YarnSpecialType.SByte},
                {typeof(long), YarnSpecialType.Int64},
                {typeof(ulong), YarnSpecialType.UInt64},
                {typeof(float), YarnSpecialType.Single},
                {typeof(double), YarnSpecialType.Double},
                {typeof(decimal), YarnSpecialType.Decimal},
            };

            if (basicReturns.TryGetValue(type, out var result))
            {
                return result;
            }
            return YarnSpecialType.Other;
        }
    }

    public class Actions : IActionRegistration
    {
        private static string[] asyncReturns =
        {
            "Yarn.Unity.YarnTask",
            "System.Threading.Tasks.Task",
            "Cysharp.Threading.Tasks.UniTask",
            "UnityEngine.Awaitable",
        };
        private static Dictionary<Type, ReturnType> basicReturns = new()
        {
            {typeof(string), ReturnType.String},
            {typeof(bool), ReturnType.Boolean},

            {typeof(int), ReturnType.Number},
            {typeof(uint), ReturnType.Number},
            {typeof(short), ReturnType.Number},
            {typeof(ushort), ReturnType.Number},
            {typeof(byte), ReturnType.Number},
            {typeof(sbyte), ReturnType.Number},
            {typeof(long), ReturnType.Number},
            {typeof(ulong), ReturnType.Number},
            {typeof(float), ReturnType.Number},
            {typeof(double), ReturnType.Number},
            {typeof(decimal), ReturnType.Number},
        };

        public static Yarn.Shared.Action ActionFromMethodInfo(MethodInfo method, string yarnName, ActionType actionType)
        {
            List<Parameter> parameters = new();
            foreach (var paramInfo in method.GetParameters())
            {
                var p = ParameterFromInfo(paramInfo);
                parameters.Add(p);
            }

            bool isInstance = true;

            // super crude hack to make it "detect" if it is a local function or lambda
            // this isn't realistically rigorous enough but will work for now I think
            if (method.Name.Contains('<') &&  method.Name.Contains('>'))
            {
                isInstance = false;
            }
            if (method.IsStatic)
            {
                isInstance = false;
            }
            
            foreach (var art in asyncReturns)
            {
                if (method.ReturnType.FullName == art)
                {
                    return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.AsyncVoid, method.DeclaringType.ToNamedType(), parameters.ToArray());
                }
            }
            
            if (method.ReturnType == typeof(void))
            {
                return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.Void, method.DeclaringType.ToNamedType(), parameters.ToArray());
            }
            else if(basicReturns.ContainsKey(method.ReturnType))
            {
                return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, basicReturns[method.ReturnType], method.DeclaringType.ToNamedType(), parameters.ToArray());
            }
            else if (method.ReturnType.FullName == "UnityEngine.Coroutine")
            {
                return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.CoroutineVoid, method.DeclaringType.ToNamedType(), parameters.ToArray());
            }
            else if (method.ReturnType == typeof(IEnumerator))
            {
                return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.IEnumeratorVoid, method.DeclaringType.ToNamedType(), parameters.ToArray());
            }
            else
            {
                // ok so at this stage we are either an invalid action
                // or we are a generic task
                // which requires a bit more work
                // le sigh
                if (method.ReturnType.IsGenericType)
                {
                    var baseType = method.ReturnType.BaseType;
                    var wrappedTypes = method.ReturnType.GetGenericArguments();
                    if (wrappedTypes.Length == 1)
                    {
                        var internalType = wrappedTypes[0];
                        foreach (var art in asyncReturns)
                        {
                            if (baseType.FullName == art)
                            {
                                if (basicReturns.ContainsKey(internalType))
                                {
                                    switch (basicReturns[internalType])
                                    {
                                        case ReturnType.String:
                                            return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.AsyncString, method.DeclaringType.ToNamedType(), parameters.ToArray());
                                        case ReturnType.Number:
                                            return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.AsyncNumber, method.DeclaringType.ToNamedType(), parameters.ToArray());
                                        case ReturnType.Boolean:
                                            return new Shared.Action(yarnName, method.GlobalMethodName(), method.Name, actionType, isInstance, ReturnType.Boolean, method.DeclaringType.ToNamedType(), parameters.ToArray());
                                    }
                                }
                            }
                        }
                    }

                }
            }

            return new InvalidAction(method.Name, actionType);
        }

        public static Parameter ParameterFromInfo(ParameterInfo parameterInfo)
        {
            // parameters are allowed to be
                // various numeric types
                // strings
                // bools
                // tokens
                    // as long as they are the last parameter
                // enums
                    // as long as they are a Yarn Enum
                // game objects
                // component derived classes
            // parameters can
                // be an array of the above types
                    // as long as it is the last parameter
                    // and not a token
                // have default values
            
            // is it an array?
            var pType = parameterInfo.ParameterType;
            if (parameterInfo.ParameterType.IsArray)
            {
                pType = parameterInfo.ParameterType.GetElementType();
            }

            // is it attributed as an enum?
            string? enumAttributeValue = parameterInfo.GetCustomAttribute<Attributes.YarnEnumParameterAttribute>()?.Name;

            // is it attributed as a node?
            var nodeAttributeType = typeof(Attributes.YarnNodeParameterAttribute);
            var isNodeAttributed = parameterInfo.GetCustomAttribute(nodeAttributeType) != null;

            // and finally does it have a default value?
            string? defaultDisplay = parameterInfo.DefaultValue?.ToString();

            Type[] basics = new Type[]
            {
                typeof(bool),
                typeof(sbyte),
                typeof(byte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(decimal),
                typeof(float),
                typeof(double),
                typeof(string),
            };
            for (int i = 0; i < basics.Length; i++)
            {
                if (basics[i] != parameterInfo.ParameterType)
                {
                    continue;
                }

                return new BasicParameter(parameterInfo.Name, basics[i].ToSpecialType(), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
            }

            if (pType == typeof(System.Threading.CancellationToken) || pType == typeof(LineCancellationToken))
            {
                return new TokenParameter(parameterInfo.Name, pType == typeof(LineCancellationToken), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
            }
            else if (typeof(GameObject).IsAssignableFrom(pType))
            {
                return new GameObjectParameter(parameterInfo.Name, pType.ToNamedType(), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
            }
            else if (typeof(Component).IsAssignableFrom(pType))
            {
                return new ComponentParameter(parameterInfo.Name, pType.ToNamedType(), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
            }
            else if (pType.IsEnum && pType.IsDefined(typeof(Attributes.YarnGeneratedEnumAttribute), false))
            {
                // so now we know we are an enum and flagged as a yarn generated enum
                // which means we can now trust the source gen to have written the requisite code to let us interface with this
                // but I am unsure if I can do dynamic calls like this...
                var attribute = pType.GetCustomAttribute<Attributes.YarnGeneratedEnumAttribute>();
                if (attribute == null)
                {
                    return new UnknownParameter(parameterInfo.Name, pType.ToNamedType(), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, null);
                }
                return new EnumParameter(parameterInfo.Name, pType.Name, attribute.backing, parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
            }

            return new UnknownParameter(parameterInfo.Name, pType.ToNamedType(), parameterInfo.ParameterType.IsArray, parameterInfo.IsOut, parameterInfo.HasDefaultValue, defaultDisplay, isNodeAttributed, enumAttributeValue);
        }

        internal class RuntimeCommand
        {
            public RuntimeCommand(string name, Delegate @delegate)
            {
                // we make an action from the method info
                // and it's possible we can make a valid action but one we can't invoke
                // so we validate it and if it fails validation we then wipe it
                var action = Actions.ActionFromMethodInfo(@delegate.Method, name, ActionType.Command);
                if (action is not InvalidAction)
                {
                    if (!Shared.Action.TryValidateAction(action, Array.Empty<Shared.YarnConverter>(), null, false))
                    {
                        Debug.LogError("failed validation");
                        action = new InvalidAction(action.Name, ActionType.Command);
                    }
                }

                Name = name;
                Method = @delegate.Method;
                Action = action;
                // delegates have a target which is needed for all non-instance non-static commands
                // aka locals and lambdas which are implictly capturing this
                // so we scoop up the target and keep it around for later when it comes time to invoke the method
                Target = @delegate.Target;
            }

            public RuntimeCommand(string name, MethodInfo method)
            {
                // we make an action from the method info
                // and it's possible we can make a valid action but one we can't invoke
                // so we validate it and if it fails validation we then wipe it
                var action = Actions.ActionFromMethodInfo(method, name, ActionType.Command);
                if (action is not InvalidAction)
                {
                    if (!Shared.Action.TryValidateAction(action, Array.Empty<Shared.YarnConverter>(), null, false))
                    {
                        action = new InvalidAction(action.Name, ActionType.Command);
                    }
                }

                Name = name;
                Method = method;
                Action = action;
            }

            public string Name { get; set; }
            public MethodInfo Method { get; set; }
            private Shared.Action Action;
            private object? Target { get; set; }

            private static bool TryConvertParameter(Parameter parameter, string input, Type type, out object? value)
            {
                // we know how to handle game objects, components, and basic parameters
                // for everything else we ask the collection of converters that the invoker has lying around and hope one of those can handle it
                if (parameter is GameObjectParameter)
                {
                    value = GameObject.Find(input);
                    return value != null;
                }
                else if (parameter is ComponentParameter cp)
                {
                    var go = GameObject.Find(input);
                    if (go == null)
                    {
                        value = null;
                        return false;
                    }
                    value = go.GetComponentInChildren(type);
                    return value != null;
                }
                else if (parameter is BasicParameter bp)
                {
                    switch (bp.SpecialType)
                    {
                        case YarnSpecialType.String:
                            value = input;
                            return true;

                        case YarnSpecialType.Boolean:
                            value = input == parameter.Name || input.ToLowerInvariant() == "true"; // should this be a case insenstive check?
                            return true;

                        case YarnSpecialType.Byte:
                            {
                                var result = byte.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.Decimal:
                            {
                                var result = decimal.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.Double:
                            {
                                var result = double.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }    
                        case YarnSpecialType.Int16:
                            {
                                var result = short.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.Int32:
                            {
                                var result = int.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.Int64:
                            {
                                var result = long.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.SByte:
                            {
                                var result = sbyte.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.Single:
                            {
                                var result = float.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.UInt16:
                            {
                                var result = ushort.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.UInt32:
                            {
                                var result = uint.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        case YarnSpecialType.UInt64:
                            {
                                var result = ulong.TryParse(input, out var converted);
                                value = converted;
                                return result;
                            }
                        
                        default:
                            value = null;
                            return false;
                    }
                }
                // if any of the runtime converters say they can handle it then we will let them take the wheel
                else if (ActionInvoker.TryRuntimeConvert(input, type, out value))
                {
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            public CommandDispatchResult Invoke(string[] parameters, LineCancellationToken token, MonoBehaviour dispatcher)
            {
                if (Action is InvalidAction)
                {
                    return new CommandDispatchResult(CommandDispatchResult.StatusType.CommandUnknown);
                }

                var (min, max) = Action.NumberOfParameters;
                if (parameters.Length < min || parameters.Length > max)
                {
                    return new CommandDispatchResult(CommandDispatchResult.StatusType.InvalidParameterCount);
                }

                // I think this is all I need to do to get the right number of parameters
                // we want a slice of all parameters but the first one

                // if we are an instance command we need to lop off the first two parameters
                // one for the name of the command
                // and one for the instance
                ArraySegment<string> paramSlice;
                if (Action.IsInstance)
                {
                    paramSlice = new ArraySegment<string>(parameters, 2, parameters.Length - 2);
                }
                else
                {
                    // if we are a static, local, or lambda we only drop the first parameter
                    // which is the name of the command itself
                    paramSlice = new ArraySegment<string>(parameters, 1, parameters.Length - 1);
                }
                
                object?[] resolvedParams = new object[Action.Parameters.Length];
                var parameterInfos = Method.GetParameters();

                for (int i = 0; i < Action.Parameters.Length; i++)
                {
                    var p = Action.Parameters[i];

                    if (p is TokenParameter tp)
                    {
                        if (tp.IsYarnToken)
                        {
                            resolvedParams[i] = token;
                        }
                        else
                        {
                            resolvedParams[i] = token.HurryUpToken;
                        }
                        continue;
                    }

                    var pType = parameterInfos[i].ParameterType;
                    if (p.IsArray)
                    {
                        pType = parameterInfos[i].ParameterType.GetElementType();
                    }
                    
                    // if we are an array we eat all the remaining values
                    if (p.IsArray)
                    {
                        var remainingParameters = paramSlice.Count - i;
                        // if we have no parameters left in the invocation we don't need to bother doing anythign
                        // we can just make the empty array and put that in
                        if (remainingParameters < 1)
                        {
                            resolvedParams[i] = Array.CreateInstance(pType, 0);
                            continue;
                        }

                        var array = Array.CreateInstance(pType, remainingParameters);
                        for (int j = 0; j < remainingParameters; j++)
                        {
                            if (TryConvertParameter(p, paramSlice[i + j], pType, out var value))
                            {
                                array.SetValue(value, j);
                            }
                            else
                            {
                                Debug.LogError($"Unable to determine a way to convert {paramSlice[i + j]} into a {pType.Name}.");
                                return new CommandDispatchResult(CommandDispatchResult.StatusType.InvalidParameter);
                            }
                        }
                        resolvedParams[i] = array;
                    }
                    else
                    {
                        // because parameters can have default values if we encounter a parameter that we don't have a matching parameter value for we flag it as being defaulted
                        // this would fail if there was a mix of default and tokens but because we require specific ordering we can trust earlier checks to have caught this
                        if (i < paramSlice.Count)
                        {
                            if (TryConvertParameter(p, paramSlice[i], pType, out var value))
                            {
                                resolvedParams[i] = value;
                            }
                            else
                            {
                                Debug.LogError($"Unable to determine a way to convert {paramSlice[i]} into a {pType.Name}.");
                                return new CommandDispatchResult(CommandDispatchResult.StatusType.InvalidParameter);
                            }
                        }
                        else
                        {
                            resolvedParams[i] = System.Type.Missing;
                        }
                    }
                }

                object returnValue;
                if (Action.IsInstance)
                {
                    // we are an instance method
                    // which means we need a target and there are two possible target types
                    // either we are a component or we are converted type
                    // we check for the converted type first just in case it has a custom converter on a type that is also a mono behaviour
                    var targetName = parameters[1];

                    if (ActionInvoker.TryRuntimeConvert(targetName, Method.DeclaringType, out object? target))
                    {
                        returnValue = this.Method.Invoke(target, resolvedParams);
                    }
                    else if (typeof(UnityEngine.Component).IsAssignableFrom(Method.DeclaringType))
                    {
                        var go = GameObject.Find(targetName);
                        if (go == null)
                        {
                            return new CommandDispatchResult(CommandDispatchResult.StatusType.NoTargetFound);
                        }
                        target = go.GetComponentInChildren(Method.DeclaringType);
                        if (target == null)
                        {
                            return new CommandDispatchResult(CommandDispatchResult.StatusType.NoTargetFound);
                        }
                        returnValue = this.Method.Invoke(target, resolvedParams);
                    }
                    else
                    {
                        return new CommandDispatchResult(CommandDispatchResult.StatusType.NoTargetFound);
                    }
                }
                else
                {
                    // it's static so I don't need a target
                    // I can just call it
                    returnValue = this.Method.Invoke(this.Target, resolvedParams);
                }

                if (returnValue is Coroutine coro)
                {
                    // The method returned a Coroutine object.
                    return new CommandDispatchResult(CommandDispatchResult.StatusType.Succeeded, dispatcher.WaitForCoroutine(coro));
                }
                else if (returnValue is YarnTask yarnTask)
                {
                    return new CommandDispatchResult(CommandDispatchResult.StatusType.Succeeded, yarnTask);
                }
#if UNITY_2023_1_OR_NEWER
                else if (returnValue is Awaitable awaitable)
                {
                    // The method returned an Awaitable. Convert it to a
                    // YarnTask. (Awaitables implement IEnumerator, so check
                    // this before testing against other IEnumerators like
                    // coroutines.)
                    return new CommandDispatchResult(CommandDispatchResult.StatusType.Succeeded, awaitable);
                }
#endif
                else if (returnValue is IEnumerator enumerator)
                {
                    // The method returned an IEnumerator.
                    return new CommandDispatchResult(CommandDispatchResult.StatusType.Succeeded, dispatcher.WaitForCoroutine(enumerator));
                }
                else if (returnValue is System.Threading.Tasks.Task task)
                {
                    // The method returned a task. Convert it to a YarnTask.
                    return new CommandDispatchResult(CommandDispatchResult.StatusType.Succeeded, task);
                }
#if USE_UNITASK
                else if (returnValue is Cysharp.Threading.Tasks.UniTask unitask)
                {
                    // The method returned a UniTask. Convert it to a YarnTask.
                    return new CommandDispatchResult(CommandDispatchResult.StatusType.Succeeded, unitask);
                }
#endif
                else
                {
                    // The method returned no value.
                    return new CommandDispatchResult(CommandDispatchResult.StatusType.Succeeded);
                }
            }
        }

        private Dictionary<string, RuntimeCommand> _commands = new Dictionary<string, RuntimeCommand>();

        private BasicFunctionLibrary Library = new();

        public void AddCommandHandler(string commandName, Delegate handler)
        {
            if (commandName.Contains(' '))
            {
                Debug.LogError($"Failed to register command {commandName}: command names are not allowed to contain spaces.");
                return;
            }

            if (_commands.ContainsKey(commandName))
            {
                Debug.LogError($"Failed to register command {commandName}: a command by this name has already been registered.");
                return;
            }
            else
            {
#if YARN_SOURCE_GENERATION_DEBUG_LOGGING
                Debug.Log($"Registering command {commandName}");
#endif
                _commands.Add(commandName, new RuntimeCommand(commandName, handler));
            }
        }

        public void AddFunction(string name, Delegate implementation)
        {
            // ok so part of the issue is the existing flow I was looking at seemed to only work for commands?
            // so functions always worked totally differently?
            // ok then...
            // alright so I guess first thing is I should see if the runtime commands are behaving as expected
            // and then if they are then I can look at making functions also work this way
            if (name.Contains(' '))
            {
                Debug.LogError($"Cannot add function {name}: function names are not allowed to contain spaces.");
                return;
            }

            if (Library.HasFunction(name))
            {
                Debug.LogError($"Cannot add function {name}: one already exists");
                return;
            }
#if YARN_SOURCE_GENERATION_DEBUG_LOGGING
            Debug.Log($"Registering command {name} from method {implementation.Method.DeclaringType.FullName}.{implementation.Method.Name}");
#endif

            Library.RegisterFunction(name, implementation);
        }

        public void AddCommandHandler(string commandName, MethodInfo methodInfo)
        {
            if (commandName.Contains(' '))
            {
                Debug.LogError($"Failed to register command {commandName}: command names are not allowed to contain spaces.");
                return;
            }

            if (_commands.ContainsKey(commandName))
            {
                Debug.LogError($"Failed to register command {commandName}: a command by this name has already been registered.");
                return;
            }
            else
            {
                _commands.Add(commandName, new RuntimeCommand(commandName, methodInfo));
            }
        }
        public void RemoveCommandHandler(string commandName)
        {
            if (_commands.Remove(commandName) == false)
            {
                Debug.LogError($"Can't remove command {commandName}, because no command with this name is currently registered.");
            }
        }

        public void RemoveFunction(string name)
        {
            if (Library.HasFunction(name) == false)
            {
                Debug.LogError($"Cannot remove function {name}: no function with that name exists in the library");
                return;
            }
            Library.DeregisterFunction(name);
        }

        public bool CanInvoke(string functionName)
        {
            return Library.HasFunction(functionName);
        }
        public bool CanDispatch(string commandName)
        {
            return _commands.ContainsKey(commandName);
        }

        internal CommandDispatchResult DispatchCommand(string command, MonoBehaviour coroutineHost, LineCancellationToken token)
        {
            var commandPieces = new List<string>(DialogueRunner.SplitCommandText(command));

            if (commandPieces.Count == 0)
            {
                // No text was found inside the command, so we won't be able to
                // find it.
                return new CommandDispatchResult(CommandDispatchResult.StatusType.CommandUnknown, YarnTask.CompletedTask);
            }

            if (_commands.TryGetValue(commandPieces[0], out var registration))
            {
                return registration.Invoke(commandPieces.ToArray(), token, coroutineHost);
            }
            else
            {
                return new CommandDispatchResult(CommandDispatchResult.StatusType.CommandUnknown);
            }
        }

        public async YarnTask<IConvertible> Invoke(string functionName, IConvertible[] parameters, System.Threading.CancellationToken token)
        {
            return await Library.Invoke(functionName, parameters, token);
        }

        public void AddCommandHandler(string commandName, Func<object> handler)
        {
            this.AddCommandHandler(commandName, (Delegate)handler);
        }
    }
}
