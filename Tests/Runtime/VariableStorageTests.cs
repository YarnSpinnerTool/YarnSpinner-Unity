using System.Collections;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Yarn.Unity;

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
        DialogueRunner Runner => GameObject.FindObjectOfType<DialogueRunner>();
        DialogueUI UI => GameObject.FindObjectOfType<DialogueUI>();
        InMemoryVariableStorage VarStorage => GameObject.FindObjectOfType<InMemoryVariableStorage>();
        Text TextCanvas => UI.dialogueContainer.transform.GetComponentsInChildren<Text>()
                                                         .First(element => element.gameObject.name == "Text");

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

        [UnityTest]
        public IEnumerator SetValue_TryGetValue()
        {
            // run all lines
            Runner.StartDialogue();
            UI.MarkLineComplete();
            yield return null;
            yield return null;
            UI.MarkLineComplete();
            yield return null;
            yield return null;

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

            yield return null;
        }

        void TestClearVarStorage() {
            VarStorage.Clear();
            int varCount = 0;
            foreach ( var variable in VarStorage ) {
                varCount++;
            }
            Assert.AreEqual(0,varCount);
        }

        void TestVariableValuesFromYarnScript() {
            VarStorage.TryGetValue<string>("$stringVar", out var actualStringResult);
            Assert.AreEqual("hola", actualStringResult);
            VarStorage.TryGetValue<bool>("$boolVar", out var actualBoolResult);
            Assert.AreEqual(true, actualBoolResult);
            VarStorage.TryGetValue<float>("$floatVar", out var actualFloatResult);
            Assert.AreEqual(1.420f, actualFloatResult);
        }

        const string testPlayerPrefsKey = "TemporaryPlayerPrefsYarnTestKey";
        [UnityTest]
        public IEnumerator SaveLoadPlayerPrefs()
        {
            // run all lines
            Runner.StartDialogue();
            UI.MarkLineComplete();
            yield return null;
            yield return null;
            UI.MarkLineComplete();
            yield return null;
            yield return null;

            // save all variable values to Player Prefs, clear, then load from Player Prefs
            VarStorage.SaveToPlayerPrefs( testPlayerPrefsKey );
            TestClearVarStorage();
            VarStorage.LoadFromPlayerPrefs( testPlayerPrefsKey );
            TestVariableValuesFromYarnScript();

            // cleanup
            PlayerPrefs.DeleteKey( testPlayerPrefsKey );
        }

        string testFilePath { get { return Application.persistentDataPath + Path.DirectorySeparatorChar + "YarnVariableStorageTest.json" ;} }
        [UnityTest]
        public IEnumerator SaveLoadFile()
        {
            // run all lines
            Runner.StartDialogue();
            UI.MarkLineComplete();
            yield return null;
            yield return null;
            UI.MarkLineComplete();
            yield return null;
            yield return null;

            // save all variable values to a file, clear, then load from a file
            VarStorage.SaveToFile( testFilePath );
            TestClearVarStorage();
            VarStorage.LoadFromFile( testFilePath );
            TestVariableValuesFromYarnScript();

            // cleanup
            File.Delete( testFilePath );
        }
    }
}
