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

        List<string> createdFilePaths = new List<string>();

        [TearDown]
        public void TearDown() {
            foreach (var path in createdFilePaths) {
                File.Delete(path);

                string metaFilePath = path + ".meta";

                if (File.Exists(metaFilePath)) {
                    File.Delete(metaFilePath);
                }
            }
        }

        [Test]
        public void YarnImporter_OnValidYarnFile_ShouldCompile()
        {
            string fileName = Path.GetRandomFileName();

            var path = Application.dataPath + "/" + fileName + ".yarn";
            createdFilePaths.Add(path);

            File.WriteAllText(path, TestYarnScriptSource);
            AssetDatabase.Refresh();
            var result = ScriptedImporter.GetAtPath("Assets/" + fileName + ".yarn") as YarnImporter;

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
            var result = ScriptedImporter.GetAtPath("Assets/" + fileName + ".yarn") as YarnImporter;

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
            var result = ScriptedImporter.GetAtPath("Assets/" + fileName + ".yarn") as YarnImporter;

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
        public void YarnEditorUtility_HasValidEditorResources()
        {

            // Test that YarnEditorUtility can locate the editor assets
            Assert.IsNotNull(YarnEditorUtility.GetYarnDocumentIconTexture());
            Assert.IsNotNull(YarnEditorUtility.GetTemplateYarnScriptPath());
        }
    }
}
