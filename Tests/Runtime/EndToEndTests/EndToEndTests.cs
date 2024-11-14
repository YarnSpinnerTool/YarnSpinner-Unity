/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Tests
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;

#if USE_UNITASK
    using Cysharp.Threading.Tasks;
    using YarnTask = Cysharp.Threading.Tasks.UniTask;
#else
#endif

    using static EndToEndUtility;
    using static RunnerExecution;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class CoroutineHost : MonoBehaviour { }

    [TestFixture]
    public class EndToEndTests : IPrebuildSetup, IPostBuildCleanup
    {
        const string EndToEndTestSceneGUID = "5497df506e7f14781b61ccbccad19db2";


        [AllowNull]
        MonoBehaviour coroutineHost;

        public void Setup()
        {
            RuntimeTestUtility.AddSceneToBuild(EndToEndTestSceneGUID);
        }

        public void Cleanup()
        {
            RuntimeTestUtility.RemoveSceneFromBuild(EndToEndTestSceneGUID);
        }

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("EndToEndTest");
            bool loaded = false;
            SceneManager.sceneLoaded += (index, mode) =>
            {
                loaded = true;
            };

            yield return new WaitUntil(() => loaded);

            var coroutineHostGO = new GameObject("Coroutine Host");
            coroutineHostGO.hideFlags = HideFlags.HideInHierarchy;
            coroutineHost = coroutineHostGO.AddComponent<CoroutineHost>();
        }

        [TearDown]
        public void TearDown()
        {
            GameObject.Destroy(coroutineHost.gameObject);
            coroutineHost = null;
        }

        [UnityTest]
        public IEnumerator Character_CanWalkToMarker() => YarnAsync.ToCoroutine(async () =>
        {
            await MoveToMarkerAsync("Character1", "Character2");
        });

        [UnityTest]
        public IEnumerator NodeGroups_DeliverContent() => YarnAsync.ToCoroutine(async () =>
        {
            await MoveToMarkerAsync("Character1", "Character2");

            await using (var dialogue = StartDialogue("SpeakToGuard"))
            {
                await WaitForLineAsync("Halt, scum!");
                await WaitForLineAsync("None shall pass this point!");
            }

            var variableStorage = GameObject.FindAnyObjectByType<VariableStorageBehaviour>();
            variableStorage.SetValue("$guard_friendly", true);

            await using (var dialogue = StartDialogue("SpeakToGuard"))
            {
                await WaitForLineAsync("Halt, traveller!");
                await WaitForLineAsync("Why, hello there!");
                await WaitForLineAsync("Ah, my friend! You may pass.");
            }
        });

        [UnityTest]
        public IEnumerator EndToEndTest_CanAwaitContent() => YarnAsync.ToCoroutine(async () =>
        {
            await using var dialogue = StartDialogue("LinesAndOptions");

            await WaitForLineAsync("Line 1");
            await WaitForLineAsync("Line 2");
            await WaitForOptionAndSelectAsync("Option 1");
            await WaitForLineAsync("Option 1 Selected");
            await WaitForLineAsync("Line 3");
        });

        [UnityTest]
        public IEnumerator LineGroups_DeliverContent() => YarnAsync.ToCoroutine(() =>
        {
            throw new System.NotImplementedException();
        });

    }
}
