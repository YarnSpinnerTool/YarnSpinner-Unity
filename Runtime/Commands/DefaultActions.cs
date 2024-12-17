/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal class DemoAction
{
    public static async System.Threading.Tasks.Task DemoCommandAsync()
    {
        await System.Threading.Tasks.Task.Delay(1000);
    }
}

namespace Yarn.Unity
{
    internal class DefaultActions : MonoBehaviour
    {
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void AddRegisterFunction()
        {
            // When the domain is reloaded, scripts are recompiled, or the game
            // starts, add RegisterActions as a method that populates a
            // DialogueRunner or Library with commands and functions.
            Actions.AddRegistrationMethod(RegisterActions);
        }

        public static void RegisterActions(IActionRegistration target, RegistrationType registrationType)
        {
            // Register the built-in methods and commands from Yarn Spinner for Unity.
            target.AddCommandHandler<float>("wait", Wait);
        }

        #region Commands
        /// <summary>
        /// Yarn Spinner defines two built-in commands: "wait", and "stop".
        /// Stop is defined inside the Virtual Machine (the compiler traps it
        /// and makes it a special case.) Wait is defined here in Unity.
        /// </summary>
        /// <param name="duration">How long to wait, in seconds.</param>
        [YarnCommand("wait")]
        public static IEnumerator Wait(float duration)
        {
            yield return new WaitForSeconds(duration);
        }
        #endregion
    }
}
