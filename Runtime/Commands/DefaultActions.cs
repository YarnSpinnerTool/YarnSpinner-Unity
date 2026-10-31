/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    static class DefaultActions
    {
        #region Commands
        /// <summary>
        /// Yarn Spinner defines two built-in commands: "wait", and "stop".
        /// Stop is defined inside the Virtual Machine (the compiler traps it
        /// and makes it a special case.) Wait is defined here in Unity.
        /// </summary>

        [RuntimeInitializeOnLoadMethod]
        static void RegisterDefaultActions()
        {
            ActionInvoker.RegisterCommandHandler(CanHandleCommand, InvokeCommand);
        }

        private static YarnTask InvokeCommand(Command command, LineCancellationToken token)
        {
            var parts = new List<string>(DialogueRunner.SplitCommandText(command.Text));
            var commandName = parts[0];

            if (commandName != "wait") { throw new ArgumentException($"Can't handle command {commandName}"); }

            if (parts.Count != 2) { throw new ArgumentNullException("Can't wait: not enough parameters"); }

            if (float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var durationSeconds) == false || durationSeconds < 0)
            {
                throw new ArgumentException($"Can't wait: invalid parameter '{parts[1]}'");
            }

            var delayDuration = TimeSpan.FromSeconds(durationSeconds);
            return YarnTask.Delay(delayDuration, token.HurryUpToken);
        }

        private static bool CanHandleCommand(string commandName)
        {
            return commandName switch
            {
                "wait" => true,
                _ => false
            };
        }
        #endregion
    }
}
