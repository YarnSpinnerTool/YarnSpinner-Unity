/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

#nullable enable

namespace Yarn.Unity
{
    public static class ActionInvoker
    {
        public delegate YarnTask<IConvertible> InvokeFunction(string functionName, IConvertible[] parameters, CancellationToken token);
        public delegate bool CanHandleFunction(string functionName);
        private static List<(CanHandleFunction check, InvokeFunction call)> functionDelegates = new();

        public delegate bool CanHandleCommand(string commandName);
        public delegate YarnTask InvokeCommand(Command command, LineCancellationToken token);
        private static List<(CanHandleCommand check, InvokeCommand call)> commandDelegates = new();


        public delegate bool ConvertRuntimeStringValue(string input, Type type, out object? value);
        public delegate bool ConvertRuntimeIConvertibleValue(IConvertible input, Type type, out object? value);
        private static List<ConvertRuntimeStringValue> runtimeStringConverters = new();
        private static List<ConvertRuntimeIConvertibleValue> runtimeConvertibleConverters = new();
        public static void RegisterRuntimeStringConverter(ConvertRuntimeStringValue converter)
        {
            runtimeStringConverters.Add(converter);
        }
        public static bool TryRuntimeConvert(string input, Type type, out object? value)
        {
            foreach (var converter in runtimeStringConverters)
            {
                if (converter(input, type, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }
        public static void RegisterRuntimeIConvertibleConverter(ConvertRuntimeIConvertibleValue converter)
        {
            runtimeConvertibleConverters.Add(converter);
        }
        public static bool TryRuntimeConvert(IConvertible input, Type type, out object? value)
        {
            foreach (var converter in runtimeConvertibleConverters)
            {
                if (converter(input, type, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        public static void RegisterFunctionHandler(CanHandleFunction check, InvokeFunction call)
        {
            functionDelegates.Add((check, call));
        }

        public static bool CanInvoke(string functionName)
        {
            foreach (var (check, _) in functionDelegates)
            {
                if (check(functionName))
                {
                    return true;
                }
            }

            // now check all the runtime registered methods
            return runtimeDelegateActionInvoker?.CanInvoke(functionName) ?? false;
        }
        public static YarnTask<IConvertible> Invoke(string functionName, IConvertible[] parameters, CancellationToken token)
        {
            foreach (var (check, call) in functionDelegates)
            {
                if (check(functionName))
                {
                    return call(functionName, parameters, token);
                }
            }

            Debug.Log("falling back on the runtime function approach");

            // now check the run time registered ones
            if (runtimeDelegateActionInvoker != null)
            {
                if (runtimeDelegateActionInvoker.CanInvoke(functionName))
                {
                    return runtimeDelegateActionInvoker.Invoke(functionName, parameters, token);
                }
            }

            throw new ArgumentException($"We do not have a function called '{functionName}'");
        }

        public static void RegisterCommandHandler(CanHandleCommand check, InvokeCommand call)
        {
            commandDelegates.Add((check, call));
        }

        internal static YarnTask Invoke(Command command, MonoBehaviour host, LineCancellationToken token)
        {
            var parts = new List<string>(DialogueRunner.SplitCommandText(command.Text));
            var commandName = parts[0];

            foreach (var (check, call) in commandDelegates)
            {
                if (check(commandName))
                {
                    var result = call(command, token);
                    return result;
                }
            }

            if (runtimeDelegateActionInvoker == null)
            {
                Debug.LogError($"The command dispatcher has not been configured to handle {commandName}");
                return YarnTask.CompletedTask;
            }

            var dispatchResult = runtimeDelegateActionInvoker.DispatchCommand(command.Text, host, token);

            if (dispatchResult is ActionDispatchFailed adf)
            {
                switch (adf.Status)
                {
                    case StatusType.ActionUnknown:
                        Debug.Log($"Encountered undispatched command <<{command.Text}>>.");
                        return YarnTask.CompletedTask;
                    case StatusType.NoTargetFound:
                        Debug.LogError($"Can't call command <<{command.Text}>>: failed to find a target object named {parts[1]}");
                        return YarnTask.CompletedTask;
                    case StatusType.InvalidReturn:
                        Debug.LogError($"Can't call command <<{command.Text}>>: the action had an invalid return type for a command");
                        return YarnTask.CompletedTask;
                    case StatusType.InvalidParameterCount:
                        Debug.LogError($"Can't call command <<{command.Text}>>: incorrect number of parameters");
                        return YarnTask.CompletedTask;
                    case StatusType.InvalidParameter:
                        Debug.LogError($"Can't call command <<{command.Text}>>: invalid parameter encountered during invocation");
                        return YarnTask.CompletedTask;
                }
            }

            // we are a synchronous command
            // aka we are already done, so just say so
            if (dispatchResult is CommandDispatchSucceededSynchronously)
            {
                return YarnTask.CompletedTask;
            }

            // we are an ASYNC command
            // so we want to return the task within so the rest of the system can await us
            if (dispatchResult is CommandDispatchSucceededAsynchronously cdsa)
            {
                return cdsa.Task;
            }

            // there is no other form of dispatch result we'd expect
            // so it's an error to get here
            throw new System.InvalidOperationException($"Encountered a dispatched command: <<{command.Text}>> that did not fail or succeed?!");
        }

        private static Actions? runtimeDelegateActionInvoker;
        public static void AddCommandHandler(string commandName, Delegate handler)
        {
            foreach (var (check, _) in commandDelegates)
            {
                if (check(commandName))
                {
                    return;
                }
            }
            runtimeDelegateActionInvoker ??= new Actions();
            runtimeDelegateActionInvoker.AddCommandHandler(commandName, handler);
        }
        public static void AddCommandHandler(string commandName, System.Reflection.MethodInfo method)
        {
            foreach (var (check, _) in commandDelegates)
            {
                if (check(commandName))
                {
                    return;
                }
            }
            runtimeDelegateActionInvoker ??= new Actions();
            runtimeDelegateActionInvoker.AddCommandHandler(commandName, method);
        }
        public static void RemoveCommandHandler(string commandName)
        {
            foreach (var (check, _) in commandDelegates)
            {
                if (check(commandName))
                {
                    // should this log?
                    return;
                }
            }
            runtimeDelegateActionInvoker ??= new Actions();
            runtimeDelegateActionInvoker.RemoveCommandHandler(commandName);
        }
        public static void AddFunction(string name, Delegate implementation)
        {
            foreach (var (check, _) in functionDelegates)
            {
                if (check(name))
                {
                    return;
                }
            }
            runtimeDelegateActionInvoker ??= new Actions();
            runtimeDelegateActionInvoker.AddFunction(name, implementation);
        }
        public static void AddFunction(string name, System.Reflection.MethodInfo implementation)
        {
            foreach (var (check, _) in functionDelegates)
            {
                if (check(name))
                {
                    return;
                }
            }
            runtimeDelegateActionInvoker ??= new Actions();
            runtimeDelegateActionInvoker.AddFunction(name, implementation);
        }

        public static void RemoveFunction(string name)
        {
            foreach (var (check, _) in functionDelegates)
            {
                if (check(name))
                {
                    // should this log?
                    return;
                }
            }
            runtimeDelegateActionInvoker ??= new Actions();
            runtimeDelegateActionInvoker.RemoveFunction(name);
        }
    }
}
