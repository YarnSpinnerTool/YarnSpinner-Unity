/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Tests
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Yarn.Unity;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class TestCommands : MonoBehaviour
    {
        [YarnFunction("testExternalAssemblyFunction")]
        public static int CoolFunction()
        {
            return 42;
        }

        [YarnCommand("testExternalAssemblyCommand")]
        public static void CoolCommand()
        {
            Debug.Log($"success");
        }
    }
}
