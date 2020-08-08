using System.Collections;
using System.Linq;
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

        [UnityTest]
        public IEnumerator HandleLine_OnValidYarnFile_SendCorrectLinesToUI()
        {
            SceneManager.LoadScene("DialogueRunnerTest");
            bool loaded = false;
            SceneManager.sceneLoaded += (index, mode) =>
            {
                loaded = true;
            };
            yield return new WaitUntil(() => loaded);

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
            SceneManager.LoadScene("DialogueRunnerTest");
            bool loaded = false;
            SceneManager.sceneLoaded += (index, mode) =>
            {
                loaded = true;
            };
            yield return new WaitUntil(() => loaded);

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
    }
}
