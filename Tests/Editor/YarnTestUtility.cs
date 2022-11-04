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
    public static class YarnTestUtility {

        internal static DefaultAsset GetFolder(string directoryName)
        {
            var path = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(directoryName) + " t:DefaultAsset")
                                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                                    .FirstOrDefault(p => Path.GetFileName(p) == directoryName);

            if (path == null)
            {
                throw new DirectoryNotFoundException(path);
            }
            if (Directory.Exists(path) == false)
            {
                throw new DirectoryNotFoundException(path);
            }

            return AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
        }

        internal static TextAsset GetScriptSource(string fileName)
        {
            var path = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName))
                                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                                    .FirstOrDefault(p => Path.GetFileName(p) == fileName);

            if (path == null)
            {
                throw new FileNotFoundException(path);
            }

            Debug.Log($"Resolved {fileName} to {path}");

            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }

        // A sample Yarn script that we'll store in an asset as part of
        // these tests
        internal static string TestYarnScriptSource => GetScriptSource("TestYarnScript.yarn").text;

        internal static string TestYarnProgramSource => GetScriptSource("TestYarnProject.yarnproject").text;

        internal static string TestYarnScriptSourceModified => GetScriptSource("TestYarnScript-Modified.yarn").text;

        internal static string TestYarnScriptSourceNoMetadata => GetScriptSource("TestYarnScript-NoMetadata.yarn").text;

        internal static IEnumerable<StringTableEntry> ExpectedStrings => StringTableEntry.ParseFromCSV(GetScriptSource("TestYarnProject-Strings.csv").text);

        internal static IEnumerable<LineMetadataTableEntry> ExpectedMetadata => LineMetadataTableEntry.ParseFromCSV(GetScriptSource("TestYarnProject-Metadata.csv").text);

        
        internal static void DeleteFilesAndMetadata(List<string> paths)
        {
            foreach (var path in paths)
            {
                Debug.Log($"Cleanup: Deleting {path}");
                File.Delete(path);

                string metaFilePath = path + ".meta";

                if (File.Exists(metaFilePath))
                {
                    File.Delete(metaFilePath);
                }
            }

            AssetDatabase.Refresh();
        }


        internal static void SetupYarnProject(string[] yarnScriptText, ref List<string> createdFilePaths, out YarnProject yarnProject)
        {
            // Disable errors causing failures, in case the yarn script
            // text contains deliberately invalid code
            var wasIgnoringFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            string yarnProjectName = Path.GetRandomFileName();
            string yarnProjectPath = $"Assets/{yarnProjectName}.yarnproject";
            createdFilePaths.Add(yarnProjectPath);

            var project = YarnEditorUtility.CreateYarnProject(yarnProjectPath) as YarnProject;
            var yarnProjectImporter = AssetImporter.GetAtPath(yarnProjectPath) as YarnProjectImporter;

            var pathsToAdd = new List<string>();

            foreach (var scriptText in yarnScriptText)
            {

                string yarnScriptName = Path.GetRandomFileName();
                string yarnScriptPath = $"Assets/{yarnScriptName}.yarn";
                createdFilePaths.Add(yarnScriptPath);
                pathsToAdd.Add(yarnScriptPath);

                string textToWrite;

                if (string.IsNullOrEmpty(scriptText))
                {
                    textToWrite = $"title: {yarnScriptName.Replace(".", "_")}\n---\n===\n";
                }
                else
                {
                    textToWrite = scriptText;
                }

                File.WriteAllText(yarnScriptPath, textToWrite);
            }

            // Import all these files
            AssetDatabase.Refresh();

            foreach (var path in pathsToAdd)
            {
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

                // We should have a text asset, imported by a YarnImporter
                Assert.IsNotNull(textAsset);
                Assert.IsInstanceOf<YarnImporter>(AssetImporter.GetAtPath(path));

                // Make the yarn project use this script
                yarnProjectImporter.sourceScripts.Add(textAsset);
            }

            // Reimport the project to make it set up the links
            EditorUtility.SetDirty(yarnProjectImporter);
            yarnProjectImporter.SaveAndReimport();

            foreach (var path in pathsToAdd)
            {
                var scriptImporter = AssetImporter.GetAtPath(path) as YarnImporter;

                Assert.AreSame(project, scriptImporter.DestinationProject);
            }

            // As a final check, make sure the project is referencing the
            // right number of scripts
            Assert.AreEqual(yarnScriptText.Length, yarnProjectImporter.sourceScripts.Count);

            LogAssert.ignoreFailingMessages = wasIgnoringFailingMessages;

            yarnProject = project;
        }
    }
}
