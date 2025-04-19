/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity.Editor;

namespace Yarn.Unity.Tests
{
    public class CodeAnalysisTests
    {
        string outputFilePath => YarnTestUtility.TestFilesDirectoryPath + "YarnActionRegistration.cs";
        const string testScriptGUID = "32f15ac5211d54a68825dfb9532e93f4";

        string TestScriptPathSource => AssetDatabase.GUIDToAssetPath(testScriptGUID);
        string TestScriptPathInProject => YarnTestUtility.TestFilesDirectoryPath + Path.GetFileName(TestScriptPathSource);

        string TestNamespace => "Yarn.Unity.Generated." + TestContext.CurrentContext.Test.MethodName;

        private void SetUpTestActionCode()
        {
            if (Directory.Exists(YarnTestUtility.TestFilesDirectoryPath) == false)
            {
                AssetDatabase.CreateFolder("Assets", YarnTestUtility.TestFolderName);
            }

            AssetDatabase.CopyAsset(TestScriptPathSource, TestScriptPathInProject);
        }
        private void TearDownTestActionCode()
        {
            AssetDatabase.DeleteAsset(YarnTestUtility.TestFilesDirectoryPath);
            AssetDatabase.Refresh();
        }

        [UnityTest]
        public IEnumerator CodeAnalysis_CanGenerateSourceCode()
        {
            try
            {
                SetUpTestActionCode();

                // Generate source code from our test script, save the resulting
                // source code in the proejct, and validate that everything still
                // compiles.
                var analysis = new Yarn.Unity.ActionAnalyser.Analyser(TestScriptPathInProject);
                var actions = analysis.GetActions();
                var source = Yarn.Unity.ActionAnalyser.Analyser.GenerateRegistrationFileSource(actions, TestNamespace);

                System.IO.File.WriteAllText(outputFilePath, source);

                AssetDatabase.Refresh();
                yield return new RecompileScripts(expectScriptCompilation: true, expectScriptCompilationSuccess: true);
            }
            finally
            {
                TearDownTestActionCode();
            }
        }

        [Test]
        public void CodeAnalysis_FindsExpectedActions()
        {
            var analysis = new Yarn.Unity.ActionAnalyser.Analyser(TestScriptPathSource);
            var actions = analysis.GetActions();
            var generatedSource = ActionAnalyser.Analyser.GenerateRegistrationFileSource(actions, TestNamespace);

            var expectedCommands = new string[] {
                "InstanceDemoActionWithNoName",
                "instance_demo_action",
                "instance_demo_action_with_params",
                "instance_demo_action_with_optional_params",
                "StaticDemoActionWithNoName",
                "static_demo_action",
                "static_demo_action_with_params",
                "static_demo_action_with_optional_params",
                "instance_variadic",
                "static_variadic",
            };

            var expectedFunctions = new string[] {
                "direct_register_lambda_no_params",
                "direct_register_lambda_fixed_params",
                "direct_register_lambda_variadic_params",
                "direct_register_method_no_params",
                "direct_register_method_fixed_params",
                "direct_register_method_variadic_params",
                "int_void",
                "int_params",
                "local_constant_name",
                "other_type_constant",
                "constant_name",
            };

            var commands = actions.Where(a => a.Type == ActionAnalyser.ActionType.Command);
            var functions = actions.Where(a => a.Type == ActionAnalyser.ActionType.Function);

            foreach (var commandName in expectedCommands)
            {
                commands.Should().Contain(c => c.Name == commandName, $"command {commandName} should be found");

                var matchRegex = new Regex($@"AddCommandHandler(<.*>)?\(""{commandName}""");
                generatedSource.Should().Match(matchRegex, $"command {commandName} should be registered in the generated source");
            }

            foreach (var functionName in expectedFunctions)
            {
                functions.Should().Contain(c => c.Name == functionName, $"function {functionName} should be found");
                var matchRegex = new Regex($@"RegisterFunctionDeclaration\(""{functionName}!""");
                generatedSource.Should().Match(matchRegex, $"function {functionName} should be registered in the generated source");
            }
        }

        [UnityTest]
        public IEnumerator CodeAnalysis_GeneratedSourceCode_AttachesRegistrationMethod()
        {
            try
            {
                SetUpTestActionCode();
                AssetDatabase.Refresh();
                yield return new RecompileScripts(expectScriptCompilation: false, expectScriptCompilationSuccess: true);

                var registrationMethods = Actions.ActionRegistrationMethods;

                // The generated source code should have this fully qualified name:
                string expectedFullMethodName = TestNamespace + ".ActionRegistration.RegisterActions";

                var registrationMethodNames = registrationMethods.Select(m => GetFullMethodName(m.Method)).ToList();

                Debug.Log($"Action registration methods:");
                foreach (var registrationMethodName in registrationMethodNames)
                {
                    Debug.Log(registrationMethodName);
                }

                Assert.Contains(expectedFullMethodName, registrationMethodNames, $"Actions should have registered method {expectedFullMethodName}");

                string GetFullMethodName(System.Reflection.MethodInfo method)
                {
                    if (method is null)
                    {
                        throw new ArgumentNullException(nameof(method));
                    }

                    return $"{method.DeclaringType.FullName}.{method.Name}";
                }
            }
            finally
            {
                TearDownTestActionCode();
            }
        }
    }
}
