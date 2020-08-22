using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;

#if UNITY_EDITOR
#endif

namespace Yarn.Unity.Tests
{

    [TestFixture]
    public class DialogueRunnerTests: IPrebuildSetup, IPostBuildCleanup
    {
        const string DialogueRunnerTestSceneGUID = "a04d7174042154a47a29ac4f924e0474";

        public void Setup()
        {
            RuntimeTestUtility.AddSceneToBuild(DialogueRunnerTestSceneGUID);
        }

        public void Cleanup()
        {
            RuntimeTestUtility.RemoveSceneFromBuild(DialogueRunnerTestSceneGUID);
        }

        [UnitySetUp]
        public IEnumerator LoadScene() {
            SceneManager.LoadScene("DialogueRunnerTest");
            bool loaded = false;
            SceneManager.sceneLoaded += (index, mode) =>
            {
                loaded = true;
            };
            yield return new WaitUntil(() => loaded);
        }

        [UnityTest]
        public IEnumerator HandleLine_OnValidYarnFile_SendCorrectLinesToUI()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            DialogueRunnerMockUI dialogueUI = GameObject.FindObjectOfType<DialogueRunnerMockUI>();

            runner.StartDialogue();
            yield return null;

            Assert.That(string.Equals(dialogueUI.CurrentLine, "Spieler: Kannst du mich hören?"));
            dialogueUI.MarkLineComplete();

            Assert.That(string.Equals(dialogueUI.CurrentLine, "NPC: Klar und deutlich."));
            dialogueUI.MarkLineComplete();

            Assert.AreEqual(2, dialogueUI.CurrentOptions.Count);
            Assert.AreEqual("Mir reicht es.", dialogueUI.CurrentOptions[0]);
            Assert.AreEqual("Nochmal!", dialogueUI.CurrentOptions[1]);
        }

        [UnityTest]
        public IEnumerator HandleLine_OnViewsArrayContainingNullElement_SendCorrectLinesToUI()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            DialogueRunnerMockUI dialogueUI = GameObject.FindObjectOfType<DialogueRunnerMockUI>();

            // Insert a null element into the dialogue views array
            var viewArrayWithNullElement = runner.dialogueViews.ToList();
            viewArrayWithNullElement.Add(null);
            runner.dialogueViews = viewArrayWithNullElement.ToArray();

            runner.StartDialogue();
            yield return null;

            Assert.That(string.Equals(dialogueUI.CurrentLine, "Spieler: Kannst du mich hören?"));
            dialogueUI.MarkLineComplete();

            Assert.That(string.Equals(dialogueUI.CurrentLine, "NPC: Klar und deutlich."));
            dialogueUI.MarkLineComplete();

            Assert.AreEqual(2, dialogueUI.CurrentOptions.Count);
            Assert.AreEqual("Mir reicht es.", dialogueUI.CurrentOptions[0]);
            Assert.AreEqual("Nochmal!", dialogueUI.CurrentOptions[1]);
        }


        [TestCase("testCommandInteger DialogueRunner 1 2", "3")]
        [TestCase("testCommandString DialogueRunner a b", "ab")]
        [TestCase("testCommandGameObject DialogueRunner Sphere", "Sphere")]
        [TestCase("testCommandComponent DialogueRunner Sphere", "Sphere's MeshRenderer")]
        [TestCase("testCommandGameObject DialogueRunner DoesNotExist", "(null)")]
        [TestCase("testCommandComponent DialogueRunner DoesNotExist", "(null)")]
        [TestCase("testCommandNoParameters DialogueRunner", "success")]
        [TestCase("testCommandOptionalParams DialogueRunner 1", "3")]
        [TestCase("testCommandOptionalParams DialogueRunner 1 3", "4")]
        public void HandleCommand_DispatchesCommands(string test, string expectedLogResult) {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            LogAssert.Expect(LogType.Log, expectedLogResult);
            var (methodFound, executionType) = runner.DispatchCommandToGameObject(test);

            Assert.True(methodFound);
            Assert.AreEqual(Dialogue.HandlerExecutionType.ContinueExecution, executionType);
        
        }

        [UnityTest]
        public IEnumerator HandleCommand_DispatchedCommands_StartCoroutines() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            var currentFrameNumber = Time.frameCount;

            var framesToWait = 5;

            runner.DispatchCommandToGameObject($"testCommandCoroutine DialogueRunner {framesToWait}");

            LogAssert.Expect(LogType.Log, $"success {currentFrameNumber + framesToWait}");

            // After framesToWait frames, we should have seen the log
            while (framesToWait > 0) {
                framesToWait -= 1;
                yield return null;
            }
        }

        [Test]
        public void HandleCommand_FailsWhenParameterCountNotCorrect() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            LogAssert.Expect(LogType.Error, new Regex("requires between 1 and 2 parameters, but 0 were provided"));
            runner.DispatchCommandToGameObject("testCommandOptionalParams DialogueRunner");

            LogAssert.Expect(LogType.Error, new Regex("requires between 1 and 2 parameters, but 3 were provided"));
            runner.DispatchCommandToGameObject("testCommandOptionalParams DialogueRunner 1 2 3");
        }

        [Test]
        public void HandleCommand_FailsWhenParameterTypesNotValid() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            LogAssert.Expect(LogType.Error, new Regex("can't convert parameter"));
            runner.DispatchCommandToGameObject("testCommandInteger DialogueRunner 1 not_an_integer");
        }
    }
}
