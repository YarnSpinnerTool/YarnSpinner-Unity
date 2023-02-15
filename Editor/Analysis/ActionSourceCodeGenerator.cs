using UnityEngine;
using Yarn.Unity;
using UnityEditor;
using System.Linq;

namespace Yarn.Unity.Editor
{
    public static class ActionSourceCodeGenerator
    {

        public static string GeneratedSourcePath
        {
            get
            {
                const string YarnRegistrationFileName = "YarnActionRegistration.cs";
                const string DefaultOutputFilePath = "Assets/" + YarnRegistrationFileName;
                const string YarnGeneratedCodeSignature = "GeneratedCode(\"YarnActionAnalyzer\"";
                
                var existingFile = System.IO.Directory.EnumerateFiles(System.Environment.CurrentDirectory, YarnRegistrationFileName, System.IO.SearchOption.AllDirectories).FirstOrDefault();

                if (existingFile == null)
                {
                    return DefaultOutputFilePath;
                }
                else
                {
                    try
                    {
                        var text = System.IO.File.ReadAllText(existingFile);
                        return text.Contains(YarnGeneratedCodeSignature) ? existingFile : DefaultOutputFilePath;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Can't check to see if {existingFile} is a valid action registration script, using {DefaultOutputFilePath} instead: {e}");
                        return DefaultOutputFilePath;
                    }
                }

            }
        }

        [MenuItem("Window/Yarn Spinner/Update Yarn Commands")]
        public static void GenerateYarnActionSourceCode()
        {
            var analysis = new Yarn.Unity.ActionAnalyser.Analyser("Assets");
            try
            {
                var actions = analysis.GetActions();
                var source = analysis.GenerateRegistrationFileSource(actions);

                var path = GeneratedSourcePath;

                System.IO.File.WriteAllText(path, source);
                UnityEditor.AssetDatabase.ImportAsset(path);

                Debug.Log($"Generated Yarn command and function registration code at {path}");
            }
            catch (Yarn.Unity.ActionAnalyser.AnalyserException e)
            {
                Debug.LogError($"Error generating source code: " + e.InnerException.ToString());
            }
        }

    }
}
