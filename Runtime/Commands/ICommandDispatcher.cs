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
            return actionDelegateThing?.CanInvoke(functionName) ?? false;
        }
        public static YarnTask<IConvertible> Invoke(string functionName, IConvertible[] parameters, CancellationToken token)
        {
            foreach (var (check, call) in functionDelegates)
            {
                if (check(functionName))
                {
                    Debug.Log("found a delegate to handle the function");
                    return call(functionName, parameters, token);
                }
            }

            Debug.Log("falling back on the runtime function approach");

            // now check the run time registered ones
            if (actionDelegateThing != null)
            {
                if (actionDelegateThing.CanInvoke(functionName))
                {
                    return actionDelegateThing.Invoke(functionName, parameters, token);
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
                    Debug.Log($"found a delegate for {commandName}");

                    var result = call(command, token);
                    return result;
                }
            }

            // I might have to special case wait
            // get back to this
            Debug.Log("falling back on the runtime command approach");

            if (actionDelegateThing == null)
            {
                throw new ArgumentException($"Internal error: command dispatcher has not been configured");
            }

            CommandDispatchResult dispatchResult = actionDelegateThing.DispatchCommand(command.Text, host, token);
            switch (dispatchResult.Status)
            {
                case CommandDispatchResult.StatusType.Succeeded:
                    // The command has successfully dispatched, but has not
                    // yet finished running. Wait for it to finish.
                    return dispatchResult.Task;
                case CommandDispatchResult.StatusType.NoTargetFound:
                    Debug.LogError($"Can't call command <<{command.Text}>>: failed to find a game object named {parts[1]}");
                    return YarnTask.CompletedTask;
                case CommandDispatchResult.StatusType.TargetMissingComponent:
                    Debug.LogError($"Can't call command <<{command.Text}>>, because {parts[1]} doesn't have the correct component");
                    return YarnTask.CompletedTask;
                case CommandDispatchResult.StatusType.InvalidParameterCount:
                    Debug.LogError($"Can't call command <<{command.Text}>>: {dispatchResult.Message ?? "incorrect number of parameters"}");
                    return YarnTask.CompletedTask;
                case CommandDispatchResult.StatusType.CommandUnknown:
                    Debug.Log($"Encountered undispatched command <<{command.Text}>>.");
                    return YarnTask.CompletedTask;
                default:
                    throw new ArgumentOutOfRangeException($"Internal error: Unknown command dispatch result status {dispatchResult}");
            }
        }

        private static Actions? actionDelegateThing;
        public static void AddCommandHandler(string commandName, Delegate handler)
        {
            foreach (var (check, _) in commandDelegates)
            {
                if (check(commandName))
                {
                    return;
                }
            }
            actionDelegateThing ??= new Actions();
            actionDelegateThing.AddCommandHandler(commandName, handler);
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
            actionDelegateThing ??= new Actions();
            actionDelegateThing.AddCommandHandler(commandName, method);
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
            actionDelegateThing ??= new Actions();
            actionDelegateThing.RemoveCommandHandler(commandName);
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
            actionDelegateThing ??= new Actions();
            actionDelegateThing.AddFunction(name, implementation);
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
            actionDelegateThing ??= new Actions();
            actionDelegateThing.RemoveFunction(name);
        }
    }
}
