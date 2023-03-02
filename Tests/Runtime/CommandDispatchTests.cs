using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;
using System.IO;

namespace Yarn.Unity.Tests
{
    [TestFixture]
    public class CommandDispatchTests : IPrebuildSetup, IPostBuildCleanup
    {

#if UNITY_EDITOR
        string outputFilePath => TestFilesDirectoryPath + "YarnActionRegistration.cs";
        const string testScriptGUID = "32f15ac5211d54a68825dfb9532e93f4";

        string TestFolderName => nameof(CommandDispatchTests);
        string TestFilesDirectoryPath => $"Assets/{TestFolderName}/";
        string TestScriptPathSource => UnityEditor.AssetDatabase.GUIDToAssetPath(testScriptGUID);
        string TestScriptPathInProject => TestFilesDirectoryPath + Path.GetFileName(TestScriptPathSource);
#endif

        public void Setup()
        {
            if (Directory.Exists(TestFilesDirectoryPath) == false) {
                UnityEditor.AssetDatabase.CreateFolder("Assets", TestFolderName);
                UnityEditor.AssetDatabase.CopyAsset(TestScriptPathSource, TestScriptPathInProject);
            }

#if UNITY_EDITOR && !UNITY_2021_2_OR_NEWER
            // On Unity 2021.1 and earlier, we need to manually generate the
            // registration code. On later versions, the source code generator
            // will handle it for us.

            var analysis = new Yarn.Unity.ActionAnalyser.Analyser(TestScriptPathInProject);
            var actions = analysis.GetActions();
            var source = Yarn.Unity.ActionAnalyser.Analyser.GenerateRegistrationFileSource(actions, "Yarn.Unity.Tests.Generated");
            
            System.IO.File.WriteAllText(outputFilePath, source);
#endif
            UnityEditor.AssetDatabase.Refresh();
        }

        public void Cleanup()
        {
            UnityEditor.AssetDatabase.DeleteAsset(TestFilesDirectoryPath);
            UnityEditor.AssetDatabase.Refresh();
        }

        [Test]
        public void CommandDispatch_Passes() {
            var dialogueRunnerGO = new GameObject("Dialogue Runner");
            var dialogueRunner = dialogueRunnerGO.AddComponent<DialogueRunner>();

            var dispatcher = dialogueRunner.CommandDispatcher;

            var expectedCommandNames = new[] {
                "InstanceDemoActionWithNoName",
                "instance_demo_action",
                "instance_demo_action_with_params",
                "instance_demo_action_with_optional_params",
                "StaticDemoActionWithNoName",
                "static_demo_action",
                "static_demo_action_with_params",
                "static_demo_action_with_optional_params",
            };

            var expectedFunctionNames = new[] {
                "int_void",
                "int_params",
            };

            var actualCommandNames = dispatcher.Commands.Select(c => c.Name).ToList();

            foreach (var expectedCommandName in expectedCommandNames)
            {
                Assert.Contains(expectedCommandName, actualCommandNames, "expected command {0} to be registered", expectedCommandName);
            }

            foreach (var expectedFunctionName in expectedFunctionNames)
            {
                Assert.True(dialogueRunner.Dialogue.Library.FunctionExists(expectedFunctionName), "expected function {0} to be registered", expectedFunctionName);
            }
        }
    }
}
