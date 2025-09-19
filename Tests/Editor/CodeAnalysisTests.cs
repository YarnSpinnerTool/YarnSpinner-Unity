/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity.Editor;

#nullable enable

namespace Yarn.Unity.Tests
{
    public class CodeAnalysisTests
    {
        readonly string[] testScriptGUIDs = new string[] {
            "32f15ac5211d54a68825dfb9532e93f4",
            "38cc17b47f2af4fb5a9f4837db188e62",
        };

        string OutputFilePath => YarnTestUtility.TestFilesDirectoryPath + "YarnActionRegistration.cs";

        IEnumerable<string> TestScriptPathSources => testScriptGUIDs.Select(g => AssetDatabase.GUIDToAssetPath(g));

        string TestScriptFolderInProject => YarnTestUtility.TestFilesDirectoryPath;

        string TestNamespace => "Yarn.Unity.Generated." + TestContext.CurrentContext.Test.MethodName;

        readonly string[] expectedCommands = new string[] {
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
            "external_file_command",
        };

        readonly string[] expectedFunctions = new string[] {
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
            "direct_register_external_file_function_lambda",
            "direct_register_external_file_function_method",
            "direct_register_nested_class",
            "external_file_function",
        };

        private void SetUpTestActionCode()
        {
            if (Directory.Exists(YarnTestUtility.TestFilesDirectoryPath) == false)
            {
                AssetDatabase.CreateFolder("Assets", YarnTestUtility.TestFolderName);
            }

            foreach (var source in TestScriptPathSources)
            {
                AssetDatabase.CopyAsset(source, Path.Combine(TestScriptFolderInProject, Path.GetFileName(source)));
            }

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
                var analysis = new Yarn.Unity.ActionAnalyser.Analyser(TestScriptFolderInProject);
                var actions = analysis.GetActions();
                var source = Yarn.Unity.ActionAnalyser.Analyser.GenerateRegistrationFileSource(actions, TestNamespace);

                System.IO.File.WriteAllText(OutputFilePath, source);

                AssetDatabase.Refresh();
                yield return new RecompileScripts(expectScriptCompilation: true, expectScriptCompilationSuccess: true);
            }
            finally
            {
                TearDownTestActionCode();
            }
        }

        [Test]
        public void CodeAnalysis_GeneratesDescriptionDataForActions()
        {
            try
            {
                SetUpTestActionCode();

                var analysis = new Yarn.Unity.ActionAnalyser.Analyser(TestScriptFolderInProject);
                var actions = analysis.GetActions();

                var documentedAttributeAction = actions.Single(a => a.Name == "instance_demo_action_with_optional_params");

                documentedAttributeAction.Description.Should().BeEqualTo("An instance action with two parameters, one of which is optional.");

                documentedAttributeAction.Parameters[0].Name.Should().BeEqualTo("param");
                documentedAttributeAction.Parameters[0].Description.Should().BeEqualTo("The first, non-optional parameter.");
                documentedAttributeAction.Parameters[0].IsOptional.Should().BeFalse();
                documentedAttributeAction.Parameters[0].DefaultValueString.Should().BeNull();

                documentedAttributeAction.Parameters[1].Name.Should().BeEqualTo("param2");
                documentedAttributeAction.Parameters[1].Description.Should().BeEqualTo("The second, optional parameter.");
                documentedAttributeAction.Parameters[1].IsOptional.Should().BeTrue();
                documentedAttributeAction.Parameters[1].DefaultValueString.Should().BeEqualTo("0");

                var documentedDirectAction = actions.Single(a => a.Name == "direct_register_method_fixed_params");
                documentedDirectAction.Description.Should().BeEqualTo("A directly-registered method.");
                documentedDirectAction.Parameters[0].Name.Should().BeEqualTo("a");
                documentedDirectAction.Parameters[0].Description.Should().BeEqualTo("The first parameter.");

                documentedDirectAction.Parameters[1].Name.Should().BeEqualTo("b");
                documentedDirectAction.Parameters[1].Description.Should().BeEqualTo("The second parameter.");

                var text = documentedAttributeAction.ToJSON();
                Debug.Log(text);
            }
            finally
            {
                TearDownTestActionCode();
            }
        }

        [Test]
        public void CodeAnalysis_FindsExpectedActions()
        {
            try
            {
                SetUpTestActionCode();

                var analysis = new Yarn.Unity.ActionAnalyser.Analyser(TestScriptFolderInProject);
                var actions = analysis.GetActions();
                var generatedSource = ActionAnalyser.Analyser.GenerateRegistrationFileSource(actions, TestNamespace);

                var commands = actions.Where(a => a.Type == ActionAnalyser.ActionType.Command);
                var functions = actions.Where(a => a.Type == ActionAnalyser.ActionType.Function);

                foreach (var commandName in expectedCommands)
                {
                    commands.Should().ContainSingle(c => c.Name == commandName, $"command {commandName} should be found");

                    var matchRegex = new Regex($@"AddCommandHandler(<.*>)?\(""{commandName}""");
                    generatedSource.Should().Match(matchRegex, $"command {commandName} should be registered in the generated source");
                }

                foreach (var functionName in expectedFunctions)
                {
                    functions.Should().ContainSingle(c => c.Name == functionName, $"function {functionName} should be found");
                    var matchRegex = new Regex($@"RegisterFunctionDeclaration\(""{functionName}""");
                    generatedSource.Should().Match(matchRegex, $"function {functionName} should be registered in the generated source");
                }
            }
            finally
            {
                TearDownTestActionCode();
            }
        }

        private class TestActionRegistration : IActionRegistration
        {
            public List<string> RegisteredCommandNames = new();
            public List<string> RegisteredFunctionNames = new();

            public void AddCommandHandler(string commandName, Delegate handler) => RegisteredCommandNames.Add(commandName);

            public void AddCommandHandler(string commandName, MethodInfo methodInfo) => RegisteredCommandNames.Add(commandName);

            public void AddFunction(string name, Delegate implementation) { }

            public void RegisterFunctionDeclaration(string name, Type returnType, Type[] parameterTypes) => RegisteredFunctionNames.Add(name);

            public void RemoveCommandHandler(string commandName) { }

            public void RemoveFunction(string name) { }
        }

        [UnityTest]
        public IEnumerator CodeAnalysis_OnDomainReload_RegistersCommandsAndFunctions()
        {
            try
            {
                // Given: a .cs file containing actions is added to the codebase
                SetUpTestActionCode();
                yield return new RecompileScripts(expectScriptCompilation: true, expectScriptCompilationSuccess: true);

                var registrationMethods = Actions.ActionRegistrationMethods;

                var registrar = new TestActionRegistration();

                // When
                foreach (var method in registrationMethods)
                {
                    method.Invoke(registrar, RegistrationType.Compilation);
                }

                // Then
                foreach (var command in expectedCommands)
                {
                    registrar.RegisteredCommandNames.Should().ContainSingle(n => n == command,
                        $"command {command} should be registered once");
                }

                foreach (var function in expectedFunctions)
                {
                    registrar.RegisteredFunctionNames.Should().ContainSingle(n => n == function,
                        $"function {function} should be registered once");
                }
            }
            finally
            {
                TearDownTestActionCode();
            }
        }
    }
}
