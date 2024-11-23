/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Tests
{
    using NUnit.Framework;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
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
        public IEnumerator Character_CanWalkToMarker() => YarnTask.ToCoroutine(async () =>
        {
            await MoveToMarkerAsync("Character1", "Character2");
        });

        [UnityTest]
        public IEnumerator NodeGroups_DeliverContent() => YarnTask.ToCoroutine(async () =>
        {
            await MoveToMarkerAsync("Character1", "Character2");

            await StartDialogue("SpeakToGuard", async () =>
            {
                await WaitForLineAsync("Halt, scum!");
                await WaitForLineAsync("None shall pass this point!");
            });

            var variableStorage = GameObject.FindAnyObjectByType<VariableStorageBehaviour>();
            variableStorage.SetValue("$guard_friendly", true);

            await StartDialogue("SpeakToGuard", async () =>
            {
                await WaitForLineAsync("Halt, traveller!");
                await WaitForLineAsync("Why, hello there!");
                await WaitForLineAsync("Ah, my friend! You may pass.");
            });
        });

        [UnityTest]
        public IEnumerator EndToEndTest_CanAwaitContent() => YarnTask.ToCoroutine(async () =>
        {
            await StartDialogue("LinesAndOptions", async () =>
            {
                await WaitForLineAsync("Line 1");
                await WaitForLineAsync("Line 2");
                await WaitForOptionAndSelectAsync("Option 1");
                await WaitForLineAsync("Option 1 Selected");
                await WaitForLineAsync("Line 3");
            });

            bool didTimeout = false;

            try
            {
                await StartDialogue("LinesAndOptions", async () =>
                {
                    await WaitForLineAsync("Line 1");
                    // Don't progress any further; dialogue should time out
                }, 100);
            }
            catch (System.TimeoutException)
            {
                didTimeout = true;
            }

            didTimeout.Should().BeTrue("The task is expected to time out");
        });

        [UnityTest]
        public IEnumerator LineGroups_DeliverContent() => YarnTask.ToCoroutine(async () =>
        {
            // Repeatedly run the node LineGroups; we should see different
            // content each time, depending on the state of the variable storage

            // Use the 'best least recently viewed' strategy, which shows sorts
            // by complexity (descending), view count (ascending), and order in
            // the file (ascending). This means that we should see these lines
            // in this exact order every time.

            var runner = GameObject.FindAnyObjectByType<DialogueRunner>();
            runner.Dialogue.ContentSaliencyStrategy = new Yarn.Saliency.BestLeastRecentlyViewedSalienceStrategy(runner.VariableStorage);

            await StartDialogue("LineGroups", async () => { await WaitForLineAsync("I've got to stay alert!"); });
            await StartDialogue("LineGroups", async () => { await WaitForLineAsync("Halt!"); });

            await StartDialogue("LineGroups", async () => { await WaitForLineAsync("Stop right there!"); });
            await StartDialogue("LineGroups", async () => { await WaitForLineAsync("No entry!"); });
            await StartDialogue("LineGroups", async () => { await WaitForLineAsync("Halt!"); }); // loop back

            runner.VariableStorage.SetValue("$is_criminal", true);

            await StartDialogue("LineGroups", async () => { await WaitForLineAsync("Stop right there, you criminal scum!"); });
            await StartDialogue("LineGroups", async () => { await WaitForLineAsync("Halt, you brigand!"); });
            await StartDialogue("LineGroups", async () => { await WaitForLineAsync("Thief! Stop right there!"); });

            runner.VariableStorage.SetValue("$helped_king", true);
            await StartDialogue("LineGroups", async () => { await WaitForLineAsync("I hear the king has a new advisor."); }); // only appears once

            await StartDialogue("LineGroups", async () => { await WaitForLineAsync("Stop right there, you criminal scum!"); });
        });

        [UnityTest]
        public IEnumerator OnceStatement_DeliversLinesOnce() => YarnTask.ToCoroutine(async () =>
        {

            await StartDialogue("Once", async () =>
            {
                // On the first run, we should see first-run content.
                await WaitForLineAsync("Who are you?");
                await WaitForLineAsync("I've never seen you before.");
                await WaitForLineAsync("I'm new!");
            });

            await StartDialogue("Once", async () =>
            {
                // On all subsequent second runs, we should see different content.
                await WaitForLineAsync("Ah, it's you.");
            });

            await StartDialogue("Once", async () =>
            {
                // On all subsequent second runs, we should see different content.
                await WaitForLineAsync("Ah, it's you.");
            });
        });

        [UnityTest]
        public IEnumerator Detours_DeliverLinesFromOtherNodeAndReturn() => YarnTask.ToCoroutine(async () =>
        {
            var runner = GameObject.FindAnyObjectByType<Yarn.Unity.DialogueRunner>();


            await StartDialogue("Detours", async () =>
            {
                // We start in the 'Detours' node
                await WaitForLineAsync("Have I told you my backstory?");
                await WaitForOptionAndSelectAsync("No?");

                // Selecting this option detours us to the 'Guard_Backstory'
                // node
                await WaitForLineAsync("It all started when I was a new recruit.");
                await WaitForLineAsync("Why, I was but a young whippersnapper.");
                await WaitForLineAsync("My backstory is so long.");
                await WaitForLineAsync("Want to hear more?");
                await WaitForOptionAndSelectAsync("Yes.");
                await WaitForLineAsync("Great! After I graduated...");
                await WaitForLineAsync("(I'm going to stop now.)");

                // After the node finishes it returns to 'Detours', and then ends.
                await WaitForLineAsync("Anyway, move along.");
            });
        });

        [UnityTest]
        public IEnumerator SmartVariables_UpdateValueFromStoredVariables() => YarnTask.ToCoroutine(async () =>
        {
            var storage = GameObject.FindAnyObjectByType<Yarn.Unity.VariableStorageBehaviour>();

            // Different lines run depending on whether the player has money or not
            storage.SetValue("$player_money", 5);
            await StartDialogue("SmartVariables", async () =>
            {
                await WaitForLineAsync("Can I have a pie?");
                await WaitForLineAsync("You can't afford it!");
            });

            storage.SetValue("$player_money", 11);
            await StartDialogue("SmartVariables", async () =>
            {
                await WaitForLineAsync("One pie, please.");
                await WaitForLineAsync("Certainly!");
            });
        });
    }
}
