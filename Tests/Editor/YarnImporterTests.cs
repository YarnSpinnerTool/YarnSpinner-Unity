// #define KEEP_FILES_ON_TEARDOWN

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity.Editor;


namespace Yarn.Unity.Tests
{
    public class YarnImporterTests
    {
        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(Path.Combine("Assets", YarnTestUtility.TestFolderName)) == false) {
                AssetDatabase.CreateFolder("Assets", YarnTestUtility.TestFolderName);
            }
        }

        [TearDown]
        public void TearDown()
        {
#if !KEEP_FILES_ON_TEARDOWN
            AssetDatabase.DeleteAsset(YarnTestUtility.TestFilesDirectoryPath);
#endif
        }


        // Sets up a YarnProject, and as many yarn scripts as there are
        // parameters. All files will have random filenames.
        public YarnProject SetUpProject(params string[] yarnScriptText)
        {
            YarnTestUtility.SetupYarnProject(yarnScriptText, new Compiler.Project(), out var yarnProject);
            return yarnProject;
        }

        public YarnProject SetUpProject(Yarn.Compiler.Project project, params string[] yarnScriptText)
        {
            YarnTestUtility.SetupYarnProject(yarnScriptText, project, out var yarnProject);
            return yarnProject;
        }


        [Test]
        public void YarnProjectImporter_OnValidYarnFile_ImportsAndCompilesSuccessfully()
        {
            // Arrange: 
            // Set up a Yarn project and a Yarn script.
             var project = SetUpProject(new[] {
                string.Join("\n", new[] {
                    "title: Test",
                    "---",
                    "Hello, world!",
                    "===",
                    ""
                })
            });

            var yarnProjectImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;
            var scriptImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(yarnProjectImporter.ImportData.yarnFiles.First())) as YarnImporter;

            // Assert:
            // The Yarn project has a reference to the Yarn script. They
            // all report no compilation errors.
            Assert.IsFalse(yarnProjectImporter.ImportData.HasCompileErrors);
            CollectionAssert.Contains(scriptImporter.DestinationProjectImporters, yarnProjectImporter);
            Assert.False(scriptImporter.HasErrors);

            Assert.AreEqual(ProjectImportData.ImportStatusCode.Succeeded, yarnProjectImporter.ImportData.ImportStatus);
        }

        [Test]
        public void YarnProjectImporter_OnInvalidYarnFile_ImportsButDoesNotCompile()
        {

            // Arrange: 
            // Set up a Yarn project and a Yarn script, with invalid code.
            var project = SetUpProject("This is invalid yarn script, and will not compile.");

            var yarnProjectImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;
            var scriptImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(yarnProjectImporter.ImportData.yarnFiles.First())) as YarnImporter;

            // Assert:
            // The Yarn script will fail to compile, and both the script
            // and the project will know about this, but they otherwise
            // have correct asset references to each other.

            Assert.IsTrue(yarnProjectImporter.ImportData.HasCompileErrors);
            CollectionAssert.Contains(scriptImporter.DestinationProjectImporters, yarnProjectImporter);
            Assert.True(scriptImporter.HasErrors);

            Assert.AreEqual(yarnProjectImporter.ImportData.ImportStatus, ProjectImportData.ImportStatusCode.CompilationFailed);
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
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            // Act: 
            // Get the strings table from this project.
            var generatedStringsTable = importer.GenerateStringsTable();

            // Simplify the results so that we can compare these string
            // table entries based only on specific fields
            System.Func<StringTableEntry, (string id, string text)> simplifier = e => (id: e.ID, text: e.Text);
            var simpleExpected = YarnTestUtility.ExpectedStrings.Select(simplifier);
            var simpleResult = generatedStringsTable.Select(simplifier);

            // Assert:
            // The two string tables should be identical.

            Assert.AreEqual(simpleExpected, simpleResult);
        }

        [Test]
        public void YarnProjectImporter_OnValidYarnFileWithMetadata_GeneratesLineMetadata()
        {
            // Arrange:
            // Set up a project with a Yarn file with metadata in some lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);

            // Assert:
            // Line metadata entry is generated for the project.
            Assert.NotNull(project.lineMetadata);
            Assert.Greater(project.lineMetadata.GetLineIDs().Count(), 0);
        }

        [Test]
        public void YarnProjectImporter_OnValidYarnFileWithoutMetadata_GeneratesEmptyLineMetadata()
        {
            // Arrange:
            // Set up a project with a Yarn file with metadata in some lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSourceNoMetadata);

            // Assert:
            // Line metadata entry is generated for the project.
            Assert.NotNull(project.lineMetadata);
            Assert.IsEmpty(project.lineMetadata.GetLineIDs());
        }

        [Test]
        public void YarnProjectImporter_OnValidYarnFileWithMetadata_GetExpectedLineMetadata()
        {
            // Arrange:
            // Set up a project with a Yarn file with metadata in some lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            // Act:
            var metadataEntries = importer.GenerateLineMetadataEntries();

            // Simplify the results so that we can compare these metadata
            // table entries based only on specific fields.
            System.Func<LineMetadataTableEntry, (string id, string node, string lineNo, string metadata)> simplifier =
                e => (id: e.ID, node: e.Node, lineNo: e.LineNumber, metadata: string.Join(" ", e.Metadata));
            var simpleExpected = YarnTestUtility.ExpectedMetadata.Select(simplifier);
            var simpleResult = metadataEntries.Select(simplifier);

            // Assert:
            // Metadata entries should be identical to what we expect.
            CollectionAssert.AreEquivalent(simpleResult, simpleExpected);
        }

        [Test]
        public void YarnProjectImporter_UpdatesLineMetadata_WhenBaseScriptChanges()
        {
            // Arrange:
            // Set up a project with a Yarn file with metadata in some lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;
            var scriptPath = AssetDatabase.GetAssetPath(importer.ImportData.yarnFiles.First());

            // Act:
            // Modify the original source script.
            File.WriteAllText(scriptPath, YarnTestUtility.TestYarnScriptSourceModified);
            AssetDatabase.Refresh();

            // Assert: verify the line metadata exists and contains the expected number of entries.
            Assert.NotNull(project.lineMetadata);
            Assert.AreEqual(project.lineMetadata.GetLineIDs().Count(), 2);
        }

        [Test]
        public void YarnProjectImporter_UpdatesLineMetadata_WhenBaseScriptChangesWithoutMetadata()
        {
            // Arrange:
            // Set up a project with a Yarn file with metadata in some lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;
            var scriptPath = AssetDatabase.GetAssetPath(importer.ImportData.yarnFiles.First());

            // Act:
            // Modify the original source script to a script without any metadata.
            File.WriteAllText(scriptPath, YarnTestUtility.TestYarnScriptSourceNoMetadata);
            AssetDatabase.Refresh();

            // Assert: verify the line metadata exists and is empty.
            Assert.NotNull(project.lineMetadata);
            Assert.IsEmpty(project.lineMetadata.GetLineIDs());
        }

        [Test]
        public void YarnProjectImporter_OnImport_CreatesLocalizations()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            // Assert:
            // The project has a base localization, and no other
            // localizations. The base localization contains the expected
            // line IDs.
            Assert.IsNotNull(project.baseLocalization);
            Assert.AreEqual(1, project.localizations.Count());
            Assert.AreSame(project.baseLocalization, project.localizations[0]);
            CollectionAssert.AreEquivalent(project.baseLocalization.GetLineIDs(), YarnTestUtility.ExpectedStrings.Select(l => l.ID));
        }

        [Test]
        public void YarnProjectUtility_OnGeneratingLinesFile_CreatesFile()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            string projectFilePath = AssetDatabase.GetAssetPath(project);
            var importer = AssetImporter.GetAtPath(projectFilePath) as YarnProjectImporter;

            var destinationStringsFilePath = Path.Combine(YarnTestUtility.TestFilesDirectoryPath, "OutputStringsFile.csv");

            // Act:
            // Create a .CSV File, and add it to the Yarn project. 
            YarnProjectUtility.WriteStringsFile(destinationStringsFilePath, importer);
            AssetDatabase.Refresh();

            // Add a new fake localisation to the project by adding it to the
            // project file and saving it
            var projectFile = Yarn.Compiler.Project.LoadFromFile(projectFilePath);
            
            projectFile.Localisation.Add("test", new Compiler.Project.LocalizationInfo {
                Strings = YarnProjectImporter.UnityProjectRootVariable + "/" + destinationStringsFilePath,
            });

            projectFile.SaveToFile(projectFilePath);
            AssetDatabase.Refresh();

            // Assert:
            // A new localization, based on the .csv file we just created,
            // should be present.
            Assert.IsNotNull(project.baseLocalization);
            Assert.IsNotEmpty(project.localizations);
            Assert.AreEqual("test", project.localizations[0].LocaleCode);
            CollectionAssert.AreEquivalent(project.localizations[0].GetLineIDs(), YarnTestUtility.ExpectedStrings.Select(l => l.ID));
        }

        [Test]
        public void YarnImporterUtility_CanUpdateLocalizedCSVs_WhenBaseScriptChanges()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            string projectFilePath = AssetDatabase.GetAssetPath(project);
            var importer = AssetImporter.GetAtPath(projectFilePath) as YarnProjectImporter;
            var scriptPath = AssetDatabase.GetAssetPath(importer.ImportData.yarnFiles.First());

            var destinationStringsFilePath = YarnTestUtility.TestFilesDirectoryPath + "Generated.csv";

            // Act:
            // Create a .CSV File, and add it to the Yarn project. 
            YarnProjectUtility.WriteStringsFile(destinationStringsFilePath, importer);
            
            AssetDatabase.Refresh();

            // Add a new fake localisation to the project by adding it to the
            // project file and saving it
            var projectFile = Yarn.Compiler.Project.LoadFromFile(projectFilePath);
            projectFile.Localisation.Add("test", new Compiler.Project.LocalizationInfo {
                Strings = $"Generated.csv"
            });
            projectFile.SaveToFile(projectFilePath);
            AssetDatabase.Refresh();

            // Capture the strings tables. We'll use them later.
            var unmodifiedBaseStringsTable = importer.GenerateStringsTable();
            var unmodifiedLocalizedStringsTable = StringTableEntry.ParseFromCSV(File.ReadAllText(destinationStringsFilePath));

            // Next, modify the original source script.
            File.WriteAllText(scriptPath, YarnTestUtility.TestYarnScriptSourceModified);

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

            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            var projectFilePath = AssetDatabase.GetAssetPath(project);

            // Update the base language of this project and apply the change
            var projectFile = Yarn.Compiler.Project.LoadFromFile(projectFilePath);
            projectFile.BaseLanguage = defaultLanguage;
            projectFile.SaveToFile(projectFilePath);

            // Act: 
            // No further steps are taken besides re-importing it.
            AssetDatabase.Refresh();

            // Assert:
            // A single localization exists, with the default language.
            Assert.AreEqual(1, project.localizations.Count);
            Assert.AreSame(project.baseLocalization, project.localizations.First());

            Assert.NotNull(project.baseLocalization);

            var allAssetsAtPath = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(project));

            // Three assets: the project, the import data, and the localization
            Assert.AreEqual(3, allAssetsAtPath.Count());

            Assert.That(allAssetsAtPath, Has.Exactly(1).Matches(Is.TypeOf<YarnProject>()));
            Assert.That(allAssetsAtPath, Has.Exactly(1).Matches(Is.TypeOf<ProjectImportData>()));
            Assert.That(allAssetsAtPath, Has.Exactly(1).Matches(Is.TypeOf<Localization>()));

            // The localizations that were imported are the same as the
            // localizations the asset knows about
            Assert.AreSame(project.baseLocalization, allAssetsAtPath.OfType<Localization>().First());
        }

        [Test]
        public void YarnProjectImporter_OnLocalizationsSupplied_GeneratesExpectedLocalizations()
        {
            // Arrange: 
            // A project with a yarn script, configured with a known
            // default language.
            const string defaultLanguage = "de";
            const string otherLanguage = "en";

            Compiler.Project projectData = new Yarn.Compiler.Project
            {
                BaseLanguage = "de",
            };
            var project = SetUpProject(projectData, YarnTestUtility.TestYarnScriptSource);
            var projectPath = AssetDatabase.GetAssetPath(project);

            // Act:
            // Configure this importer to have a localization that:
            // - is not the same language as the default language" 
            // - has a strings file

            var newStringsAsset = YarnTestUtility.GetScriptSource("TestYarnProject-Strings.csv");

            var newStringsAssetPath = AssetDatabase.GetAssetPath(newStringsAsset);

            projectData.Localisation.Add(
                otherLanguage,
                new Compiler.Project.LocalizationInfo
                {
                    Strings = Path.Combine(YarnProjectImporter.UnityProjectRootVariable, newStringsAssetPath)
                }
            );

            projectData.SaveToFile(projectPath);
            AssetDatabase.Refresh();

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

            // Four assets: the project, the import data, and the two localizations
            Assert.AreEqual(4, allAssetsAtPath.Count());
            Assert.That(allAssetsAtPath, Has.Exactly(1).Matches(Is.TypeOf<YarnProject>()));
            Assert.That(allAssetsAtPath, Has.Exactly(1).Matches(Is.TypeOf<ProjectImportData>()));
            Assert.That(allAssetsAtPath, Has.Exactly(2).Matches(Is.TypeOf<Localization>()));

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

            // Configure this importer to have a localization that:
            // - is the same language as the default language" (and
            //   therefore has no strings file)
            // - has an assets folder to pull from

            var project = SetUpProject(new Yarn.Compiler.Project {
                BaseLanguage = defaultLanguage,
                Localisation = new Dictionary<string, Compiler.Project.LocalizationInfo> {
                    {
                        defaultLanguage, 
                        new Compiler.Project.LocalizationInfo {
                            Assets = Path.Combine(
                                YarnProjectImporter.UnityProjectRootPath, 
                                AssetDatabase.GetAssetPath(
                                    YarnTestUtility.GetFolder("Editor Test Resources")
                                )
                            ),
                        }
                    }
                }
            }, YarnTestUtility.TestYarnScriptSource);

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            // Act:
            importer.SaveAndReimport();

            // Assert:
            // A single localization exists that contains the loaded assets.

            Assert.AreEqual(1, project.localizations.Count);

            Localization localization = project.localizations[0];
            IEnumerable<AudioClip> allAudioClips = localization.GetLineIDs()
                                                               .Select(id => localization.GetLocalizedObject<AudioClip>(id));

            Assert.AreEqual(defaultLanguage, localization.LocaleCode);
            CollectionAssert.AreEquivalent(YarnTestUtility.ExpectedStrings.Select(l => l.ID), localization.GetLineIDs());
            Assert.AreEqual(YarnTestUtility.ExpectedStrings.Count(), allAudioClips.Count());
            CollectionAssert.AllItemsAreNotNull(allAudioClips);
            CollectionAssert.AllItemsAreUnique(allAudioClips);
        }

        [Test]
        public void YarnImporter_CanCreateNewScript()
        {
            // Arrange:
            // Choose a location where the new asset should be created
            var scriptPath = YarnTestUtility.TestFilesDirectoryPath + "/NewScript.yarn";

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
            var scriptPath = YarnTestUtility.TestFilesDirectoryPath + "/NewScript.yarn";

            var scriptAsset = YarnEditorUtility.CreateYarnAsset(scriptPath);
            var importer = AssetImporter.GetAtPath(scriptPath) as YarnImporter;

            // Act:
            // Create a Yarn Project from that script.
            var projectPath = YarnProjectUtility.CreateYarnProject(importer);
            
            // Assert: A new Yarn Project should exist, and is imported as
            // a Yarn Project that has the original Yarn script as one of
            // its source scripts.
            Assert.True(File.Exists(projectPath));

            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(projectPath);
            var projectImporter = AssetImporter.GetAtPath(projectPath) as YarnProjectImporter;

            Assert.IsNotNull(project);
            Assert.IsNotNull(projectImporter);
            Assert.Contains(scriptAsset, projectImporter.ImportData.yarnFiles);
            CollectionAssert.Contains(importer.DestinationProjects, project);
        }

        [Test]
        public void YarnImporter_CanCreateProjectAndScriptSimultaneously()
        {
            // Given
            string yarnProjectPath = $"{YarnTestUtility.TestFilesDirectoryPath}/Project.yarnproject";
            string yarnScriptPath = $"{YarnTestUtility.TestFilesDirectoryPath}/Script.yarn";

            var projectText = YarnProjectUtility.CreateDefaultYarnProject().GetJson();
            var scriptText = "title: Start\n---\n===\n";

            File.WriteAllText(yarnProjectPath, projectText);
            File.WriteAllText(yarnScriptPath, scriptText);

            // When
            AssetDatabase.Refresh();

            // Then

            var projectImporter = AssetImporter.GetAtPath(yarnProjectPath) as YarnProjectImporter;
            var projectAsset = AssetDatabase.LoadAssetAtPath<YarnProject>(yarnProjectPath);

            var scriptImporter = AssetImporter.GetAtPath(yarnScriptPath) as YarnImporter;
            var scriptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(yarnScriptPath);
            Assert.NotNull(projectImporter);
            Assert.NotNull(scriptImporter);
            Assert.NotNull(scriptAsset);
            
            Assert.That(projectImporter.ImportData.yarnFiles, Contains.Item(scriptAsset));
            
            Assert.That(scriptImporter.DestinationProjects, Contains.Item(projectAsset));
            Assert.That(scriptImporter.DestinationProjectImporters, Contains.Item(projectImporter));
        }

        [Test]
        public void YarnImporter_OnCreatingScriptWithNoLineIDs_HasImplicitLineIDs() {
            var project = SetUpProject(new[] {
                string.Join("\n", new[] {
                    "title: Test",
                    "---",
                    "Line with no id!",
                    "===",
                    ""
                })
            });

            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            Assert.True(importer.ImportData.containsImplicitLineIDs);
        }
        [Test]
        public void YarnImporter_OnCreatingScriptNoLineIDs_HasNoImplicitLineIDs() {
            var project = SetUpProject(new[] {
                string.Join("\n", new[] {
                    "title: Test",
                    "---",
                    "Line with an id! #line:1234",
                    "===",
                    ""
                })
            });

            AssetDatabase.Refresh();

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            Assert.False(importer.ImportData.containsImplicitLineIDs);
        }

        private static string OldStyleProjectText => string.Join("\n", new[] {
            "title: Project",
            "---",
            "<<declare $StringVariable = \"Hello\">>",
            "<<declare $NumberVariable = 1234>>",
            "<<declare $BoolVariable = true>>",
            "===",
            ""
        });

        [Test]
        public void YarnImporter_OnNonJSONProjectFormat_ProducesUsefulError() {

            //Given
            var outputPath = Path.Combine(YarnTestUtility.TestFilesDirectoryPath, "Project.yarnproject");
            
            LogAssert.Expect(LogType.Error, new Regex(".*needs to be upgraded.*"));

            // When
            File.WriteAllText(outputPath, OldStyleProjectText);
            AssetDatabase.Refresh();

            // Then
            var importer = AssetImporter.GetAtPath(outputPath) as YarnProjectImporter;

            Assert.That(importer, Is.Not.Null);

            Assert.That(importer.ImportData.ImportStatus, Is.EqualTo(ProjectImportData.ImportStatusCode.NeedsUpgradeFromV1));
        }

        [Test]
        public void YarnImporter_OnNonJSONProjectFormat_CanUpgrade() {
            var outputPath = Path.Combine(YarnTestUtility.TestFilesDirectoryPath, "Project.yarnproject");

            // When
            
            File.WriteAllText(outputPath, OldStyleProjectText);

            // Expect an import error to be logged
            LogAssert.Expect(LogType.Error, new Regex(".*needs to be upgraded.*"));

            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(outputPath) as YarnProjectImporter;
            Assert.That(importer.ImportData.ImportStatus, Is.EqualTo(ProjectImportData.ImportStatusCode.NeedsUpgradeFromV1));

            YarnProjectUtility.UpgradeYarnProject(importer);

            AssetDatabase.Refresh();

            Assert.That(importer.ImportData.ImportStatus, Is.EqualTo(ProjectImportData.ImportStatusCode.Succeeded));

            Assert.That(importer.ImportData.serializedDeclarations, Has.Count.EqualTo(3));
        }
        [Test]
        public void YarnImporter_ProgramCacheIsInvalidatedAfterReimport() {
            // Arrange: 
            // Set up a Yarn project and a Yarn script.
            var project = SetUpProject(new[] {
                    string.Join("\n", new[] {
                    "title: Test",
                    "---",
                    "Hello, world!",
                    "===",
                    ""
                })
            });
            // Act:
            // Get the cache's state after the project first gets imported.
            var before = project.Program;
            // Retrieve the Yarn script.
            var searchResults = AssetDatabase.FindAssets(
                $"t:{nameof(TextAsset)}", // Look for TextAssets...
                new[] {$"{YarnTestUtility.TestFilesDirectoryPath}"} // Under the test file directory.
            );
            // (Sanity check) There should only be one text asset (the script referenced by the Yarn project) here.
            Assert.AreEqual(1, searchResults.Length);
            var yarnScriptPath = AssetDatabase.GUIDToAssetPath(searchResults[0]);
            // Edit the Yarn script with new content.
            File.WriteAllText(
                yarnScriptPath, 
                string.Join("\n", new[] {
                    "title: Start",
                    "---",
                    "The quick brown fox jumps over the lazy dog.",
                    "===",
                    ""
                })
            );
            // Refresh the asset database to trigger reimport and thus recompilation.
            AssetDatabase.Refresh();
            // Access the cache again. 
            var after = project.Program;
            // Assert:
            // "before" and "after" are different objects because the cache is invalidated.
            Assert.AreNotEqual(before, after);
        }
    }
}
