/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

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
            if (Directory.Exists(TestFilesDirectoryPath) == false)
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", TestFolderName);
                UnityEditor.AssetDatabase.CopyAsset(TestScriptPathSource, TestScriptPathInProject);
            }

            UnityEditor.AssetDatabase.Refresh();
        }

        public void Cleanup()
        {
            UnityEditor.AssetDatabase.DeleteAsset(TestFilesDirectoryPath);
            UnityEditor.AssetDatabase.Refresh();
        }

        [Test]
        public void CommandDispatch_Passes()
        {
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
                "static_variadic",
                "instance_variadic",
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
