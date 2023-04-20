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

        public static string TestFolderName => TestContext.CurrentContext.Test.FullName;
        public static string TestFilesDirectoryPath => $"Assets/{TestFolderName}/";

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


        internal static void SetupYarnProject(string[] yarnScriptText, Yarn.Compiler.Project projectData, out YarnProject yarnProject)
        {
            // Disable errors causing failures, in case the yarn script
            // text contains deliberately invalid code
            var wasIgnoringFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            // Write the scripts first, and then write the project - that way,
            // the project will detect its scripts on its first import, and the
            // YarnImporter won't need to reimport the Yarn Project

            var pathsToAdd = new List<string>();

            int fileCount = 1;

            foreach (var scriptText in yarnScriptText)
            {
                string yarnScriptName = $"YarnScript{fileCount}";
                fileCount += 1;

                string yarnScriptPath = $"{TestFilesDirectoryPath}/{yarnScriptName}.yarn";
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

            // Now create and import the project

            string yarnProjectPath = $"{TestFilesDirectoryPath}/Project.yarnproject";
            
            var project = YarnEditorUtility.CreateYarnProject(yarnProjectPath, projectData) as YarnProject;
            var yarnProjectImporter = AssetImporter.GetAtPath(yarnProjectPath) as YarnProjectImporter;

            foreach (var path in pathsToAdd)
            {
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

                // We should have a text asset, imported by a YarnImporter
                Assert.IsNotNull(textAsset);
                AssetImporter actual = AssetImporter.GetAtPath(path);
                Assert.IsInstanceOf<YarnImporter>(actual);

                var scriptImporter = AssetImporter.GetAtPath(path) as YarnImporter;
                
                // The created script should have the newly-created project in its destinations list
                Assert.True(scriptImporter.DestinationProjects.Contains(project));
            }

            // As a final check, make sure the project is referencing the
            // right number of scripts
            Assert.AreEqual(yarnScriptText.Length, yarnProjectImporter.ImportData.yarnFiles.Count);

            LogAssert.ignoreFailingMessages = wasIgnoringFailingMessages;

            yarnProject = project;
        }
    }
}
