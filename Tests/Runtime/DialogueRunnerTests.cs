using System.Collections;
using System.Collections.Generic;
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

            runner.StartDialogue(runner.startNode);
            yield return null;

            Assert.AreEqual("Spieler: Kannst du mich hören? 2", dialogueUI.CurrentLine);
            dialogueUI.ReadyForNextLine();

            Assert.AreEqual("NPC: Klar und deutlich.", dialogueUI.CurrentLine);
            dialogueUI.ReadyForNextLine();

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

            runner.StartDialogue(runner.startNode);
            yield return null;

            Assert.AreEqual("Spieler: Kannst du mich hören? 2", dialogueUI.CurrentLine);
            dialogueUI.ReadyForNextLine();

            Assert.AreEqual("NPC: Klar und deutlich.", dialogueUI.CurrentLine);
            dialogueUI.ReadyForNextLine();

            Assert.AreEqual(2, dialogueUI.CurrentOptions.Count);
            Assert.AreEqual("Mir reicht es.", dialogueUI.CurrentOptions[0]);
            Assert.AreEqual("Nochmal!", dialogueUI.CurrentOptions[1]);
        }


        [TestCase("testCommandInteger DialogueRunner 1 2", "3")]
        [TestCase("testCommandString DialogueRunner a b", "ab")]
        [TestCase("testCommandString DialogueRunner \"a b\" \"c d\"", "a bc d")]
        [TestCase("testCommandGameObject DialogueRunner Sphere", "Sphere")]
        [TestCase("testCommandComponent DialogueRunner Sphere", "Sphere's MeshRenderer")]
        [TestCase("testCommandGameObject DialogueRunner DoesNotExist", "(null)")]
        [TestCase("testCommandComponent DialogueRunner DoesNotExist", "(null)")]
        [TestCase("testCommandNoParameters DialogueRunner", "success")]
        [TestCase("testCommandOptionalParams DialogueRunner 1", "3")]
        [TestCase("testCommandOptionalParams DialogueRunner 1 3", "4")]
        [TestCase("testCommandDefaultName DialogueRunner", "success")]
        [TestCase("testCommandCustomInjector custom", "success")]
        [TestCase("testStaticCommand", "success")]
        [TestCase("testClassWideCustomInjector something", "success")]
        [TestCase("testPrivateStaticCommand", "success")]
        [TestCase("testPrivate something", "success")]
        [TestCase("testCustomParameter Sphere", "Got Sphere")]
        [TestCase("testExternalAssemblyCommand", "success")]
        public void HandleCommand_DispatchesCommands(string test, string expectedLogResult) {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            LogAssert.Expect(LogType.Log, expectedLogResult);
            var methodFound = runner.DispatchCommandToGameObject(test, () => {});

            Assert.AreEqual(methodFound, DialogueRunner.CommandDispatchResult.Success);        
        }

        [UnityTest]
        public IEnumerator HandleCommand_DispatchedCommands_StartCoroutines() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            var framesToWait = 5;

            runner.DispatchCommandToGameObject($"testCommandCoroutine DialogueRunner {framesToWait}", () => {});

            LogAssert.Expect(LogType.Log, $"success {Time.frameCount + framesToWait}");

            // After framesToWait frames, we should have seen the log
            while (framesToWait > 0) {
                framesToWait -= 1;
                yield return null;
            }
        }

        [TestCase("testCommandOptionalParams DialogueRunner", "requires between 1 and 2 parameters, but 0 were provided")]
        [TestCase("testCommandOptionalParams DialogueRunner 1 2 3", "requires between 1 and 2 parameters, but 3 were provided")]
        public void HandleCommand_FailsWhenParameterCountNotCorrect(string command, string error) {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            LogAssert.Expect(LogType.Error, new Regex(error));
            runner.DispatchCommandToGameObject(command, () => {});
        }

        [TestCase("testCommandInteger DialogueRunner 1 not_an_integer", "Can't convert the given parameter")]
        [TestCase("testCommandCustomInjector asdf", "Non-static method requires a target")]
        public void HandleCommand_FailsWhenParameterTypesNotValid(string command, string error) {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            LogAssert.Expect(LogType.Error, new Regex(error));
            runner.DispatchCommandToGameObject(command, () => {});
        }

        [Test]
        public void AddCommandHandler_RegistersCommands() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            runner.AddCommandHandler("test1", () => { Debug.Log("success 1"); } );
            runner.AddCommandHandler("test2", (int val) => { Debug.Log($"success {val}"); } );

            LogAssert.Expect(LogType.Log, "success 1");
            LogAssert.Expect(LogType.Log, "success 2");

            runner.DispatchCommandToRegisteredHandlers("test1", () => {});
            runner.DispatchCommandToRegisteredHandlers("test2 2", () => {});
        }

        [UnityTest]
        public IEnumerator AddCommandHandler_RegistersCoroutineCommands() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

             IEnumerator TestCommandCoroutine(int frameDelay) {
                // Wait the specified number of frames
                while (frameDelay > 0) {
                    frameDelay -= 1;
                    yield return null;
                }
                Debug.Log($"success {Time.frameCount}");
            }

            var framesToWait = 5;

            runner.AddCommandHandler("test", () => runner.StartCoroutine(TestCommandCoroutine(framesToWait)));

            LogAssert.Expect(LogType.Log, $"success {Time.frameCount + framesToWait}");

            runner.DispatchCommandToRegisteredHandlers("test", () => {});

            // After framesToWait frames, we should have seen the log
            while (framesToWait > 0) {
                framesToWait -= 1;
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator VariableStorage_OnExternalChanges_ReturnsExpectedValue() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            DialogueRunnerMockUI dialogueUI = GameObject.FindObjectOfType<DialogueRunnerMockUI>();
            var variableStorage = GameObject.FindObjectOfType<VariableStorageBehaviour>();

            runner.StartDialogue("VariableTest");
            yield return null;

            Assert.AreEqual("Jane: Yes! I've already walked 0 laps!", dialogueUI.CurrentLine);

            variableStorage.SetValue("$laps", 1);
            runner.Stop();
            runner.StartDialogue("VariableTest");
            yield return null;

            Assert.AreEqual("Jane: Yes! I've already walked 1 laps!", dialogueUI.CurrentLine);

            variableStorage.SetValue("$laps", 5);
            runner.Stop();
            runner.StartDialogue("FunctionTest");
            yield return null;

            Assert.AreEqual("Jane: Yes! I've already walked 25 laps!", dialogueUI.CurrentLine);

            runner.Stop();
            runner.StartDialogue("FunctionTest2");
            yield return null;

            Assert.AreEqual("Jane: Yes! I've already walked arg! i am a pirate no you're not! arg! i am a pirate laps!", dialogueUI.CurrentLine);

            runner.Stop();
            runner.StartDialogue("ExternalFunctionTest");
            yield return null;

            Assert.AreEqual("Jane: Here's a function from code that's in another assembly: 42", dialogueUI.CurrentLine);

            runner.Stop();
            runner.StartDialogue("BuiltinsTest");
            yield return null;

            Assert.AreEqual("Jane: round(3.522) = 4; round_places(3.522, 2) = 3.52; floor(3.522) = 3; floor(-3.522) = -4; ceil(3.522) = 4; ceil(-3.522) = -3; inc(3.522) = 4; inc(4) = 5; dec(3.522) = 3; dec(3) = 2; decimal(3.522) = 0.5220001; int(3.522) = 3; int(-3.522) = -3;", dialogueUI.CurrentLine);

            dialogueUI.ReadyForNextLine();
        }   

        [TestCase(@"one two three four", new[] {"one", "two", "three", "four"})]
        [TestCase(@"one ""two three"" four", new[] {"one", "two three", "four"})]
        [TestCase(@"one ""two three four", new[] {"one", "two three four"})]
        [TestCase(@"one ""two \""three"" four", new[] {"one", "two \"three", "four"})]
        [TestCase(@"one \two three four", new[] {"one", "\\two", "three", "four"})]
        [TestCase(@"one ""two \\ three"" four", new[] {"one", "two \\ three", "four"})]
        [TestCase(@"one ""two \1 three"" four", new[] {"one", "two \\1 three", "four"})]
        [TestCase(@"one      two", new[] {"one", "two"})]
        public void SplitCommandText_SplitsTextCorrectly(string input, IEnumerable<string> expectedComponents) 
        {
            IEnumerable<string> parsedComponents = DialogueRunner.SplitCommandText(input);

            Assert.AreEqual(expectedComponents, parsedComponents);
        }
    }
}
