using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    internal class DefaultActions : MonoBehaviour
    {
        #region Commands
        /// <summary>
        /// Yarn Spinner defines two built-in commands: "wait", and "stop".
        /// Stop is defined inside the Virtual Machine (the compiler traps it
        /// and makes it a special case.) Wait is defined here in Unity.
        /// </summary>
        /// <param name="duration">How long to wait.</param>
        [YarnCommand]
        public static IEnumerator wait(float duration)
        {
            yield return new WaitForSeconds(duration);
        }
        #endregion
    }
}
