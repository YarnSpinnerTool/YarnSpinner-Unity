using System.Collections.Generic;
using System.IO;
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
            const string textYarnAsset = "title: Start\ntags:\ncolorID: 0\nposition: 0,0\n--- \nSpieler: Kannst du mich hören? #line:0e3dc4b\nNPC: Klar und deutlich. #line:0967160\n[[Mir reicht es.| Exit]] #line:04e806e\n[[Nochmal!|Start]] #line:0901fb2\n===\ntitle: Exit\ntags: \ncolorID: 0\nposition: 0,0\n--- \n===";
            string fileName = Path.GetRandomFileName();

            var path = Application.dataPath + "/" + fileName + ".yarn";
            createdFilePaths.Add(path);

            File.WriteAllText(path, textYarnAsset);
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
            const string textYarnAsset = "title: Start\ntags:\ncolorID: 0\nposition: 0,0\n--- \nSpieler: Kannst du mich hören? #line:0e3dc4b\nNPC: Klar und deutlich. #line:0967160\n[[Mir reicht es.| Exit]] #line:04e806e\n[[Nochmal!|Start]] #line:0901fb2\n===\ntitle: Exit\ntags: \ncolorID: 0\nposition: 0,0\n--- \n===";
            string fileName = Path.GetRandomFileName();
            string path = Application.dataPath + "/" + fileName + ".yarn";
            createdFilePaths.Add(path);

            File.WriteAllText(path, textYarnAsset);
            AssetDatabase.Refresh();
            var result = ScriptedImporter.GetAtPath("Assets/" + fileName + ".yarn") as YarnImporter;

            Assert.That(Equals(result.baseLanguage.text, expectedStringTable));

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
