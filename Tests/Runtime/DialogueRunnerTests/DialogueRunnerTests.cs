using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace Yarn.Unity.Tests
{

    [TestFixture]
    public class DialogueRunnerTests: IPrebuildSetup, IPostBuildCleanup
    {
        const string DialogueRunnerTestSceneGUID = "a04d7174042154a47a29ac4f924e0474";
        const string TestResourcesFolderGUID = "be395506411a5a74eb2458a5cf1de710";

        public void Setup()
        {
            RuntimeTestUtility.GenerateRegistrationSource(TestResourcesFolderGUID);
            RuntimeTestUtility.AddSceneToBuild(DialogueRunnerTestSceneGUID);
        }

        public void Cleanup()
        {
            RuntimeTestUtility.CleanupGeneratedSource();
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
        public IEnumerator DialogueRunner_WhenStateSaved_CanRestoreState_PlayerPrefs()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var storage = runner.VariableStorage;

            var testKey = "TemporaryTestingKey";
            runner.StartDialogue("LotsOfVars");
            yield return null;

            var originals = storage.GetAllVariables();

            runner.SaveStateToPlayerPrefs(testKey);
            yield return null;

            bool success = runner.LoadStateFromPlayerPrefs(testKey);
            PlayerPrefs.DeleteKey(testKey);
            Assert.IsTrue(success);

            VerifySaveAndLoadStorageIntegrity(storage, originals.FloatVariables, originals.StringVariables, originals.BoolVariables);
        }
        [UnityTest]
        public IEnumerator DialogueRunner_WhenStateSaved_CanRestoreState()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var storage = runner.VariableStorage;

            var testFile = "TemporaryTestingFile.json";
            runner.StartDialogue("LotsOfVars");
            yield return null;

            var originals = storage.GetAllVariables();

            runner.SaveStateToPersistentStorage(testFile);
            yield return null;

            bool success = runner.LoadStateFromPersistentStorage(testFile);
            Assert.IsTrue(success);

            VerifySaveAndLoadStorageIntegrity(storage, originals.FloatVariables, originals.StringVariables, originals.BoolVariables);

            success = CleanUpSaveFile(testFile);
            Assert.IsTrue(success);
        }
        private bool CleanUpSaveFile(string SaveFile)
        {
            var path = System.IO.Path.Combine(Application.persistentDataPath, SaveFile);

            try
            {
                System.IO.File.Delete(path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save file {path}: {e.Message}");
                return false;
            }
        }
        [UnityTest]
        public IEnumerator DialogueRunner_WhenRestoringInvalidKey_FailsToLoad()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var storage = runner.VariableStorage;

            runner.StartDialogue("LotsOfVars");
            yield return null;

            var originals = storage.GetAllVariables();

            bool success = runner.LoadStateFromPlayerPrefs("invalid key");

            // because the load should have failed this should still be fine
            VerifySaveAndLoadStorageIntegrity(storage, originals.FloatVariables, originals.StringVariables, originals.BoolVariables);

            Assert.IsFalse(success);
        }
        [UnityTest]
        public IEnumerator SaveAndLoad_BadSave()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var storage = runner.VariableStorage;

            runner.StartDialogue("LotsOfVars");
            yield return null;

            var testKey = "TemporaryTestingKey";
            PlayerPrefs.SetString(testKey,"{}");
            
            var originals = storage.GetAllVariables();

            bool success = runner.LoadStateFromPlayerPrefs(testKey);

            // because the load should have failed this should still be fine
            VerifySaveAndLoadStorageIntegrity(storage, originals.FloatVariables, originals.StringVariables, originals.BoolVariables);

            Assert.IsFalse(success);
        }
        
        private void VerifySaveAndLoadStorageIntegrity(VariableStorageBehaviour storage, Dictionary<string, float> testFloats, Dictionary<string, string> testStrings, Dictionary<string, bool> testBools)
        {
            var currentVariables = storage.GetAllVariables();

            VerifySaveAndLoad(currentVariables.FloatVariables, testFloats);
            VerifySaveAndLoad(currentVariables.StringVariables, testStrings);
            VerifySaveAndLoad(currentVariables.BoolVariables, testBools);

            void VerifySaveAndLoad<T>(Dictionary<string, T> current, Dictionary<string, T> original)
            {
                foreach (var pair in current)
                {
                    T originalValue;
                    Assert.IsTrue(original.TryGetValue(pair.Key, out originalValue), "new key is not inside the original set of variables");
                    Assert.AreEqual(originalValue, pair.Value, "values under the same key are different");
                }
            }
        }
        
        [UnityTest]
        public IEnumerator DialogueRunner_CanAccessNodeHeaders()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            // these are all set inside of TestHeadersAreAccessible.yarn
            // which is part of the test scene project
            var allHeaders = new Dictionary<string, Dictionary<string, List<string>>>();
            var headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>(){"EmptyTags"});
            headers.Add("tags", new List<string>() {string.Empty});
            allHeaders.Add("EmptyTags", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() {"ArbitraryHeaderWithValue"});
            headers.Add("arbitraryheader", new List<string>() {"some-arbitrary-text"});
            allHeaders.Add("ArbitraryHeaderWithValue", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>(){"Tags"});
            headers.Add("tags",new List<string>(){"one two three"});
            allHeaders.Add("Tags", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>(){"SingleTagOnly"});
            allHeaders.Add("SingleTagOnly",headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() {"Comments"});
            headers.Add("tags", new List<string>() {"one two three"});
            allHeaders.Add("Comments", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("contains", new List<string>() {"lots"});
            headers.Add("title", new List<string>() {"LotsOfHeaders"});
            headers.Add("this", new List<string>() {"node"});
            headers.Add("of", new List<string>() {string.Empty});
            headers.Add("blank", new List<string>() {string.Empty});
            headers.Add("others", new List<string>() {"are"});
            headers.Add("headers", new List<string>() {""});
            headers.Add("some", new List<string>() {"are"});
            headers.Add("not", new List<string>() {""});
            allHeaders.Add("LotsOfHeaders", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() {"DuplicateHeaders"});
            headers.Add("repeat", new List<string>() {"tag1", "tag2", "tag3"});
            allHeaders.Add("DuplicateHeaders", headers);

            foreach (var headerTestData in allHeaders)
            {
                var yarnHeaders = runner.yarnProject.GetHeaders(headerTestData.Key);

                // its possible we got no headers or more/less headers
                // so we need to check we found all the ones we expected to see
                Assert.AreEqual(headerTestData.Value.Count, yarnHeaders.Count);

                foreach (var pair in headerTestData.Value)
                {
                    // is the lust of strings the same as what the yarn program thinks?
                    // ie do we a value that matches each and every one of our tests?
                    CollectionAssert.AreEquivalent(pair.Value, yarnHeaders[pair.Key]);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DialogueRunner_CanAccessInitialValues()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            // these are derived from the declares and sets inside of DialogueRunnerTest.yarn
            var testDefaults = new Dictionary<string, System.IConvertible>();
            testDefaults.Add("$laps", 0);
            testDefaults.Add("$float", 1);
            testDefaults.Add("$string", "this is a string");
            testDefaults.Add("$bool", true);
            testDefaults.Add("$true", false);

            CollectionAssert.AreEquivalent(runner.yarnProject.InitialValues, testDefaults);

            yield return null;
        }
        [UnityTest]
        public IEnumerator DialogueRunner_CanAccessNodeNames()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            // these are derived from the nodes inside of:
            //   - DialogueTest.yarn
            //   - TestHeadersAreAccessible.yarn
            // which are part of the default test scene's project
            var testNodes = new string[]
            {
                "Start",
                "Exit",
                "VariableTest",
                "FunctionTest",
                "FunctionTest2",
                "ExternalFunctionTest",
                "BuiltinsTest",
                "LotsOfVars",
                "EmptyTags",
                "Tags",
                "ArbitraryHeaderWithValue",
                "Comments",
                "SingleTagOnly",
                "LotsOfHeaders",
                "DuplicateHeaders",
            };

            CollectionAssert.AreEquivalent(runner.yarnProject.NodeNames, testNodes);

            yield return null;
        }

        [UnityTest]
        public IEnumerator HandleLine_OnValidYarnFile_SendCorrectLinesToUI()
        {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            DialogueRunnerMockUI dialogueUI = GameObject.FindObjectOfType<DialogueRunnerMockUI>();

            runner.StartDialogue(runner.startNode);
            yield return null;

            Assert.AreEqual("Spieler: Kannst du mich hören? 2", dialogueUI.CurrentLine);
            dialogueUI.Advance();

            Assert.AreEqual("NPC: Klar und deutlich.", dialogueUI.CurrentLine);
            dialogueUI.Advance();

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
            dialogueUI.Advance();

            Assert.AreEqual("NPC: Klar und deutlich.", dialogueUI.CurrentLine);
            dialogueUI.Advance();

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
        [TestCase("testStaticCommand", "success")]
        [TestCase("testExternalAssemblyCommand", "success")]
        public void HandleCommand_DispatchesCommands(string test, string expectedLogResult) {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var dispatcher = runner.CommandDispatcher;

            LogAssert.Expect(LogType.Log, expectedLogResult);
            var result = dispatcher.DispatchCommand(test, out var commandCoroutine);
            
            Assert.AreEqual(CommandDispatchResult.StatusType.SucceededSync, result.Status);
            Assert.IsNull(commandCoroutine);
        }

        [UnityTest]
        public IEnumerator HandleCommand_DispatchedCommands_StartCoroutines() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var dispatcher = runner.CommandDispatcher;

            var framesToWait = 5;

            var result = dispatcher.DispatchCommand($"testCommandCoroutine DialogueRunner {framesToWait}", out var commandCoroutine);

            Assert.AreEqual(CommandDispatchResult.StatusType.SucceededAsync, result.Status);
            Assert.IsNotNull(commandCoroutine);

            // commandCoroutine will already be running on runner, so now we wait for it

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
            var dispatcher = runner.CommandDispatcher;
            var regex = new Regex(error);

            var result = dispatcher.DispatchCommand(command, out _);

            Assert.AreEqual(CommandDispatchResult.StatusType.InvalidParameterCount, result.Status);
            Assert.That(regex.IsMatch(result.Message));
        }

        [TestCase("testCommandInteger DialogueRunner 1 not_an_integer", "Can't convert the given parameter")]
        public void HandleCommand_FailsWhenParameterTypesNotValid(string command, string error) {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var dispatcher = runner.CommandDispatcher;
            var regex = new Regex(error);

            var result = dispatcher.DispatchCommand(command, out _);
            Assert.AreEqual(CommandDispatchResult.StatusType.InvalidParameterCount, result.Status);
            Assert.That(regex.IsMatch(result.Message));
        }

        [Test]
        public void AddCommandHandler_RegistersCommands() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var dispatcher = runner.CommandDispatcher;

            runner.AddCommandHandler("test1", () => { Debug.Log("success 1"); } );
            runner.AddCommandHandler("test2", (int val) => { Debug.Log($"success {val}"); } );

            LogAssert.Expect(LogType.Log, "success 1");
            LogAssert.Expect(LogType.Log, "success 2");

            var result1 = dispatcher.DispatchCommand("test1", out _);
            var result2 = dispatcher.DispatchCommand("test2 2", out _);

            Assert.IsNull(result1.Message);
            Assert.IsNull(result2.Message);
            Assert.AreEqual(result1.Status, CommandDispatchResult.StatusType.SucceededSync, "test1 should succeed synchronously");
            Assert.AreEqual(result1.Status, CommandDispatchResult.StatusType.SucceededSync, "test2 should succeed synchronously");
        }

        [UnityTest]
        public IEnumerator AddCommandHandler_RegistersCoroutineCommands() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();
            var dispatcher = runner.CommandDispatcher;

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

            dispatcher.DispatchCommand("test", out var coroutine);

            Assert.IsNotNull(coroutine);

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

            // dialogueUI.ReadyForNextLine();
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

        [UnityTest]
        public IEnumerator DialogueRunner_OnDialogueStartAndStop_CallsEvents() {
            var runner = GameObject.FindObjectOfType<DialogueRunner>();

            runner.onDialogueStart.AddListener(() =>
            {
                Debug.Log("Dialogue start");
            });

            runner.onDialogueComplete.AddListener(() =>
            {
                Debug.Log("Dialogue complete");
            });

            LogAssert.Expect(LogType.Log, "Dialogue start");
            LogAssert.Expect(LogType.Log, "Dialogue complete");

            runner.StartDialogue(runner.startNode);

            yield return new WaitForSeconds(0.5f);

            runner.Stop();
        }
    }
}
