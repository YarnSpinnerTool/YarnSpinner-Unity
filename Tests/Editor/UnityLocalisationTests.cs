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

namespace Yarn.Unity.Tests
{

#if USE_UNITY_LOCALIZATION
    [TestFixture]
    public class UnityLocalisationTests : IPrebuildSetup, IPostBuildCleanup
    {
        static string TestFolderName = typeof(UnityLocalisationTests).Name;
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

        string[] lines => aLines.Concat(bLines).ToArray();

        LocalizationSettings oldSettings;

        public void Setup()
        {
            // first we need to make a temporary folder to store all these assets
            if (Directory.Exists(AssetPath) == false)
            {
                AssetDatabase.CreateFolder("Assets", TestFolderName);
            }

            // first we are assuming that there is already some localisation settings configured
            // and if not then we will make some
            oldSettings = LocalizationEditorSettings.ActiveLocalizationSettings;
            var settings = oldSettings;
            if (settings == null)
            {
                // we have no existing settings
                // so we will need to make a localisation settings object first
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "Test Localization Settings";
                AssetDatabase.CreateAsset(settings, Path.Combine(AssetPath, "settings.asset"));

                // setting this new settings object to be th global settings for the project
                AssetDatabase.SaveAssets();
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            // we now have a valid settings, but we don't know if it has english locale support
            var localeID = new LocaleIdentifier("en");
            if (LocalizationSettings.AvailableLocales.GetLocale(localeID) == null)
            {
                // we don't have an english locale
                // we need to make one and add it to the settings and on disk
                var locale = Locale.CreateLocale(localeID);
                AssetDatabase.CreateAsset(locale, Path.Combine(AssetPath, "en.asset"));
                AssetDatabase.SaveAssets();

                LocalizationEditorSettings.AddLocale(locale);
            }
            // at this point it is *highly* likely we have dirty assets, so save them.
            AssetDatabase.SaveAssets();

            // now we create the string table collection
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

            // setting the path to use *only* the files for this project
            var a = importer.GetProject();
            a.SourceFilePatterns = newPaths.Select(p => $"./{Path.GetFileName(p)}");
            a.BaseLanguage = "en";
            a.SaveToFile($"{AssetPath}/{projectName}.yarnproject");
            
            // making it use the tables we made
            importer.UseUnityLocalisationSystem = true;
            importer.unityLocalisationStringTableCollection = tableCollection;
            
            // flagging it as needing save and reimport
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        public void Cleanup()
        {
            // put the old settings back
            LocalizationEditorSettings.ActiveLocalizationSettings = oldSettings;

            // delete the assets we made
            AssetDatabase.DeleteAsset(AssetPath);
            AssetDatabase.Refresh();
        }

        public UnityEngine.Localization.Tables.StringTable ValidateSetup()
        {
            var projectA = AssetImporter.GetAtPath($"{AssetPath}/ProjectA.yarnproject") as YarnProjectImporter;
            Assert.NotNull(projectA);

            var projectB = AssetImporter.GetAtPath($"{AssetPath}/ProjectB.yarnproject") as YarnProjectImporter;
            Assert.NotNull(projectB);

            // A and B use the same table so we just grab either of them
            var table = projectA.unityLocalisationStringTableCollection.StringTables.First();
            // and we need it to not be null
            Assert.NotNull(table);

            return table;
        }

        [Test]
        public void UnityLocalisation_ImplicitStringsImportedCorrectly()
        {
            var table = ValidateSetup();

            // and it needs to have the same number of lines as our projects have
            Assert.AreEqual(table.Count(), lines.Count());

            // each value in the table is one of our lines
            foreach (var value in table.Values)
            {
                Assert.That(lines, Contains.Item(value.Value));
            }
        }

        [Test]
        public void UnityLocalisation_FormerImplictLinesAreRemovedFromStringTables()
        {
            var table = ValidateSetup();

            var projectA = AssetImporter.GetAtPath($"{AssetPath}/ProjectA.yarnproject") as YarnProjectImporter;
            // now we tag the yarn
            YarnProjectUtility.AddLineTagsToFilesInYarnProject(projectA);

            // and now we make sure it correctly added and removed the lines

            // the number of lines shouldn't have changed
            Assert.AreEqual(table.Count(), lines.Count());

            // each value in the table is one of our lines
            foreach (var value in table.Values)
            {
                Assert.That(lines, Contains.Item(value.Value));
            }
        }
    }
#else
    public class UnityLocalisationTests
    {
        [Test]
        public void UnityLocalisation_UnityLocalisationPackageInstalled()
        {
            Assert.Fail("Unity Localisation package is not installed, tests cannot continue");
        }
    }
#endif
}
