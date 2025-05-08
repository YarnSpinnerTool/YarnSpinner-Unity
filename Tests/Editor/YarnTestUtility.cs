/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity.Editor;

#nullable enable

namespace Yarn.Unity.Tests
{
    /// <summary>
    /// Provides utility methods for testing Yarn Projects and scripts.
    /// </summary>
    public static class YarnTestUtility
    {
        /// <summary>
        /// Gets the folder name of the current test context.
        /// </summary>
        public static string TestFolderName => TestContext.CurrentContext.Test.FullName;

        /// <summary>
        /// Gets the path to the directory containing the test files.
        /// </summary>
        public static string TestFilesDirectoryPath => $"Assets/{TestFolderName}/";

        /// <summary>
        /// Retrieves a <see cref="DefaultAsset"/> representing a folder from
        /// the asset database that matches the specified folder name.
        /// </summary>
        /// <param name="directoryName">The name of the folder to find.</param>
        /// <returns>The <see cref="DefaultAsset"/> representing the folder, or
        /// <see langword="null"/> if not found.</returns>
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

        /// <summary>
        /// Retrieves a <see cref="TextAsset"/> from the asset database that
        /// matches the specified file name.
        /// </summary>
        /// <param name="fileName">The name of the file to find.</param>
        /// <returns>The <see cref="TextAsset"/> for the file, or <see
        /// langword="null"/> if not found.</returns>
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

        /// <summary>
        /// Gets the source text for the TestYarnScript sample yarn file.
        /// </summary>
        /// <returns>The source text for the script, or <see langword="null"/>
        /// if not found.</returns>
        internal static string TestYarnScriptSource => GetScriptSource("TestYarnScript.yarn").text;

        /// <summary>
        /// Gets the source text for the TestYarnProject sample Yarn Project.
        /// </summary>
        /// <returns>The source text for the project, or <see langword="null"/>
        /// if not found.</returns>
        internal static string TestYarnProgramSource => GetScriptSource("TestYarnProject.yarnproject").text;

        /// <summary>
        /// Gets the source text for the TestYarnScript-Modified yarn file.
        /// </summary>
        /// <returns>The modified source text for the script, or <see
        /// langword="null"/> if not found.</returns>
        internal static string TestYarnScriptSourceModified => GetScriptSource("TestYarnScript-Modified.yarn").text;

        /// <summary>
        /// Gets the source text for the TestYarnScript-NoMetadata yarn file.
        /// </summary>
        /// <returns>The source text for the script, or <see langword="null"/>
        /// if not found.</returns>
        internal static string TestYarnScriptSourceNoMetadata => GetScriptSource("TestYarnScript-NoMetadata.yarn").text;

        /// <summary>
        /// Gets the collection of expected strings from the
        /// TestYarnProject-Strings.csv file.
        /// </summary>
        /// <returns>The expected strings, or <see langword="null"/> if not
        /// found.</returns>
        internal static IEnumerable<StringTableEntry> ExpectedStrings => StringTableEntry.ParseFromCSV(GetScriptSource("TestYarnProject-Strings.csv").text);

        /// <summary>
        /// Gets the collection of expected metadata from the
        /// TestYarnProject-Metadata.csv file.
        /// </summary>
        /// <returns>The expected metadata, or <see langword="null"/> if not
        /// found.</returns>
        internal static IEnumerable<LineMetadataTableEntry> ExpectedMetadata => LineMetadataTableEntry.ParseFromCSV(GetScriptSource("TestYarnProject-Metadata.csv").text);

        /// <summary>
        /// Deletes files and metadata from a list of paths.
        /// </summary>
        /// <param name="paths">The paths to delete.</param>
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

        /// <summary>
        /// Sets up a Yarn Project with the specified script text and data.
        /// </summary>
        /// <param name="yarnScriptText">The source text for the
        /// scripts.</param>
        /// <param name="projectData">The data for the project.</param>
        /// <param name="yarnProject">On return, contains a newly created <see
        /// cref="YarnProject"/> asset that now exists on disk.</param>
        internal static void SetupYarnProject(string[] yarnScriptText, Yarn.Compiler.Project projectData, out YarnProject yarnProject)
        {
            SetupYarnProject(yarnScriptText, projectData, TestFilesDirectoryPath, "Project", true, out yarnProject);
        }

        /// <summary>
        /// Sets up a Yarn Project with the specified script text and data.
        /// </summary>
        /// <param name="yarnScriptText">The source text for the
        /// scripts.</param>
        /// <param name="projectData">A <see cref="Yarn.Compiler.Project"/>
        /// object to write to <paramref name="testFolderPath"/>.</param>
        /// <param name="testFolderPath">The path to the folder in which to
        /// create the files.</param>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="validateCreation">Whether to validate that the file was
        /// created on disk and imported correctly.</param>
        /// <param name="yarnProject">On return, contains a newly created <see
        /// cref="YarnProject"/> asset that now exists on disk.</param>
        /// <returns>A list of paths to files that were created.</returns>
        internal static List<string> SetupYarnProject(
            string[] yarnScriptText,
            Yarn.Compiler.Project projectData,
            string testFolderPath,
            string projectName,
            bool validateCreation,
            out YarnProject yarnProject)
        {
            // Disable errors causing failures, in case the yarn script text
            // contains deliberately invalid code
            var wasIgnoringFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            // Write the scripts first, and then write the project - that way,
            // the project will detect its scripts on its first import, and the
            // YarnImporter won't need to reimport the Yarn Project

            var pathsToAdd = new List<string>();

            int fileCount = 1;

            foreach (var scriptText in yarnScriptText)
            {
                string yarnScriptName = $"YarnScript-{projectName}-{fileCount}";
                fileCount += 1;

                string yarnScriptPath = $"{testFolderPath}/{yarnScriptName}.yarn";
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

            string yarnProjectPath = $"{testFolderPath}/{projectName}.yarnproject";

            var project = YarnEditorUtility.CreateYarnProject(yarnProjectPath, projectData) as YarnProject;
            project.Should().NotBeNull();

            var yarnProjectImporter = AssetImporter.GetAtPath(yarnProjectPath) as YarnProjectImporter;

            foreach (var path in pathsToAdd)
            {
                var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

                if (validateCreation)
                {
                    // We should have a text asset, imported by a YarnImporter
                    Assert.IsNotNull(textAsset);
                    AssetImporter actual = AssetImporter.GetAtPath(path);
                    Assert.IsInstanceOf<YarnImporter>(actual);

                    var scriptImporter = AssetImporter.GetAtPath(path) as YarnImporter;

                    scriptImporter.Should().NotBeNull();


                    // The created script should have the newly-created project
                    // in its destinations list
                    scriptImporter!.DestinationProjects.Should().Contain(project!);
                }
            }

            if (validateCreation)
            {
                // As a final check, make sure the project is referencing the
                // right number of scripts

                yarnProjectImporter.Should().NotBeNull();
                yarnProjectImporter!.ImportData.Should().NotBeNull();

                yarnProjectImporter.ImportData!.yarnFiles.Should().HaveCount(yarnScriptText.Length);

            }

            LogAssert.ignoreFailingMessages = wasIgnoringFailingMessages;

            yarnProject = project!;
            return pathsToAdd;
        }
    }
}
