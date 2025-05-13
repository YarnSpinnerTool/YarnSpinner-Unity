/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Tests
{
    using NUnit.Framework;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    [TestFixture]
    public class DialogueRunnerTests : IPrebuildSetup, IPostBuildCleanup
    {
        const string DialogueRunnerTestSceneGUID = "a04d7174042154a47a29ac4f924e0474";
        const string TestResourcesFolderGUID = "be395506411a5a74eb2458a5cf1de710";

        public void Setup()
        {
            RuntimeTestUtility.AddSceneToBuild(DialogueRunnerTestSceneGUID);
        }

        public void Cleanup()
        {
            RuntimeTestUtility.RemoveSceneFromBuild(DialogueRunnerTestSceneGUID);
        }

        [AllowNull]
        private DialogueRunner runner;
        [AllowNull]
        private DialogueRunnerMockUI dialogueUI;
        [AllowNull]
        private YarnProject yarnProject;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            SceneManager.LoadScene("DialogueRunnerTest");
            bool loaded = false;
            SceneManager.sceneLoaded += (index, mode) =>
            {
                loaded = true;
            };

            yield return new WaitUntil(() => loaded);

            runner = UnityEngine.Object.FindAnyObjectByType<DialogueRunner>();
            dialogueUI = UnityEngine.Object.FindAnyObjectByType<DialogueRunnerMockUI>();

            runner.Should().NotBeNull();
            dialogueUI.Should().NotBeNull();

            yarnProject = runner.yarnProject!;
            yarnProject.Should().NotBeNull();
        }

        [UnityTest]
        public IEnumerator DialogueRunner_WhenStateSaved_CanRestoreState()
        {
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
            var storage = runner.VariableStorage;

            runner.StartDialogue("LotsOfVars");
            yield return null;

            var originals = storage.GetAllVariables();

            LogAssert.Expect(LogType.Error, new Regex("Failed to load save state"));
            bool success = runner.LoadStateFromPersistentStorage("invalid key");

            // because the load should have failed this should still be fine
            VerifySaveAndLoadStorageIntegrity(storage, originals.FloatVariables, originals.StringVariables, originals.BoolVariables);

            Assert.IsFalse(success);
        }
        [UnityTest]
        public IEnumerator SaveAndLoad_WhenLoadingInvalidSave_FailsToLoad()
        {
            var storage = runner.VariableStorage;

            runner.StartDialogue("LotsOfVars");
            yield return null;

            var testKey = "TemporaryTestingKey";
            PlayerPrefs.SetString(testKey, "{}");

            var originals = storage.GetAllVariables();

            LogAssert.Expect(LogType.Error, new Regex("Failed to load save state"));
            bool success = runner.LoadStateFromPersistentStorage(testKey);

            success.Should().BeFalse();

            // because the load should have failed this should still be fine
            VerifySaveAndLoadStorageIntegrity(storage, originals.FloatVariables, originals.StringVariables, originals.BoolVariables);

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
                    var exists = original.TryGetValue(pair.Key, out T originalValue);

                    Assert.IsTrue(exists, "new key is not inside the original set of variables");
                    Assert.AreEqual(originalValue, pair.Value, "values under the same key are different");
                }
            }
        }

        [UnityTest]
        public IEnumerator DialogueRunner_CanAccessNodeHeaders()
        {
            // these are all set inside of TestHeadersAreAccessible.yarn
            // which is part of the test scene project
            var allHeaders = new Dictionary<string, Dictionary<string, List<string>>>();
            var headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() { "EmptyTags" });
            headers.Add("tags", new List<string>() { string.Empty });
            allHeaders.Add("EmptyTags", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() { "ArbitraryHeaderWithValue" });
            headers.Add("arbitraryheader", new List<string>() { "some-arbitrary-text" });
            allHeaders.Add("ArbitraryHeaderWithValue", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() { "Tags" });
            headers.Add("tags", new List<string>() { "one two three" });
            allHeaders.Add("Tags", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() { "SingleTagOnly" });
            allHeaders.Add("SingleTagOnly", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() { "Comments" });
            headers.Add("tags", new List<string>() { "one two three" });
            allHeaders.Add("Comments", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("contains", new List<string>() { "lots" });
            headers.Add("title", new List<string>() { "LotsOfHeaders" });
            headers.Add("this", new List<string>() { "node" });
            headers.Add("of", new List<string>() { string.Empty });
            headers.Add("blank", new List<string>() { string.Empty });
            headers.Add("others", new List<string>() { "are" });
            headers.Add("headers", new List<string>() { "" });
            headers.Add("some", new List<string>() { "are" });
            headers.Add("not", new List<string>() { "" });
            allHeaders.Add("LotsOfHeaders", headers);
            headers = new Dictionary<string, List<string>>();

            headers.Add("title", new List<string>() { "DuplicateHeaders" });
            headers.Add("repeat", new List<string>() { "tag1", "tag2", "tag3" });
            allHeaders.Add("DuplicateHeaders", headers);

            foreach (var headerTestData in allHeaders)
            {
                var expectedHeaders = headerTestData.Value.SelectMany(h => h.Value.Select(v => KeyValuePair.Create(h.Key, v)));
                var yarnHeaders = this.runner.Dialogue.GetHeaders(headerTestData.Key);

                // its possible we got no headers or more/less headers
                // so we need to check we found all the ones we expected to see
                yarnHeaders.Count().Should().BeEqualTo(expectedHeaders.Count(), headerTestData.ToString());

                foreach (var pair in headerTestData.Value)
                {
                    // is the lust of strings the same as what the yarn program thinks?
                    // ie do we a value that matches each and every one of our tests?
                    yarnHeaders.Should().Contain(h => h.Key == pair.Key && pair.Value.Contains(h.Value));
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DialogueRunner_CanAccessInitialValues()
        {


            // these are derived from the declares and sets inside of DialogueRunnerTest.yarn
            var testDefaults = new Dictionary<string, System.IConvertible>();
            testDefaults.Add("$float", 1);
            testDefaults.Add("$string", "this is a string");
            testDefaults.Add("$bool", true);
            testDefaults.Add("$true", false);
            testDefaults.Add("$nodeGroupCondition1", false);
            testDefaults.Add("$nodeGroupCondition2", false);

            foreach (var testDefault in testDefaults)
            {
                yarnProject.InitialValues.Should().ContainKey(testDefault.Key, $"initial values should include {testDefault.Key}");
                var value = yarnProject.InitialValues[testDefault.Key];
                value.ToString().Should().BeEqualTo(testDefault.Value.ToString(), $"initial value of {testDefault.Key} should be {testDefault.Value}");
            }

            yield return null;
        }
        [UnityTest]
        public IEnumerator DialogueRunner_CanAccessNodeNames()
        {


            // these are derived from the nodes inside of:
            //   - DialogueTest.yarn
            //   - TestHeadersAreAccessible.yarn
            // which are part of the default test scene's project
            var testNodes = new string[]
            {
                "Start",
                "LotsOfVars",
                "EmptyTags",
                "Tags",
                "ArbitraryHeaderWithValue",
                "Comments",
                "SingleTagOnly",
                "LotsOfHeaders",
                "DuplicateHeaders",
            };

            yarnProject.Should().NotBeNull();

            yarnProject!.NodeNames.Should().ContainAllOf(testNodes);

            yield return null;
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
        public void HandleCommand_DispatchesCommands(string test, string expectedLogResult)
        {

            var dispatcher = runner.CommandDispatcher;

            LogAssert.Expect(LogType.Log, expectedLogResult);
            var result = dispatcher.DispatchCommand(test, runner);

            Assert.AreEqual(CommandDispatchResult.StatusType.Succeeded, result.Status);
            Assert.IsTrue(result.Task.IsCompleted());
        }

        [UnityTest]
        public IEnumerator HandleCommand_DispatchedCommands_StartCoroutines()
        {

            var dispatcher = runner.CommandDispatcher;

            var framesToWait = 5;

            var result = dispatcher.DispatchCommand($"testCommandCoroutine DialogueRunner {framesToWait}", runner);

            Assert.AreEqual(CommandDispatchResult.StatusType.Succeeded, result.Status);
            Assert.IsFalse(result.Task.IsCompleted());

            // commandCoroutine will already be running on runner, so now we wait for it

            LogAssert.Expect(LogType.Log, $"success {Time.frameCount + framesToWait}");

            // After framesToWait frames, we should have seen the log
            while (framesToWait > 0)
            {
                framesToWait -= 1;
                yield return null;
            }
        }

        [TestCase("testCommandOptionalParams DialogueRunner", "requires between 1 and 2 parameters, but 0 were provided")]
        [TestCase("testCommandOptionalParams DialogueRunner 1 2 3", "requires between 1 and 2 parameters, but 3 were provided")]
        public void HandleCommand_FailsWhenParameterCountNotCorrect(string command, string error)
        {

            var dispatcher = runner.CommandDispatcher;
            var regex = new Regex(error);

            var result = dispatcher.DispatchCommand(command, runner);

            Assert.AreEqual(CommandDispatchResult.StatusType.InvalidParameterCount, result.Status);
            Assert.That(regex.IsMatch(result.Message));
        }

        [TestCase("testCommandInteger DialogueRunner 1 not_an_integer", "Can't convert the given parameter")]
        public void HandleCommand_FailsWhenParameterTypesNotValid(string command, string error)
        {

            var dispatcher = runner.CommandDispatcher;
            var regex = new Regex(error);

            var result = dispatcher.DispatchCommand(command, runner);
            Assert.AreEqual(CommandDispatchResult.StatusType.InvalidParameter, result.Status);
            Assert.That(regex.IsMatch(result.Message));
        }

        [TestCase("testInstanceVariadic DialogueRunner 1", "Variadic instance function: 1, ()")]
        [TestCase("testInstanceVariadic DialogueRunner 1 true", "Variadic instance function: 1, (True)")]
        [TestCase("testInstanceVariadic DialogueRunner 1 true false", "Variadic instance function: 1, (True, False)")]
        [TestCase("testStaticVariadic 1", "Variadic static function: 1, ()")]
        [TestCase("testStaticVariadic 1 true", "Variadic static function: 1, (True)")]
        [TestCase("testStaticVariadic 1 true false", "Variadic static function: 1, (True, False)")]
        public void HandleCommand_DispatchesCommandsWithVariadicParameters(string command, string expectedLog)
        {
            var dispatcher = runner.CommandDispatcher;

            LogAssert.Expect(LogType.Log, expectedLog);

            var result = dispatcher.DispatchCommand(command, runner);

            Assert.AreEqual(CommandDispatchResult.StatusType.Succeeded, result.Status);
        }
        [TestCase("testInstanceVariadic DialogueRunner 1 one")]
        [TestCase("testInstanceVariadic DialogueRunner 1 true too")]
        [TestCase("testStaticVariadic 1 one")]
        [TestCase("testStaticVariadic 1 true too")]
        public void HandleCommand_InvalidVariadicParameters_ShouldFail(string command)
        {
            var dispatcher = runner.CommandDispatcher;

            var result = dispatcher.DispatchCommand(command, runner);

            Assert.AreEqual(CommandDispatchResult.StatusType.InvalidParameter, result.Status);
        }

        [Test]
        public void AddCommandHandler_RegistersCommands()
        {

            var dispatcher = runner.CommandDispatcher;

            runner.AddCommandHandler("test1", () => { Debug.Log("success 1"); });
            runner.AddCommandHandler("test2", (int val) => { Debug.Log($"success {val}"); });

            LogAssert.Expect(LogType.Log, "success 1");
            LogAssert.Expect(LogType.Log, "success 2");

            var result1 = dispatcher.DispatchCommand("test1", runner);
            var result2 = dispatcher.DispatchCommand("test2 2", runner);

            Assert.IsNull(result1.Message);
            Assert.IsNull(result2.Message);
            Assert.AreEqual(result1.Status, CommandDispatchResult.StatusType.Succeeded, "test1 should succeed synchronously");
            Assert.AreEqual(result1.Status, CommandDispatchResult.StatusType.Succeeded, "test2 should succeed synchronously");
        }

        [UnityTest]
        public IEnumerator AddCommandHandler_RegistersCoroutineCommands()
        {

            var dispatcher = runner.CommandDispatcher;

            IEnumerator TestCommandCoroutine(int frameDelay)
            {
                // Wait the specified number of frames
                while (frameDelay > 0)
                {
                    frameDelay -= 1;
                    yield return null;
                }
                Debug.Log($"success {Time.frameCount}");
            }

            var framesToWait = 5;

            runner.AddCommandHandler("test", () => runner.StartCoroutine(TestCommandCoroutine(framesToWait)));

            LogAssert.Expect(LogType.Log, $"success {Time.frameCount + framesToWait}");

            var result = dispatcher.DispatchCommand("test", runner);
            Assert.AreEqual(CommandDispatchResult.StatusType.Succeeded, result.Status);

            Assert.IsFalse(result.Task.IsCompleted());

            // After framesToWait frames, we should have seen the log
            while (framesToWait > 0)
            {
                framesToWait -= 1;
                yield return null;
            }
        }

        [TestCase(@"one two three four", new[] { "one", "two", "three", "four" })]
        [TestCase(@"one ""two three"" four", new[] { "one", "two three", "four" })]
        [TestCase(@"one ""two three four", new[] { "one", "two three four" })]
        [TestCase(@"one ""two \""three"" four", new[] { "one", "two \"three", "four" })]
        [TestCase(@"one \two three four", new[] { "one", "\\two", "three", "four" })]
        [TestCase(@"one ""two \\ three"" four", new[] { "one", "two \\ three", "four" })]
        [TestCase(@"one ""two \1 three"" four", new[] { "one", "two \\1 three", "four" })]
        [TestCase(@"one      two", new[] { "one", "two" })]
        public void SplitCommandText_SplitsTextCorrectly(string input, IEnumerable<string> expectedComponents)
        {
            IEnumerable<string> parsedComponents = DialogueRunner.SplitCommandText(input);

            Assert.AreEqual(expectedComponents, parsedComponents);
        }

        [UnityTest]
        public IEnumerator DialogueRunner_OnDialogueStartAndStop_CallsEvents()
        {


            runner.onDialogueStart?.AddListener(() =>
            {
                Debug.Log("Dialogue start");
            });

            runner.onDialogueComplete?.AddListener(() =>
            {
                Debug.Log("Dialogue complete");
            });

            LogAssert.Expect(LogType.Log, "Dialogue start");
            LogAssert.Expect(LogType.Log, "Dialogue complete");

            runner.StartDialogue(runner.startNode);

            yield return new WaitForSeconds(0.5f);

            runner.Stop();
        }

        [Test]
        public void DialogueRunner_CanQueryNodeGroupCandidates()
        {
            runner.Dialogue.GetSaliencyOptionsForNodeGroup("NodeGroups").Where(c => c?.FailingConditionValueCount == 0).Should().HaveCount(1);

            runner.VariableStorage.SetValue("$nodeGroupCondition1", true);

            runner.Dialogue.GetSaliencyOptionsForNodeGroup("NodeGroups").Where(c => c?.FailingConditionValueCount == 0).Should().HaveCount(3);

            runner.VariableStorage.SetValue("$nodeGroupCondition2", true);

            runner.Dialogue.GetSaliencyOptionsForNodeGroup("NodeGroups").Where(c => c?.FailingConditionValueCount == 0).Should().HaveCount(7);
        }
    }
}
