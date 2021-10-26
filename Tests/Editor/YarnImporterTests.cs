using System.Collections;
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
    public class YarnImporterTests
    {

        private static DefaultAsset GetFolder(string directoryName)
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

        private static TextAsset GetScriptSource(string fileName)
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
        private static string TestYarnScriptSource => GetScriptSource("TestYarnScript.yarn").text;

        private static string TestYarnProgramSource => GetScriptSource("TestYarnProject.yarnproject").text;

        private static string TestYarnScriptSourceModified => GetScriptSource("TestYarnScript-Modified.yarn").text;

        private static IEnumerable<StringTableEntry> ExpectedStrings => StringTableEntry.ParseFromCSV(GetScriptSource("TestYarnProject-Strings.csv").text);

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

        // Sets up a YarnProject, and as many yarn scripts as there are
        // parameters. All files will have random filenames.
        public YarnProject SetUpProject(params string[] yarnScriptText)
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

            return project;
        }

        /// <summary>
        /// Formats a string with a random file name, registers that path
        /// as a created file, and returns the formatted value.
        /// </summary>
        /// <param name="template">A format string compatible with <see
        /// cref="string.Format(string, object)"/> </param>
        /// <returns>A path to a file that will be deleted when the unit
        /// test is torn down.</returns>
        private string GetRandomFilePath(string template = "Assets/{0}")
        {
            var scriptPath = string.Format(template, Path.GetRandomFileName());
            createdFilePaths.Add(scriptPath);
            return scriptPath;
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

            Assert.True(result.isSuccessfullyParsed);
            Assert.False(YarnPreventPlayMode.HasCompileErrors(), "Should not have compiler errors");
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

            Assert.False(result.isSuccessfullyParsed);
            Assert.True(YarnPreventPlayMode.HasCompileErrors(), "Should have compiler errors");

            AssetDatabase.DeleteAsset(result.assetPath);
            AssetDatabase.Refresh();

            Assert.False(YarnPreventPlayMode.HasCompileErrors(), "Should not have compiler errors after deletion");
        }

        [Test]
        public void YarnProjectImporter_OnValidYarnFile_ImportsAndCompilesSuccessfully()
        {
            // Arrange: 
            // Set up a Yarn project and a Yarn script.
            var project = SetUpProject("");

            var yarnProjectImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;
            var scriptImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(yarnProjectImporter.sourceScripts.First())) as YarnImporter;

            // Assert:
            // The Yarn project has a reference to the Yarn script. They
            // all report no compilation errors.
            Assert.IsEmpty(scriptImporter.parseErrorMessages);
            Assert.IsEmpty(yarnProjectImporter.compileErrors);
            Assert.True(scriptImporter.isSuccessfullyParsed);
            Assert.AreSame(project, scriptImporter.DestinationProject);

            Assert.False(YarnPreventPlayMode.HasCompileErrors(), "Should show compiler errors");
        }

        [Test]
        public void YarnProjectImporter_OnInvalidYarnFile_ImportsButDoesNotCompile()
        {

            // Arrange: 
            // Set up a Yarn project and a Yarn script, with invalid code.
            var project = SetUpProject("This is invalid yarn script, and will not compile.");

            var yarnProjectImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;
            var scriptImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(yarnProjectImporter.sourceScripts.First())) as YarnImporter;

            // Assert:
            // The Yarn script will fail to compile, and both the script
            // and the project will know about this, but they otherwise
            // have correct asset references to each other.
            Assert.IsNotEmpty(scriptImporter.parseErrorMessages);
            Assert.AreSame(scriptImporter.DestinationProject, project);
            Assert.AreSame(project, scriptImporter.DestinationProject);

            Assert.True(YarnPreventPlayMode.HasCompileErrors(), "Should show compiler errors");
        }

        [Test]
        public void YarnProjectImporter_OnValidYarnFileWithNoLineTags_CannotGetStrings()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(@"title: Demo
---
This script contains lines that are tagged... #line:tagged_line
But not all of them are.
===
");
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            // Assert: 
            // The project cannot generate a strings table.
            Assert.IsFalse(importer.CanGenerateStringsTable);
        }

        [Test]
        public void YarnProjectImporter_OnValidYarnFile_GetExpectedStrings()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            // Act: 
            // Get the strings table from this project.
            var generatedStringsTable = importer.GenerateStringsTable();

            // Simplify the results so that we can compare these string
            // table entries based only on specific fields
            System.Func<StringTableEntry, (string id, string text)> simplifier = e => (id: e.ID, text: e.Text);
            var simpleExpected = ExpectedStrings.Select(simplifier);
            var simpleResult = generatedStringsTable.Select(simplifier);

            // Assert:
            // The two string tables should be identical.

            Assert.AreEqual(simpleExpected, simpleResult);
        }

        [Test]
        public void YarnProjectImporter_OnImport_CreatesLocalizations()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            // Assert:
            // The project has a base localization, and no other
            // localizations. The base localization contains the expected
            // line IDs.
            Assert.IsNotNull(project.baseLocalization);
            Assert.AreEqual(1, project.localizations.Count());
            Assert.AreSame(project.baseLocalization, project.localizations[0]);
            CollectionAssert.AreEquivalent(project.baseLocalization.GetLineIDs(), ExpectedStrings.Select(l => l.ID));
        }

        [Test]
        public void YarnProjectUtility_OnGeneratingLinesFile_CreatesFile()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            var destinationStringsFilePath = "Assets/" + Path.GetRandomFileName() + ".csv";

            // Act:
            // Create a .CSV File, and add it to the Yarn project. 
            YarnProjectUtility.WriteStringsFile(destinationStringsFilePath, importer);
            createdFilePaths.Add(destinationStringsFilePath);
            AssetDatabase.Refresh();

            var stringsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(destinationStringsFilePath);
            importer.languagesToSourceAssets.Add(new YarnProjectImporter.LanguageToSourceAsset { languageID = "test", stringsFile = stringsAsset });
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Assert:
            // A new localization, based on the .csv file we just created,
            // should be present.
            Assert.IsNotNull(project.baseLocalization);
            Assert.IsNotEmpty(project.localizations);
            Assert.AreEqual("test", project.localizations[0].LocaleCode);
            CollectionAssert.AreEquivalent(project.localizations[0].GetLineIDs(), ExpectedStrings.Select(l => l.ID));
        }

        [Test]
        public void YarnImporterUtility_CanUpdateLocalizedCSVs_WhenBaseScriptChanges()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;
            var scriptPath = AssetDatabase.GetAssetPath(importer.sourceScripts[0]);

            var destinationStringsFilePath = "Assets/" + Path.GetRandomFileName() + ".csv";

            // Act:
            // Create a .CSV File, and add it to the Yarn project. 
            YarnProjectUtility.WriteStringsFile(destinationStringsFilePath, importer);
            createdFilePaths.Add(destinationStringsFilePath);

            AssetDatabase.Refresh();

            var stringsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(destinationStringsFilePath);
            importer.languagesToSourceAssets.Add(new YarnProjectImporter.LanguageToSourceAsset { languageID = "test", stringsFile = stringsAsset });
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Capture the strings tables. We'll use them later.
            var unmodifiedBaseStringsTable = importer.GenerateStringsTable();
            var unmodifiedLocalizedStringsTable = StringTableEntry.ParseFromCSV(File.ReadAllText(destinationStringsFilePath));

            // Next, modify the original source script.
            File.WriteAllText(scriptPath, TestYarnScriptSourceModified);

            AssetDatabase.Refresh();

            // Finally, update the CSV.
            LogAssert.Expect(LogType.Log, $"Updated the following files: {destinationStringsFilePath}");
            YarnProjectUtility.UpdateLocalizationCSVs(importer);

            AssetDatabase.Refresh();

            // Doing it again should result in a no-op.
            LogAssert.Expect(LogType.Log, "No files needed updating.");
            YarnProjectUtility.UpdateLocalizationCSVs(importer);

            // Capture the updated strings tables, so we can compare them.
            var modifiedBaseStringsTable = importer.GenerateStringsTable();
            var modifiedLocalizedStringsTable = StringTableEntry.ParseFromCSV(File.ReadAllText(destinationStringsFilePath));

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
        public void YarnProjectImporter_OnNoLocalizationsSupplied_GeneratesExpectedLocalizations()
        {

            // Arrange: 
            // A project with a yarn script, configured with a known
            // default language.
            const string defaultLanguage = "de";

            var project = SetUpProject(YarnImporterTests.TestYarnScriptSource);

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            importer.defaultLanguage = defaultLanguage;

            // Act: 
            // No further steps are taken besides re-importing it.
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Assert:
            // A single localization exists, with the default language.
            Assert.AreEqual(1, project.localizations.Count);
            Assert.AreSame(project.baseLocalization, project.localizations.First());

            Assert.NotNull(project.baseLocalization);

            var allAssetsAtPath = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(project));

            // Two assets: the project, and the localization
            Assert.AreEqual(2, allAssetsAtPath.Count())
            ;
            // The localizations that were imported are the same as the
            // localizations the asset knows about
            Assert.AreSame(project.baseLocalization, allAssetsAtPath.OfType<Localization>().First());
        }

        [Test]
        public void YarnProjectImporter_OnLocalizationsSuppliedButNotDefaultLanguage_GeneratesExpectedLocalizations()
        {
            // Arrange: 
            // A project with a yarn script, configured with a known
            // default language.
            const string defaultLanguage = "de";
            const string otherLanguage = "en";

            var project = SetUpProject(YarnImporterTests.TestYarnScriptSource);

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            importer.defaultLanguage = defaultLanguage;

            // Act:
            // Configure this importer to have a localization that:
            // - is not the same language as the default language" 
            // - has a strings file
            importer.languagesToSourceAssets.Add(new YarnProjectImporter.LanguageToSourceAsset
            {
                languageID = otherLanguage,
                stringsFile = GetScriptSource("TestYarnProject-Strings.csv")
            });

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Assert:
            // Two localizations exist: one for the default localization,
            // and one for the additional language. Because we didn't
            // define a localization for the default language, an
            // 'implicit' localization was created. Both contain the same
            // lines.
            Assert.AreEqual(2, project.localizations.Count);

            var defaultLocalization = project.localizations.First(l => l.LocaleCode == defaultLanguage);
            var otherLocalization = project.localizations.First(l => l.LocaleCode == otherLanguage);

            Assert.NotNull(defaultLocalization);
            Assert.NotNull(otherLocalization);

            Assert.AreNotSame(defaultLocalization, otherLocalization);
            Assert.AreSame(defaultLocalization, project.baseLocalization);

            CollectionAssert.AreEquivalent(defaultLocalization.GetLineIDs(), otherLocalization.GetLineIDs());

            var allAssetsAtPath = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(project));

            // Three assets: the project, and the two localizations
            Assert.AreEqual(3, allAssetsAtPath.Count());
            // The localizations that were imported are the same as the
            // localizations the asset knows about
            CollectionAssert.AreEquivalent(project.localizations, allAssetsAtPath.OfType<Localization>());
        }

        [Test]
        public void YarnProjectImporter_OnLocalizationsSuppliedIncludingDefaultLanguage_GeneratesExpectedLocalizations()
        {
            // Arrange: 
            // A project with a yarn script, configured with a known
            // default language.
            const string defaultLanguage = "de";
            const string otherLanguage = "en";

            var project = SetUpProject(YarnImporterTests.TestYarnScriptSource);

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            importer.defaultLanguage = defaultLanguage;

            // Act:
            // Configure this importer to have two localizations.
            // - One that:
            //    - is not the same language as the default language" 
            //    - has a strings file
            // - One that:
            //    - is the same language as the default language" (and
            //      therefore needs no strings file)

            importer.languagesToSourceAssets.Add(new YarnProjectImporter.LanguageToSourceAsset
            {
                languageID = defaultLanguage,
            });

            importer.languagesToSourceAssets.Add(new YarnProjectImporter.LanguageToSourceAsset
            {
                languageID = otherLanguage,
                stringsFile = GetScriptSource("TestYarnProject-Strings.csv")
            });

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Assert: 
            // Two localizations exist: one for the default languageg, and
            // one for the other language. They contain the same lines. We
            // defined two translations, but one of them was the same
            // language as the default, so an implicit "default"
            // localization didn't need to be generated.
            Assert.AreEqual(2, project.localizations.Count);

            var defaultLocalization = project.localizations.First(l => l.LocaleCode == defaultLanguage);
            var otherLocalization = project.localizations.First(l => l.LocaleCode == otherLanguage);

            Assert.NotNull(defaultLocalization);
            Assert.NotNull(otherLocalization);

            Assert.AreNotSame(defaultLocalization, otherLocalization);
            Assert.AreSame(defaultLocalization, project.baseLocalization);

            CollectionAssert.AreEquivalent(defaultLocalization.GetLineIDs(), otherLocalization.GetLineIDs());

            var allAssetsAtPath = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(project));

            // Three assets: the project, and the two localizations
            Assert.AreEqual(3, allAssetsAtPath.Count());
            // The localizations that were imported are the same as the
            // localizations the asset knows about
            CollectionAssert.AreEquivalent(project.localizations, allAssetsAtPath.OfType<Localization>());
        }

        [Test]
        public void YarnProjectImporter_OnLocalizationsConfigured_LocatesAssets()
        {

            // Arrange: 
            // A project with a yarn script, configured with a known
            // default language.
            const string defaultLanguage = "de";

            var project = SetUpProject(YarnImporterTests.TestYarnScriptSource);

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            importer.defaultLanguage = defaultLanguage;

            // Act:
            // Configure this importer to have a localization that:
            // - is the same language as the default language" (and
            //   therefore has no strings file)
            // - has an assets folder to pull from
            importer.languagesToSourceAssets.Add(new YarnProjectImporter.LanguageToSourceAsset
            {
                languageID = defaultLanguage,
                assetsFolder = GetFolder("Editor Test Resources"),
            });

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            // Assert:
            // A single localization exists that contains the loaded assets.

            Assert.AreEqual(1, project.localizations.Count);

            Localization localization = project.localizations[0];
            IEnumerable<AudioClip> allAudioClips = localization.GetLineIDs()
                                                               .Select(id => localization.GetLocalizedObject<AudioClip>(id));

            Assert.AreEqual(defaultLanguage, localization.LocaleCode);
            CollectionAssert.AreEquivalent(ExpectedStrings.Select(l => l.ID), localization.GetLineIDs());
            Assert.AreEqual(ExpectedStrings.Count(), allAudioClips.Count());
            CollectionAssert.AllItemsAreNotNull(allAudioClips);
            CollectionAssert.AllItemsAreUnique(allAudioClips);
        }

        [Test]
        public void YarnImporter_CanCreateNewScript()
        {
            // Arrange:
            // Choose a location where the new asset should be created
            string scriptPath = GetRandomFilePath("Assets/{0}.yarn");

            // Act: 
            // Create the new script
            var scriptAsset = YarnEditorUtility.CreateYarnAsset(scriptPath) as TextAsset;

            // Assert:
            // The new file should be created as a TextAsset, and it should
            // be imported with a YarnImporter.
            var importer = AssetImporter.GetAtPath(scriptPath) as YarnImporter;

            Assert.IsNotNull(scriptAsset);
            Assert.IsNotNull(importer);
        }

        [Test]
        public void YarnImporter_CanCreateNewProjectFromScript() {
            // Arrange:
            // Create a Yarn script.
            var scriptPath = GetRandomFilePath("Assets/{0}.yarn");

            var scriptAsset = YarnEditorUtility.CreateYarnAsset(scriptPath);
            var importer = AssetImporter.GetAtPath(scriptPath) as YarnImporter;

            // Act:
            // Create a Yarn Project from that script.
            var projectPath = YarnProjectUtility.CreateYarnProject(importer);
            createdFilePaths.Add(projectPath);

            // Assert: A new Yarn Project should exist, and is imported as
            // a Yarn Project that has the original Yarn script as one of
            // its source scripts.
            Assert.True(File.Exists(projectPath));

            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(projectPath);
            var projectImporter = AssetImporter.GetAtPath(projectPath) as YarnProjectImporter;

            Assert.IsNotNull(project);
            Assert.IsNotNull(projectImporter);
            Assert.Contains(scriptAsset, projectImporter.sourceScripts);
            Assert.AreSame(project, importer.DestinationProject);
        }

        [Test]
        public void YarnImporter_CanAssignProjectToScript() {
            // Arrange: 
            // Create a new script and a project, independently of each
            // other.
            var scriptPath = GetRandomFilePath("Assets/{0}.yarn");
            var projectPath = GetRandomFilePath("Assets/{0}.yarnproject");

            var scriptAsset = YarnEditorUtility.CreateYarnAsset(scriptPath);
            var scriptImporter = AssetImporter.GetAtPath(scriptPath) as YarnImporter;

            var projectAsset = YarnEditorUtility.CreateYarnProject(projectPath);
            var projectImporter = AssetImporter.GetAtPath(projectPath) as YarnProjectImporter;

            Assert.IsNull(scriptImporter.DestinationProject);
            Assert.IsEmpty(projectImporter.sourceScripts);

            // Act:
            // Assign the script to the project.
            YarnProjectUtility.AssignScriptToProject(scriptPath, projectPath);

            // Assert:
            // The script should now be part of the destination project.
            Assert.AreSame(scriptImporter.DestinationProject, projectAsset);
            CollectionAssert.AreEquivalent(projectImporter.sourceScripts, new[] { scriptAsset });


        }

        [Test]
        public void YarnImporter_CanReassignDifferentProjectToScript() {
            // Arrange: 
            // Create a new script and two projects, independently of each
            // other.
            var scriptPath = GetRandomFilePath("Assets/{0}.yarn");
            var project1Path = GetRandomFilePath("Assets/1-{0}.yarnproject");
            var project2Path = GetRandomFilePath("Assets/2-{0}.yarnproject");

            var scriptAsset = YarnEditorUtility.CreateYarnAsset(scriptPath);
            var scriptImporter = AssetImporter.GetAtPath(scriptPath) as YarnImporter;

            var _ = YarnEditorUtility.CreateYarnProject(project1Path);
            var project1Importer = AssetImporter.GetAtPath(project1Path) as YarnProjectImporter;

            var project2Asset = YarnEditorUtility.CreateYarnProject(project2Path);
            var project2Importer = AssetImporter.GetAtPath(project2Path) as YarnProjectImporter;

            Assert.IsNull(scriptImporter.DestinationProject);
            Assert.IsEmpty(project1Importer.sourceScripts);
            Assert.IsEmpty(project2Importer.sourceScripts);

            // Act:
            // Assign the script to the project, and then assign it to a
            // different project.
            YarnProjectUtility.AssignScriptToProject(scriptPath, project1Path);
            YarnProjectUtility.AssignScriptToProject(scriptPath, project2Path);

            project1Importer = AssetImporter.GetAtPath(project1Path) as YarnProjectImporter;

            // Assert:
            // The script should now be part of the second project, and not the first.
            Assert.IsEmpty(project1Importer.sourceScripts);
            Assert.AreSame(scriptImporter.DestinationProject, project2Asset);
            CollectionAssert.AreEquivalent(project2Importer.sourceScripts, new[] { scriptAsset });


        }
    }
}
