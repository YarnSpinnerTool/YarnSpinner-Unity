﻿/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using NUnit.Framework;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Yarn.Unity;
using Yarn.Unity.Legacy;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS0612 // Type or member is obsolete

namespace Yarn.Unity.Tests
{
    public class VariableStorageTests : IPrebuildSetup, IPostBuildCleanup
    {
        public void Setup()
        {
            RuntimeTestUtility.AddSceneToBuild(VariableStorageTestsSceneGUID);
        }

        public void Cleanup()
        {
            RuntimeTestUtility.RemoveSceneFromBuild(VariableStorageTestsSceneGUID);
        }

        const string VariableStorageTestsSceneGUID = "5b5f09716ba7bce4a8d2f115ea6083d3";

        // Getters for the various components in the scene that we're
        // working with
        DialogueRunner Runner => GameObject.FindAnyObjectByType<DialogueRunner>();
        LineView UI => GameObject.FindAnyObjectByType<LineView>();
        InMemoryVariableStorage VarStorage => GameObject.FindAnyObjectByType<InMemoryVariableStorage>();
        TMPro.TextMeshProUGUI? TextCanvas => UI.lineText;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("VariableStorageTests");
            bool loaded = false;
            SceneManager.sceneLoaded += (index, mode) =>
            {
                loaded = true;
            };
            yield return new WaitUntil(() => loaded);
        }


        const string stringTest = "Testing string variable.";
        const bool boolTest = true;
        const float floatTest = 4.20f;

        public void SetValue_TryGetValue()
        {
            // set, then get, then test equality
            VarStorage.SetValue("$stringVar", stringTest);
            VarStorage.TryGetValue<string>("$stringVar", out var actualStringResult);
            Assert.AreEqual(stringTest, actualStringResult);

            VarStorage.SetValue("$boolVar", boolTest);
            VarStorage.TryGetValue<bool>("$boolVar", out var actualBoolResult);
            Assert.AreEqual(boolTest, actualBoolResult);

            VarStorage.SetValue("$floatVar", floatTest);
            VarStorage.TryGetValue<float>("$floatVar", out var actualFloatResult);
            Assert.AreEqual(floatTest, actualFloatResult);

            VarStorage.SetValue("$totallyNewUndeclaredVar", stringTest);
            VarStorage.TryGetValue<string>("$totallyNewUndeclaredVar", out var actualUndeclaredResult);
            Assert.AreEqual(stringTest, actualUndeclaredResult);
        }

        void TestClearVarStorage()
        {
            VarStorage.Clear();
            int varCount = 0;
            foreach (var variable in VarStorage)
            {
                varCount++;
            }
            Assert.AreEqual(0, varCount);
        }

        [UnityTest]
        public IEnumerator TestVariableValuesFromYarnScript()
        {
            // run all lines
            Runner.StartDialogue(Runner.startNode);
            yield return null;

            VarStorage.TryGetValue<string>("$stringVar", out var actualStringResult);
            Assert.AreEqual("hola", actualStringResult);
            VarStorage.TryGetValue<bool>("$boolVar", out var actualBoolResult);
            Assert.AreEqual(true, actualBoolResult);
            VarStorage.TryGetValue<float>("$floatVar", out var actualFloatResult);
            Assert.AreEqual(1.420f, actualFloatResult);
        }

        [UnityTest]
        public IEnumerator TestLoadingAndSettingAllVariables()
        {
            // ok I need to test that the bulk load and save works
            Runner.StartDialogue(Runner.startNode);
            yield return null;
            var dump = VarStorage.GetAllVariables();
            TestClearVarStorage();
            VarStorage.SetAllVariables(dump.Item1, dump.Item2, dump.Item3);
            TestVariableValuesFromYarnScript();
        }

        string testFilePath { get { return System.IO.Path.Combine(Application.persistentDataPath, "YarnVariableStorageTest.json"); } }

        [UnityTest]
        public IEnumerator TestSavingAndLoadingFile()
        {
            // run all lines
            Runner.StartDialogue(Runner.startNode);
            yield return null;

            // save all variable values to a file, clear, then load from a file
            Runner.SaveStateToPersistentStorage(testFilePath);
            TestClearVarStorage();
            Runner.LoadStateFromPersistentStorage(testFilePath);
            TestVariableValuesFromYarnScript();

            // cleanup
            File.Delete(testFilePath);
        }

        // need another test here where we test the default variable loading
        // because we don't currently actually test that...
        [Test]
        public void TestLoadingDefaultValues()
        {
            var hasVar = VarStorage.TryGetValue<string>("$defaultString", out var defaultString);
            Assert.IsTrue(hasVar);
            Assert.AreEqual("hello", defaultString);

            hasVar = VarStorage.TryGetValue<bool>("$defaultBool", out var defaultBool);
            Assert.IsTrue(hasVar);
            Assert.AreEqual(true, defaultBool);

            hasVar = VarStorage.TryGetValue<float>("$defaultFloat", out var defaultFloat);
            Assert.IsTrue(hasVar);
            Assert.AreEqual(999, defaultFloat);
        }

        [Test]
        public void VariableStorage_OnUsingValueWithInvalidName_ThrowsError()
        {
            VarStorage.SetValue("$valid", 1);

            Assert.Throws<System.ArgumentException>(() =>
            {
                VarStorage.SetValue("invalid", 1);
            });

            VarStorage.TryGetValue<float>("$valid", out var result1);

            Assert.Throws<System.ArgumentException>(() =>
            {
                VarStorage.TryGetValue<float>("invalid", out var result2);
            });

        }

        [Test]
        public void VariableStorage_CanRegisterChangeListeners()
        {
            var boolListener = VarStorage.AddChangeListener("$boolVar", (bool value) =>
            {
                Debug.Log($"$boolVar changed to " + value);
            });

            var stringListener = VarStorage.AddChangeListener("$stringVar", (string value) =>
            {
                Debug.Log($"$stringVar changed to " + value);
            });

            var floatListener = VarStorage.AddChangeListener("$floatVar", (float value) =>
            {
                Debug.Log($"$floatVar changed to " + value);
            });

            LogAssert.Expect(LogType.Log, "$boolVar changed to True");
            VarStorage.SetValue("$boolVar", true);

            LogAssert.Expect(LogType.Log, "$stringVar changed to goodbye");
            VarStorage.SetValue("$stringVar", "goodbye");

            LogAssert.Expect(LogType.Log, "$floatVar changed to 42");
            VarStorage.SetValue("$floatVar", 42);

            // Disposing of the listeners removes them
            boolListener.Dispose();
            stringListener.Dispose();
            floatListener.Dispose();

            // After we have removed the listeners, the change listeners don't
            // get called anymore
            VarStorage.SetValue("$boolVar", true);
            VarStorage.SetValue("$stringVar", "goodbye");
            VarStorage.SetValue("$floatVar", 42);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void VariableStorage_OnAddingInvalidChangeListener_ThrowsError()
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                VarStorage.AddChangeListener("$smartBool", (bool value) =>
                {
                    Assert.Fail("This method should never be called.");
                });
            }, "change listeners cannot be added for smart variables");

            Assert.Throws<System.ArgumentException>(() =>
            {
                VarStorage.AddChangeListener("$floatVar", (bool value) =>
                {
                    Assert.Fail("This method should never be called.");
                });
            }, "change listeners must match the type of their target variable");
        }
    }
}
