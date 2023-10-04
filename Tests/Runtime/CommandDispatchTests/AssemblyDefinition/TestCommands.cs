namespace Yarn.Unity.Tests
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Yarn.Unity;

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
