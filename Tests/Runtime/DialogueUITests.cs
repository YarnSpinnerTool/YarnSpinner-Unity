using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Yarn.Unity;

namespace Yarn.Unity.Tests
{
    public class DialogueUITests : IPrebuildSetup, IPostBuildCleanup
    {
        public void Setup()
        {
            RuntimeTestUtility.AddSceneToBuild(DialogueUITestsSceneGUID);
        }

        public void Cleanup()
        {
            RuntimeTestUtility.RemoveSceneFromBuild(DialogueUITestsSceneGUID);
        }

        const string DialogueUITestsSceneGUID = "6ddb3fe00f2d33e4e982dd435382ea97";

        // Getters for the various components in the scene that we're
        // working with
        DialogueRunner Runner => GameObject.FindObjectOfType<DialogueRunner>();
        DialogueUI UI => GameObject.FindObjectOfType<DialogueUI>();
        Text TextCanvas => UI.dialogueContainer.transform.GetComponentsInChildren<Text>()
                                                         .First(element => element.gameObject.name == "Text");

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("DialogueUITests");
            bool loaded = false;
            SceneManager.sceneLoaded += (index, mode) =>
            {
                loaded = true;
            };
            yield return new WaitUntil(() => loaded);
        }

        [UnityTest]
        public IEnumerator RunLine_OnValidYarnLine_ShowCorrectText()
        {
            // Arrange
            Runner.StartDialogue();
            float startTime;
            startTime = Time.time;
            while (Time.time - startTime < 10 && !string.Equals(TextCanvas.text, "Spieler: Kannst du mich hören? 2"))
            {
                yield return null;
            }

            Assert.AreEqual("Spieler: Kannst du mich hören? 2", TextCanvas.text);

            // Arrange for second line
            yield return null;
            yield return null;
            UI.MarkLineComplete();

            startTime = Time.time;
            while (Time.time - startTime < 10 && !string.Equals(TextCanvas.text, "NPC: Klar und deutlich."))
            {
                yield return null;
            }

            Assert.AreEqual("NPC: Klar und deutlich.", TextCanvas.text);

            // Cleanup
            yield return null;
            UI.MarkLineComplete();
            yield return null;
            UI.SelectOption(0);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RunLine_OnValidYarnLine_CanHideCharacterName()
        {
            // Arrange
            UI.showCharacterName = false;

            Runner.StartDialogue();
            float startTime;
            startTime = Time.time;
            while (Time.time - startTime < 10 && !string.Equals(TextCanvas.text, "Kannst du mich hören? 2"))
            {
                yield return null;
            }

            // Character name in this line ("Spieler: ") should be removed
            Assert.That(string.Equals(TextCanvas.text, "Kannst du mich hören? 2"));
        }

        [UnityTest]
        public IEnumerator RunLine_OnValidYarnLine_LinesCanBeInterrupted()
        {

            Runner.StartDialogue();

            var expectedFinalText = "Spieler: Kannst du mich hören? 2";

            // Wait a few frames - enough for there to be some text on
            // screen, but not all of it
            var waitFrameCount = 3;
            while (waitFrameCount > 0)
            {
                yield return null;
                waitFrameCount -= 1;
            }

            Assert.AreNotEqual(expectedFinalText, TextCanvas.text, "Dialogue view should not yet have delivered all of the text.");

            // Signal an interruption
            UI.MarkLineComplete();
            yield return null;

            Assert.AreEqual(expectedFinalText, TextCanvas.text, "Dialogue view should be displaying all text after interruption.");

        }
    }
}
