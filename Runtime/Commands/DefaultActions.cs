/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;

namespace Yarn.Unity
{
    public static class DefaultActions
    {
        #region Commands
        /// <summary>
        /// Yarn Spinner defines two built-in commands: "wait", and "stop".
        /// Stop is defined inside the Virtual Machine (the compiler traps it
        /// and makes it a special case.) Wait is defined here in Unity.
        /// </summary>
        /// <param name="duration">How long to wait, in seconds.</param>
        [YarnCommand("wait")]
        public static YarnTask Wait(float duration, LineCancellationToken token)
        {
            int milliseconds = (int)(duration * 1000);
            return YarnTask.Delay(milliseconds, token.HurryUpToken);
        }
        #endregion
    }
}
