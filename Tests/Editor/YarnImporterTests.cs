using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace Yarn.Unity.Tests
{
    public class YarnImporterTests
    {
        // A sample Yarn script that we'll store in an asset as part of
        // these tests
        private const string TestYarnScriptSource = @"title: Start
tags:
colorID: 0
position: 0,0
--- 
Spieler: Kannst du mich hören? #line:0e3dc4b
NPC: Klar und deutlich. #line:0967160
[[Mir reicht es.| Exit]] #line:04e806e
[[Nochmal!|Start]] #line:0901fb2
===
title: Exit
tags: 
colorID: 0
position: 0,0
--- 
===";

        private static List<StringTableEntry> GetExpectedStrings(string fileName)
        {
            return new List<StringTableEntry>() {
                new StringTableEntry {
                    Language = YarnImporter.DefaultLocalizationName,
                    ID = "line:0e3dc4b",
                    Text = "Spieler: Kannst du mich hören?",
                    File = fileName,
                    Node = "Start",
                    LineNumber = "6",
                },
                new StringTableEntry {
                    Language = YarnImporter.DefaultLocalizationName,
                    ID = "line:0967160",
                    Text = "NPC: Klar und deutlich.",
                    File = fileName,
                    Node = "Start",
                    LineNumber = "7",
                },
                new StringTableEntry {
                    Language = YarnImporter.DefaultLocalizationName,
                    ID = "line:04e806e",
                    Text = "Mir reicht es.",
                    File = fileName,
                    Node = "Start",
                    LineNumber = "8",
                },
                new StringTableEntry {
                    Language = YarnImporter.DefaultLocalizationName,
                    ID = "line:0901fb2",
                    Text = "Nochmal!",
                    File = fileName,
                    Node = "Start",
                    LineNumber = "9",
                }
            };
        }

        // The files that a test created, stored as paths. These files are
        // deleted in TearDown.
        List<string> createdFilePaths = new List<string>();

        [SetUp]
        public void Setup()
        {
            createdFilePaths.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var path in createdFilePaths)
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

        [Test]
        public void YarnImporter_OnValidYarnFile_ShouldCompile()
        {
            const string textYarnAsset = "title: Start\ntags:\ncolorID: 0\nposition: 0,0\n--- \nSpieler: Kannst du mich hören? #line:0e3dc4b\nNPC: Klar und deutlich. #line:0967160\n[[Mir reicht es.| Exit]] #line:04e806e\n[[Nochmal!|Start]] #line:0901fb2\n===\ntitle: Exit\ntags: \ncolorID: 0\nposition: 0,0\n--- \n===";
            string fileName = Path.GetRandomFileName();

            var path = Application.dataPath + "/" + fileName + ".yarn";
            createdFilePaths.Add(path);

            File.WriteAllText(path, textYarnAsset);
            AssetDatabase.Refresh();
            var result = AssetImporter.GetAtPath("Assets/" + fileName + ".yarn") as YarnImporter;

            Assert.That(result.isSuccesfullyCompiled);

        }

        [Test]
        public void YarnImporter_OnInvalidYarnFile_ShouldNotCompile()
        {
            const string textYarnAsset = "This is not a valid yarn file and thus compilation should fail.";
            string fileName = Path.GetRandomFileName();

            string path = Application.dataPath + "/" + fileName + ".yarn";
            createdFilePaths.Add(path);
            File.WriteAllText(path, textYarnAsset);

            LogAssert.ignoreFailingMessages = true;
            AssetDatabase.Refresh();
            LogAssert.ignoreFailingMessages = false;
            var result = AssetImporter.GetAtPath("Assets/" + fileName + ".yarn") as YarnImporter;

            Assert.That(!result.isSuccesfullyCompiled);
        }

        [Test]
        public void YarnImporter_OnValidYarnFile_GetExpectedStrings()
        {
            string fileName = Path.GetRandomFileName();
            List<StringTableEntry> expectedStrings = GetExpectedStrings(fileName);

            string path = Application.dataPath + "/" + fileName + ".yarn";
            createdFilePaths.Add(path);

            File.WriteAllText(path, TestYarnScriptSource);
            AssetDatabase.Refresh();
            var result = AssetImporter.GetAtPath("Assets/" + fileName + ".yarn") as YarnImporter;

            // Importing this Yarn script will have produced a CSV
            // TextAsset containing the string table extracted from this
            // script. Parse that - we'll check that it contains what we
            // expect.
            var generatedStringsTable = StringTableEntry.ParseFromCSV(result.baseLanguage.text);

            // Simplify the results so that we can compare these string
            // table entries based only on specific fields
            System.Func<StringTableEntry, (string id, string text)> simplifier = e => (id: e.ID, text: e.Text);
            var simpleResult = expectedStrings.Select(simplifier);
            var simpleExpected = generatedStringsTable.Select(simplifier);

            Assert.AreEqual(simpleExpected, simpleResult);
        }

        [Test]
        public void YarnImporterUtility_CanCreateNewLocalizationDatabase()
        {
            // Arrange: Import a yarn script
            string fileName = Path.GetRandomFileName();

            string path = Application.dataPath + "/" + fileName + ".yarn";
            createdFilePaths.Add(path);

            File.WriteAllText(path, TestYarnScriptSource);
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath("Assets/" + fileName + ".yarn") as YarnImporter;
            var serializedObject = new SerializedObject(importer);

            var localizationDatabaseAfterImport = importer.localizationDatabase;

            var expectedStringIDs = GetExpectedStrings(fileName);

            // Act: create a new localization database. 
            YarnImporterUtility.CreateNewLocalizationDatabase(serializedObject);

            // Assert: Verify that the new localization database exists,
            // and contains a single localization, and that localization
            // contains the string table entries we expect.
            Assert.NotNull(importer.localizationDatabase, "Importer should have a localization database");
            createdFilePaths.Add(AssetDatabase.GetAssetPath(importer.localizationDatabase));

            var db = importer.localizationDatabase;
            Assert.AreEqual(1, db.Localizations.Count(), "Localization database should have a single localization");
            createdFilePaths.Add(AssetDatabase.GetAssetPath(importer.localizationDatabase.Localizations.First()));

            Assert.Null(localizationDatabaseAfterImport, "The script should not have a localization database after initial creation");

            var localization = db.Localizations.First();
            Assert.AreEqual(localization.LocaleCode, importer.baseLanguageID, "Localization locale should match script's language");

            Assert.Contains(importer.baseLanguageID, ProjectSettings.TextProjectLanguages, "Script language should be present in the project language settings");

        }

        [Test]
        public void YarnImporterUtility_CanCreateLocalizationInLocalizationDatabase()
        {
            // Arrange: Run YarnImporterUtility_CanCreateNewLocalizationDatabase)

            // Act: Create a new localization for a new language

            // Assert: Verify that it exists, contains the string table
            // entries we expect, and has the language we expect.

            throw new System.NotImplementedException();
        }

        [Test]
        public void YarnImporterUtility_CanUpdateLocalizedCSVs_WhenBaseScriptChanges()
        {
            // Arrange: Run
            // YarnImporterUtility_CanCreateLocalizationInLocalizationDatabase,
            // modify the imported script so that lines are added, changed
            // and deleted, reimport

            // Act: update the localized CSV programmatically

            // Assert: verify the base language string table contains the
            // string table entries we expect, verify the localized string
            // table contains the string table entries we expect

            throw new System.NotImplementedException();
        }

        [Test]
        public void YarnEditorUtility_HasValidEditorResources()
        {

            // Test that YarnEditorUtility can locate the editor assets
            Assert.IsNotNull(YarnEditorUtility.GetYarnDocumentIconTexture());
            Assert.IsNotNull(YarnEditorUtility.GetTemplateYarnScriptPath());
        }
    }
}
