using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity.Editor;

namespace Yarn.Unity.Tests
{
    public class CodeAnalysisTests
    {
        string outputFilePath => TestFilesDirectoryPath + "YarnActionRegistration.cs";
        const string testScriptGUID = "32f15ac5211d54a68825dfb9532e93f4";

        string TestFolderName => TestContext.CurrentContext.Test.FullName;
        string TestFilesDirectoryPath => $"Assets/{TestFolderName}/";

        string TestScriptPathSource => AssetDatabase.GUIDToAssetPath(testScriptGUID);
        string TestScriptPathInProject => TestFilesDirectoryPath + Path.GetFileName(TestScriptPathSource);

        string TestNamespace => "Yarn.Unity.Generated." + TestContext.CurrentContext.Test.MethodName;

        [SetUp]
        public void SetUp()
        {
            if (Directory.Exists(TestFilesDirectoryPath) == false)
            {
                AssetDatabase.CreateFolder("Assets", TestFolderName);
                AssetDatabase.CopyAsset(TestScriptPathSource, TestScriptPathInProject);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AssetDatabase.DeleteAsset(TestFilesDirectoryPath);
            AssetDatabase.Refresh();
            yield return new RecompileScripts(expectScriptCompilation: true, expectScriptCompilationSuccess: true);
        }

        [UnityTest]
        public IEnumerator CodeAnalysis_CanGenerateSourceCode()
        {
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

        [UnityTest]
        public IEnumerator CodeAnalysis_GeneratedSourceCode_RegistersExpectedActions()
        {
            // Generate source code from our test script, save the resulting
            // source code in the proejct, and when recompilation is complete, validate that it registered a method with the name that we expect.

            var analysis = new Yarn.Unity.ActionAnalyser.Analyser(TestScriptPathInProject);
            var actions = analysis.GetActions();
            var source = Yarn.Unity.ActionAnalyser.Analyser.GenerateRegistrationFileSource(actions, TestNamespace);

            System.IO.File.WriteAllText(outputFilePath, source);

            AssetDatabase.Refresh();
            yield return new RecompileScripts(expectScriptCompilation: true, expectScriptCompilationSuccess: true);

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
    }
}
