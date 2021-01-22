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
-> Mir reicht es. #line:04e806e
    <<jump Exit>>
-> Nochmal! #line:0901fb2
    <<jump Start>>
===
title: Exit
tags: 
colorID: 0
position: 0,0
--- 
===";

        // A modified version of TestYarnScriptSource, with the following
        // changes:
        // - A line has been added
        // - A line has been modified
        // - A line has been removed
        private const string TestYarnScriptSourceModified = @"title: Start
tags:
colorID: 0
position: 0,0
--- 
Spieler: Kannst du mich hören? This line was modified. #line:0e3dc4b
-> Mir reicht es. #line:04e806e
    <<jump Exit>>
-> Nochmal! #line:0901fb2
    <<jump Start>>
This line was added. #line:a1b2c3
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

        // Gets a locale code for a language that is not the current base.

        private string AlternateLocaleCode =>
                    new string[] { "en", "de", "zh-cn" } // some languages
                    .First(s => s != Preferences.TextLanguage); // that are not the current one

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
        public void YarnImporter_OnValidYarnFile_ShouldParse()
        {
            string fileName = Path.GetRandomFileName();

            var path = Application.dataPath + "/" + fileName + ".yarn";
            createdFilePaths.Add(path);

            File.WriteAllText(path, TestYarnScriptSource, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            var result = AssetImporter.GetAtPath("Assets/" + fileName + ".yarn") as YarnImporter;

            Assert.True(result.isSuccesfullyParsed);

        }

        [Test]
        public void YarnImporter_OnInvalidYarnFile_ShouldNotParse()
        {
            const string textYarnAsset = "This is not a valid yarn file and thus compilation should fail.";
            string fileName = Path.GetRandomFileName();

            string path = Application.dataPath + "/" + fileName + ".yarn";
            createdFilePaths.Add(path);
            File.WriteAllText(path, textYarnAsset, System.Text.Encoding.UTF8);

            LogAssert.ignoreFailingMessages = true;
            AssetDatabase.Refresh();
            LogAssert.ignoreFailingMessages = false;
            var result = AssetImporter.GetAtPath("Assets/" + fileName + ".yarn") as YarnImporter;

            Assert.False(result.isSuccesfullyParsed);
        }

        [Test]
        public void YarnImporter_OnValidYarnFile_GetExpectedStrings()
        {
            string fileName = Path.GetRandomFileName();
            List<StringTableEntry> expectedStrings = GetExpectedStrings(fileName);

            string path = Application.dataPath + "/" + fileName + ".yarn";
            createdFilePaths.Add(path);

            File.WriteAllText(path, TestYarnScriptSource, System.Text.Encoding.UTF8);
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
            var simpleExpected = expectedStrings.Select(simplifier);
            var simpleResult = generatedStringsTable.Select(simplifier);

            Assert.AreEqual(simpleExpected, simpleResult);
        }

        [Test]
        public void YarnImporterUtility_CanCreateNewLocalizationDatabase()
        {
            // Arrange: Import a yarn script
            string fileName = Path.GetRandomFileName();

            string path = Path.Combine("Assets", fileName + ".yarn");
            createdFilePaths.Add(path);

            File.WriteAllText(path, TestYarnScriptSource, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(path) as YarnImporter;
            var serializedObject = new SerializedObject(importer);

            var localizationDatabaseAfterImport = importer.localizationDatabase;

            // Act: create a new localization database. 
            YarnImporterUtility.CreateNewLocalizationDatabase(serializedObject);

            importer.SaveAndReimport();

            // Assert: Verify that the new localization database exists,
            // and contains a single localization, and that localization
            // contains the string table entries we expect.
            Assert.Null(localizationDatabaseAfterImport, "The script should not have a localization database after initial creation");

            Assert.NotNull(importer.localizationDatabase, "Importer should have a localization database");
            createdFilePaths.Add(AssetDatabase.GetAssetPath(importer.localizationDatabase));

            var db = importer.localizationDatabase;
            Assert.AreEqual(1, db.Localizations.Count(), "Localization database should have a single localization");
            createdFilePaths.Add(AssetDatabase.GetAssetPath(importer.localizationDatabase.Localizations.First()));

            var localization = db.Localizations.First();
            Assert.AreEqual(localization.LocaleCode, importer.baseLanguageID, "Localization locale should match script's language");

            Assert.Contains(importer.baseLanguageID, ProjectSettings.TextProjectLanguages, "Script language should be present in the project language settings");

        }

        [Test]
        public void YarnImporterUtility_CanCreateLocalizationInLocalizationDatabase()
        {
            // Arrange: Import a yarn script and create a localization
            // database for it
            string fileName = Path.GetRandomFileName();
            string path = Path.Combine("Assets", fileName + ".yarn");
            createdFilePaths.Add(path);
            File.WriteAllText(path, TestYarnScriptSource, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(path) as YarnImporter;
            var importerSerializedObject = new SerializedObject(importer);

            YarnImporterUtility.CreateNewLocalizationDatabase(importerSerializedObject);
            createdFilePaths.Add(AssetDatabase.GetAssetPath(importer.localizationDatabase));

            var databaseSerializedObject = new SerializedObject(importer.localizationDatabase);

            // Act: Create a new localization CSV file for some new language
            LocalizationDatabaseUtility.CreateLocalizationWithLanguage(databaseSerializedObject, AlternateLocaleCode);
            YarnImporterUtility.CreateLocalizationForLanguageInProgram(importerSerializedObject, AlternateLocaleCode);

            foreach (var loc in importer.localizationDatabase.Localizations)
            {
                createdFilePaths.Add(AssetDatabase.GetAssetPath(loc));
            }

            foreach (var loc in importer.ExternalLocalizations)
            {
                createdFilePaths.Add(AssetDatabase.GetAssetPath(loc.text));
            }

            importer.SaveAndReimport();

            // Assert: Verify that it exists, contains the string table
            // entries we expect, and has the language we expect.
            var expectedLanguages = new HashSet<string> { importer.baseLanguageID, AlternateLocaleCode }.OrderBy(n => n);
            
            var foundLanguages = importer.AllLocalizations.Select(l => l.languageName).OrderBy(n => n);
            
            CollectionAssert.AreEquivalent(expectedLanguages, foundLanguages, $"The locales should be what we expect to see");
        }

        [Test]
        public void YarnImporterUtility_CanUpdateLocalizedCSVs_WhenBaseScriptChanges()
        {
            // Arrange: Import a yarn script and create a localization
            // database for it, create an alternate localization for it
            string fileName = Path.GetRandomFileName();
            string path = Path.Combine("Assets", fileName + ".yarn");
            createdFilePaths.Add(path);
            File.WriteAllText(path, TestYarnScriptSource, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(path) as YarnImporter;
            var importerSerializedObject = new SerializedObject(importer);            
            var localizationPaths = YarnImporterUtility.CreateNewLocalizationDatabase(importerSerializedObject);

            createdFilePaths.AddRange(localizationPaths);

            var localizationDatabaseSerializedObject = new SerializedObject(importer.localizationDatabase);

            var newLocalizationFilePath = LocalizationDatabaseUtility.CreateLocalizationWithLanguage(localizationDatabaseSerializedObject, AlternateLocaleCode);

            createdFilePaths.Add(newLocalizationFilePath);

            var csvPath = YarnImporterUtility.CreateLocalizationForLanguageInProgram(importerSerializedObject, AlternateLocaleCode);

            createdFilePaths.Add(csvPath);

            var unmodifiedBaseStringsTable = StringTableEntry.ParseFromCSV((importerSerializedObject.targetObject as YarnImporter).baseLanguage.text);
            var unmodifiedLocalizedStringsTable = StringTableEntry.ParseFromCSV((importerSerializedObject.targetObject as YarnImporter).AllLocalizations.First(l => l.languageName == AlternateLocaleCode).text.text);

            // Act: modify the imported script so that lines are added,
            // changed and deleted, and then update the localized CSV
            // programmatically

            File.WriteAllText(path, TestYarnScriptSourceModified, System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            YarnImporterUtility.UpdateLocalizationCSVs(importerSerializedObject);

            var modifiedBaseStringsTable = StringTableEntry.ParseFromCSV((importerSerializedObject.targetObject as YarnImporter).baseLanguage.text);
            var modifiedLocalizedStringsTable = StringTableEntry.ParseFromCSV((importerSerializedObject.targetObject as YarnImporter).AllLocalizations.First(l => l.languageName == AlternateLocaleCode).text.text);

            // Assert: verify the base language string table contains the
            // string table entries we expect, verify the localized string
            // table contains the string table entries we expect

            System.Func<StringTableEntry, string> CompareIDs = t => t.ID;
            System.Func<StringTableEntry, string> CompareLocks = t => t.Lock;

            var tests = new[] {
                (name: "ID", test:CompareIDs),
                (name: "lock", test:CompareLocks),
            };

            // We want to check that both the ID and the lock are the same
            // for the unmodified pair, and the same for the modified pair,
            // but different between unmodified and modified (because lines
            // have been added, removed and changed)
            foreach (var test in tests)
            {
                CollectionAssert.AreEquivalent(unmodifiedBaseStringsTable.Select(test.test), unmodifiedLocalizedStringsTable.Select(test.test), $"The unmodified string table {test.name}s should be equivalent");

                CollectionAssert.AreEquivalent(modifiedBaseStringsTable.Select(test.test), modifiedLocalizedStringsTable.Select(test.test), $"The modified string table {test.name}s should be equivalent");

                CollectionAssert.AreNotEquivalent(unmodifiedBaseStringsTable.Select(test.test), modifiedBaseStringsTable.Select(test.test), $"The unmodified and modified string table {test.name}s should not be equivalent");
            }
        }

        [Test]
        public void YarnEditorUtility_HasValidEditorResources()
        {

            // Test that YarnEditorUtility can locate the editor assets
            Assert.IsNotNull(YarnEditorUtility.GetYarnDocumentIconTexture());
            Assert.IsNotNull(YarnEditorUtility.GetTemplateYarnScriptPath());
        }

        [Test]
        public void YarnImporter_CanCreateYarnProgram()
        {

            string scriptPath = "NewYarnScript.yarn";
            string scriptFullPath = $"Assets/{scriptPath}";
            YarnEditorUtility.CreateYarnAsset(scriptFullPath);
            createdFilePaths.Add(scriptFullPath);
            
            Assert.True(File.Exists(scriptFullPath));

            var scriptImporter = AssetImporter.GetAtPath(scriptFullPath) as YarnImporter;
            
            // The script has no destination program after being created
            Assert.Null(scriptImporter.DestinationProgram);

            // Create a new Yarn Program for this script
            var programPath = YarnImporterUtility.CreateYarnProgram(scriptImporter);
            createdFilePaths.Add(programPath);

            // The script now has a destination program
            Assert.NotNull(scriptImporter.DestinationProgram);

            var programImporter = AssetImporter.GetAtPath(programPath) as YarnProgramImporter;

            var scriptTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(scriptFullPath);

            // The program includes the script importer in its Source Scripts list
            Assert.Contains(scriptTextAsset, new List<TextAsset>(programImporter.sourceScripts));
        }
    }
}
