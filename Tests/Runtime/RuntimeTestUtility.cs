#if UNITY_2021_2_OR_NEWER
    #define SOURCE_GENERATOR_AVAILABLE
#endif

using NUnit.Framework;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Yarn.Unity.Tests
{
    public static class RuntimeTestUtility
    {
        const string YarnSpinnerRuntimeAsmDefGUID = "34aa492b82754644eac2f903cd496268";

        private static string TestFolderName => "Yarn.Unity.Tests.ActionRegistrationFiles";

        private static string TestFilesDirectoryPath => $"Assets/{TestFolderName}/";
        private static string GeneratedRegistrationPath => TestFilesDirectoryPath + "YarnActionRegistration.cs";

        private static string GeneratedAssemblyDefinitionFilePath => TestFilesDirectoryPath + "RuntimeTestAssembly.asmdef";

        public static void GenerateRegistrationSource(string scriptFolderGUID)
        {
#if UNITY_EDITOR && !SOURCE_GENERATOR_AVAILABLE
            // On Unity 2021.1 and earlier, we need to manually generate the
            // registration code. On later versions, the source code generator
            // will handle it for us.
            if (System.IO.Directory.Exists(TestFilesDirectoryPath) == false)
            {
                AssetDatabase.CreateFolder("Assets", TestFolderName);
            }
            
            var scriptFolderPath = AssetDatabase.GUIDToAssetPath(scriptFolderGUID);

            if (System.IO.Directory.Exists(scriptFolderPath) == false) {
                throw new System.IO.DirectoryNotFoundException($"{scriptFolderPath} (guid {scriptFolderGUID})is not a valid directory");
            }

            var analysis = new ActionAnalyser.Analyser(scriptFolderPath);
            var actions = analysis.GetActions();
            var source = Yarn.Unity.ActionAnalyser.Analyser.GenerateRegistrationFileSource(actions, "Yarn.Unity.Tests.Generated");

            System.IO.File.WriteAllText(GeneratedRegistrationPath, source);

            // In order for the generated registration file to be able to
            // reference the methods in the runtime Yarn Spinner tests, we need
            // to explicitly create a reference to the test assemblies. This
            // means that we need to put these files in an .asmdef file that's
            // correctly configured.

            // Generate an .asmdef file that references all asmdefs in
            // scriptFolderPath
            var asmDefs = System.IO.Directory.EnumerateFiles(scriptFolderPath, "*.asmdef", System.IO.SearchOption.AllDirectories).ToArray();

            var asmDefSourceBuilder = new System.Text.StringBuilder();

            asmDefSourceBuilder.AppendLine("{");
            asmDefSourceBuilder.AppendLine(@"""name"": ""RuntimeTest"",");
            asmDefSourceBuilder.AppendLine(@"""rootNamespace"": """",");
            asmDefSourceBuilder.AppendLine(@"""references"": [");

            // Add references to every assembly definition we found in
            // scriptFolderPath
            foreach (string asmDef in asmDefs) {
                var guid = AssetDatabase.AssetPathToGUID(asmDef);

                asmDefSourceBuilder.AppendLine($@"""GUID:{guid}"",");
                
            }

            // Finally, add a reference to Yarn Spinner itself
            asmDefSourceBuilder.AppendLine($@"""GUID:{YarnSpinnerRuntimeAsmDefGUID}""");
            asmDefSourceBuilder.AppendLine(@"]");
            asmDefSourceBuilder.AppendLine("}");

            System.IO.File.WriteAllText(GeneratedAssemblyDefinitionFilePath, asmDefSourceBuilder.ToString());

            AssetDatabase.Refresh();
#endif
        }

        public static void CleanupGeneratedSource()
        {
#if UNITY_EDITOR && !SOURCE_GENERATOR_AVAILABLE
            AssetDatabase.DeleteAsset(TestFilesDirectoryPath);
            AssetDatabase.Refresh();
#endif
        }
        
        public static void AddSceneToBuild(string GUID)
        {
#if UNITY_EDITOR
            // Is a scene with this GUID already in the list?
            if (EditorBuildSettings.scenes.Any(x => x.guid.ToString() == GUID))
            {
                // Then there's nothing to do!
                return;
            }

            // Add the test scene 
            var dialogueRunnerTestScene = new EditorBuildSettingsScene(new GUID(GUID), true);
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { dialogueRunnerTestScene }).ToArray();
#endif
        }

        public static void RemoveSceneFromBuild(string GUID)
        {
#if UNITY_EDITOR
            // Filter the list to remove any scene with this GUID
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(x => x.guid.ToString() != GUID).ToArray();
#endif
        }
    }
}
