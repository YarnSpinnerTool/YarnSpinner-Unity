using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;

#if USE_UNITY_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEditor.Localization;
#endif

using NUnit.Framework;

using Yarn.Unity;
using Yarn.Unity.Editor;

#nullable enable

namespace Yarn.Unity.Tests
{

#if USE_UNITY_LOCALIZATION
    [TestFixture]
    public class UnityLocalisationImportTests : IPrebuildSetup, IPostBuildCleanup
    {
        static string TestFolderName = typeof(UnityLocalisationImportTests).Name;
        static string AssetPath = $"Assets/{TestFolderName}/";

        string[] aLines =
        {
            "This is the first implicit line in YarnA",
            "This is the second implicit line in YarnA",
        };
        string[] bLines =
        {
            "YarnB: This is the first implict line",
            "YarnB: This is the second implicit line"
        };

        string[] AllLines => aLines.Concat(bLines).ToArray();

        LocalizationSettings? oldSettings;

        public void Setup()
        {
            // Ensure that Unity Localization is set up - localizations settings
            // exist, locales are created, etc
            var setup = new Yarn.Unity.Editor.UnityLocalizationSetupStep(AssetPath);
            setup.RunSetup();

            // Now we create the string table collection for this test
            var tableCollection = LocalizationEditorSettings.CreateStringTableCollection("testcollection", AssetPath);
            AssetDatabase.SaveAssets();

            // now to configure our projects to use the tables
            CreateAndConfigureProject(aLines, "YarnA", AssetPath, "ProjectA", tableCollection);
            CreateAndConfigureProject(bLines, "YarnB", AssetPath, "ProjectB", tableCollection);
        }

        private static void CreateAndConfigureProject(string[] lines, string yarnName, string assetPath, string projectName, StringTableCollection tableCollection)
        {
            List<string> framing = new List<string>
            {
                $"title: {yarnName}",
                "---",
                "==="
            };
            framing.InsertRange(2, lines);
            string[] nodes = { string.Join("\n", framing) };

            YarnProject proj;
            var newPaths = YarnTestUtility.SetupYarnProject(nodes, new Compiler.Project(), assetPath, projectName, false, out proj);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(proj)) as YarnProjectImporter;

            importer.Should().NotBeNull();

            // setting the path to use *only* the files for this project
            var project = importer!.GetProject();
            project.Should().NotBeNull();

            project!.SourceFilePatterns = newPaths.Select(p => $"./{Path.GetFileName(p)}");
            project.BaseLanguage = "en";
            project.SaveToFile($"{AssetPath}/{projectName}.yarnproject");

            // making it use the tables we made
            importer.UseUnityLocalisationSystem = true;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(tableCollection, out var guid, out long _).Should().BeTrue();
            importer.unityLocalisationStringTableCollectionGUID = guid;

            // flagging it as needing save and reimport
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        public void Cleanup()
        {
            // delete the assets we made
            AssetDatabase.DeleteAsset(AssetPath);
            AssetDatabase.Refresh();
        }

        public UnityEngine.Localization.Tables.StringTable ValidateSetup()
        {
            var projectA = AssetImporter.GetAtPath($"{AssetPath}/ProjectA.yarnproject") as YarnProjectImporter;
            projectA.Should().NotBeNull();

            var projectB = AssetImporter.GetAtPath($"{AssetPath}/ProjectB.yarnproject") as YarnProjectImporter;
            projectB.Should().NotBeNull();

            projectA!.UnityLocalisationStringTableCollection.Should().NotBeNull();

            // A and B use the same table so we just grab either of them
            var table = projectA.UnityLocalisationStringTableCollection!.StringTables.First(t => t.LocaleIdentifier == "en");
            // and we need it to not be null
            table.Should().NotBeNull();

            return table;
        }

        [Test]
        public void UnityLocalisation_ImplicitStringsImportedCorrectly()
        {
            var table = ValidateSetup();

            table.Should().HaveCount(AllLines.Count(), "the table should have the same number of lines as our projects have");

            // each value in the table is one of our lines
            foreach (var line in AllLines)
            {
                table.Values.Should().Contain(kv => kv.Value == line, $"the table should contain the line {line}");
            }
        }

        [Test]
        public void UnityLocalisation_FormerImplictLinesAreRemovedFromStringTables()
        {
            var table = ValidateSetup();
            table.Should().HaveCount(AllLines.Count(), "all lines should be present in the string table");

            var projectA = AssetImporter.GetAtPath($"{AssetPath}/ProjectA.yarnproject") as YarnProjectImporter;
            projectA.Should().NotBeNull();
            // now we tag the yarn
            YarnProjectUtility.AddLineTagsToFilesInYarnProject(projectA!);

            // and now we make sure it correctly added and removed the lines

            // the number of lines shouldn't have changed
            table.Should().HaveCount(AllLines.Count(), "the number of lines in the table should not have changed after adding explicit line tags");

            // each value in the table is one of our lines
            foreach (var line in AllLines)
            {
                table.Values.Should().Contain(kv => kv.Value == line, $"the table should contain the line {line}");
            }
        }
    }
#else
    [Ignore("Unity Localisation is not installed.")]
    public class UnityLocalisationImportTests
    {
        public void UnityLocalisation_UnityLocalisationPackageInstalled()
        {
            Assert.Fail("Unity Localisation package is not installed, tests cannot continue");
        }
    }
#endif
}
