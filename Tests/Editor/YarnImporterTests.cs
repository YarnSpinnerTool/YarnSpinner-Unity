/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

// #define KEEP_FILES_ON_TEARDOWN

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity.Editor;

#nullable enable

namespace Yarn.Unity.Tests
{
    public class YarnImporterTests
    {
        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(Path.Combine("Assets", YarnTestUtility.TestFolderName)) == false)
            {
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

            yarnProjectImporter.Should().NotBeNull();
            yarnProjectImporter!.ImportData.Should().NotBeNull();

            var scriptImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(yarnProjectImporter.ImportData!.yarnFiles.First())) as YarnImporter;

            scriptImporter.Should().NotBeNull();

            // Assert:
            // The Yarn project has a reference to the Yarn script. They
            // all report no compilation errors.
            yarnProjectImporter.ImportData.HasCompileErrors.Should().BeFalse();
            CollectionAssert.Contains(scriptImporter!.DestinationProjectImporters, yarnProjectImporter);
            scriptImporter.HasErrors.Should().BeFalse();

            ProjectImportData.ImportStatusCode.Succeeded.Should().BeEqualTo(yarnProjectImporter.ImportData.ImportStatus);
        }

        [Test]
        public void YarnProjectImporter_OnInvalidYarnFile_ImportsButDoesNotCompile()
        {

            // Arrange: 
            // Set up a Yarn project and a Yarn script, with invalid code.
            var project = SetUpProject("This is invalid yarn script, and will not compile.");

            var yarnProjectImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;
            yarnProjectImporter.Should().NotBeNull();
            yarnProjectImporter!.ImportData.Should().NotBeNull();

            var scriptImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(yarnProjectImporter.ImportData!.yarnFiles.First())) as YarnImporter;
            scriptImporter.Should().NotBeNull();

            // Assert:
            // The Yarn script will fail to compile, and both the script
            // and the project will know about this, but they otherwise
            // have correct asset references to each other.

            yarnProjectImporter.ImportData.HasCompileErrors.Should().BeTrue();
            CollectionAssert.Contains(scriptImporter!.DestinationProjectImporters, yarnProjectImporter);
            scriptImporter.HasErrors.Should().BeTrue();

            yarnProjectImporter.ImportData.ImportStatus.Should().BeEqualTo(ProjectImportData.ImportStatusCode.CompilationFailed);
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

            importer.Should().NotBeNull();

            // Assert: 
            // The project cannot generate a strings table.
            importer!.CanGenerateStringsTable.Should().BeFalse();
        }

        [Test]
        public void YarnProjectImporter_OnValidYarnFile_GetExpectedStrings()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            importer.Should().NotBeNull();

            // Act: 
            // Get the strings table from this project.
            var generatedStringsTable = importer!.GenerateStringsTable();

            // Simplify the results so that we can compare these string
            // table entries based only on specific fields
            static (string? id, string? text) simplifier(StringTableEntry e) => (id: e.ID, text: e.Text);

            var simpleExpected = YarnTestUtility.ExpectedStrings.Select(simplifier);
            var simpleResult = generatedStringsTable.Select(simplifier);

            // Assert:
            // The two string tables should be identical.

            simpleExpected.Should().BeEqualTo(simpleResult);
        }

        [Test]
        public void YarnProjectImporter_OnValidYarnFileWithMetadata_GeneratesLineMetadata()
        {
            // Arrange:
            // Set up a project with a Yarn file with metadata in some lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);

            // Assert:
            // Line metadata entry is generated for the project.
            project.lineMetadata.Should().NotBeNull();
            project.lineMetadata!.GetLineIDs().Count().Should().BeGreaterThan(0);
        }

        [Test]
        public void YarnProjectImporter_OnValidYarnFileWithoutMetadata_GeneratesEmptyLineMetadata()
        {
            // Arrange:
            // Set up a project with a Yarn file with metadata in some lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSourceNoMetadata);

            // Assert:
            // Line metadata entry is generated for the project.
            project.lineMetadata.Should().NotBeNull();
            project.lineMetadata!.GetLineIDs().Should().BeEmpty();
        }

        [Test]
        public void YarnProjectImporter_OnValidYarnFileWithMetadata_GetExpectedLineMetadata()
        {
            // Arrange:
            // Set up a project with a Yarn file with metadata in some lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            // Act:
            importer.Should().NotBeNull();
            var metadataEntries = importer!.GenerateLineMetadataEntries();

            // Simplify the results so that we can compare these metadata
            // table entries based only on specific fields.
            System.Func<LineMetadataTableEntry, (string id, string node, string lineNo, string metadata)> simplifier =
                e =>
                {
                    // Shadow line IDs may vary, so treat all shadow line IDs
                    // (which begin with "sh_") as the same by stripping them of
                    // everything but their prefix

                    string id;
                    if (e.ID.StartsWith("line:sh_"))
                    {
                        id = e.ID.Substring(0, "line:sh_".Length);
                    }
                    else
                    {
                        id = e.ID;
                    }
                    return (id, node: e.Node, lineNo: e.LineNumber, metadata: string.Join(" ", e.Metadata));
                };
            var simpleExpected = YarnTestUtility.ExpectedMetadata.Select(simplifier);
            var simpleResult = metadataEntries.Select(simplifier);

            // Assert:
            // Metadata entries should be identical to what we expect.
            simpleExpected.Should().BeEqualTo(simpleResult);
        }

        [Test]
        public void YarnProjectImporter_UpdatesLineMetadata_WhenBaseScriptChanges()
        {
            // Arrange:
            // Set up a project with a Yarn file with metadata in some lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;

            importer.Should().NotBeNull();
            importer!.ImportData.Should().NotBeNull();

            var scriptPath = AssetDatabase.GetAssetPath(importer.ImportData!.yarnFiles.First());

            // Act:
            // Modify the original source script.
            File.WriteAllText(scriptPath, YarnTestUtility.TestYarnScriptSourceModified);
            AssetDatabase.Refresh();

            // Assert: verify the line metadata exists and contains the expected number of entries.
            project.lineMetadata.Should().NotBeNull();
            project.lineMetadata!.GetLineIDs().Count().Should().BeEqualTo(2);
        }

        [Test]
        public void YarnProjectImporter_UpdatesLineMetadata_WhenBaseScriptChangesWithoutMetadata()
        {
            // Arrange:
            // Set up a project with a Yarn file with metadata in some lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(project)) as YarnProjectImporter;
            importer.Should().NotBeNull();
            importer!.ImportData.Should().NotBeNull();

            var scriptPath = AssetDatabase.GetAssetPath(importer.ImportData!.yarnFiles.First());

            // Act:
            // Modify the original source script to a script without any metadata.
            File.WriteAllText(scriptPath, YarnTestUtility.TestYarnScriptSourceNoMetadata);
            AssetDatabase.Refresh();

            // Assert: verify the line metadata exists and is empty.
            project.lineMetadata.Should().NotBeNull();
            project.lineMetadata!.GetLineIDs().Should().BeEmpty();
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

            project.baseLocalization.Should().NotBeNull();
            project.localizations.Should().HaveCount(1);
            project.baseLocalization.Should().BeSameObjectAs(project.localizations.Single().Value);

            project.baseLocalization.GetLineIDs().Should().ContainExactly(YarnTestUtility.ExpectedStrings.Select(l => l.ID));
        }

        [Test]
        public void YarnProjectUtility_OnGeneratingLinesFile_CreatesFile()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            string projectFilePath = AssetDatabase.GetAssetPath(project);
            var importer = AssetImporter.GetAtPath(projectFilePath) as YarnProjectImporter;

            importer.Should().NotBeNull();

            var destinationStringsFilePath = Path.Combine(YarnTestUtility.TestFilesDirectoryPath, "OutputStringsFile.csv");

            // Act:
            // Create a .CSV File, and add it to the Yarn project. 
            YarnProjectUtility.WriteStringsFile(destinationStringsFilePath, importer!);
            AssetDatabase.Refresh();

            // Add a new fake localisation to the project by adding it to the
            // project file and saving it
            var projectFile = Yarn.Compiler.Project.LoadFromFile(projectFilePath);

            projectFile.Localisation.Add("test", new Compiler.Project.LocalizationInfo
            {
                Strings = YarnProjectImporter.UnityProjectRootVariable + "/" + destinationStringsFilePath,
            });

            projectFile.SaveToFile(projectFilePath);
            AssetDatabase.Refresh();

            // Assert:
            // A new localization, based on the .csv file we just created,
            // should be present.
            project.baseLocalization.Should().NotBeNull();
            project.localizations.Should().NotBeEmpty();
            project.localizations.Should().HaveCount(2);

            var localization = project.localizations.Should().ContainKey("test").Subject.Value;

            localization.GetLineIDs().Should().ContainAllOf(YarnTestUtility.ExpectedStrings.Select(l => l.ID));
        }

        [Test]
        public void YarnImporterUtility_CanUpdateLocalizedCSVs_WhenBaseScriptChanges()
        {
            // Arrange:
            // Set up a project with a Yarn file filled with tagged lines.
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);
            string projectFilePath = AssetDatabase.GetAssetPath(project);
            var importer = AssetImporter.GetAtPath(projectFilePath) as YarnProjectImporter;

            importer.Should().NotBeNull();
            importer!.ImportData.Should().NotBeNull();

            var scriptPath = AssetDatabase.GetAssetPath(importer.ImportData!.yarnFiles.First());

            var destinationStringsFilePath = YarnTestUtility.TestFilesDirectoryPath + "Generated.csv";

            // Act:
            // Create a .CSV File, and add it to the Yarn project. 
            YarnProjectUtility.WriteStringsFile(destinationStringsFilePath, importer);

            AssetDatabase.Refresh();

            // Add a new fake localisation to the project by adding it to the
            // project file and saving it
            var projectFile = Yarn.Compiler.Project.LoadFromFile(projectFilePath);
            projectFile.Localisation.Add("test", new Compiler.Project.LocalizationInfo
            {
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
                unmodifiedBaseStringsTable.Select(test.test).Should().BeEqualTo(unmodifiedLocalizedStringsTable.Select(test.test), $"The unmodified string table {test.name}s should be equivalent");

                modifiedBaseStringsTable.Select(test.test).Should().BeEqualTo(modifiedLocalizedStringsTable.Select(test.test), $"The modified string table {test.name}s should be equivalent");

                unmodifiedBaseStringsTable.Select(test.test).Should().NotBeEqualTo(modifiedBaseStringsTable.Select(test.test), $"The unmodified and modified string table {test.name}s should not be equivalent");
            }
        }

        [Test]
        public void YarnEditorUtility_HasValidEditorResources()
        {

            // Test that YarnEditorUtility can locate the editor assets
            YarnEditorUtility.GetYarnDocumentIconTexture().Should().NotBeNull();
            YarnEditorUtility.GetTemplateYarnScriptPath().Should().NotBeNull();
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
            project.localizations.Should().HaveCount(1);
            project.baseLocalization.Should().BeSameObjectAs(project.localizations.Single().Value);

            project.baseLocalization.Should().NotBeNull();

            var allAssetsAtPath = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(project));

            // Three assets: the project, the import data, and the localization
            allAssetsAtPath.Should().HaveCount(3);

            allAssetsAtPath.OfType<YarnProject>().Should().HaveCount(1);
            allAssetsAtPath.OfType<ProjectImportData>().Should().HaveCount(1);
            allAssetsAtPath.OfType<Localization>().Should().HaveCount(1);

            // The localizations that were imported are the same as the
            // localizations the asset knows about
            project.baseLocalization.Should().BeSameObjectAs(allAssetsAtPath.OfType<Localization>().First());
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
            project.localizations.Should().HaveCount(2);

            var defaultLocalization = project.localizations.First(l => l.Key == defaultLanguage).Value;
            var otherLocalization = project.localizations.First(l => l.Key == otherLanguage).Value;

            defaultLocalization.Should().NotBeNull();
            otherLocalization.Should().NotBeNull();

            defaultLocalization.Should().NotBeSameObjectAs(otherLocalization, "both localisations are distinct");
            defaultLocalization.Should().BeSameObjectAs(project.baseLocalization, "the default language localisation is the project's base localisation");

            defaultLocalization.GetLineIDs().Should().ContainAllOf(otherLocalization.GetLineIDs());

            var allAssetsAtPath = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(project));

            // Four assets: the project, the import data, and the two localizations
            allAssetsAtPath.Should().HaveCount(4);
            allAssetsAtPath.OfType<YarnProject>().Should().HaveCount(1);
            allAssetsAtPath.OfType<ProjectImportData>().Should().HaveCount(1);
            allAssetsAtPath.OfType<Localization>().Should().HaveCount(2);

            // The localizations that were imported are the same as the
            // localizations the asset knows about
            project.localizations.Values.Should().ContainAllOf(allAssetsAtPath.OfType<Localization>());
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

            var project = SetUpProject(new Yarn.Compiler.Project
            {
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

            importer.Should().NotBeNull();

            // Act:
            importer!.SaveAndReimport();

            // Assert:
            // A single localization exists that contains the loaded assets.

            project.localizations.Should().HaveCount(1);

            (string localeCode, Localization localization) = project.localizations.First();
            IEnumerable<AudioClip> allAudioClips = localization
                .GetLineIDs()
                .Select(id =>
                {
                    AudioClip? audioClip = localization.GetLocalizedObjectSync<AudioClip>(id);

                    audioClip.Should().NotBeNull($"an audio clip should be found for id {id}");

                    return audioClip!;
                });

            defaultLanguage.Should().BeEqualTo(localeCode);
            YarnTestUtility.ExpectedStrings.Select(l => l.ID).Should().BeEqualTo(localization.GetLineIDs());
            YarnTestUtility.ExpectedStrings.Count().Should().BeEqualTo(allAudioClips.Count());
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

            scriptAsset.Should().NotBeNull();
            importer.Should().NotBeNull();
        }

        [Test]
        public void YarnImporter_CanCreateNewProjectFromScript()
        {
            // Arrange:
            // Create a Yarn script.
            var scriptPath = YarnTestUtility.TestFilesDirectoryPath + "/NewScript.yarn";

            var scriptAsset = YarnEditorUtility.CreateYarnAsset(scriptPath);
            var importer = AssetImporter.GetAtPath(scriptPath) as YarnImporter;

            importer.Should().NotBeNull();

            // Act:
            // Create a Yarn Project from that script.
            var projectPath = YarnProjectUtility.CreateYarnProject(importer!);

            // Assert: A new Yarn Project should exist, and is imported as
            // a Yarn Project that has the original Yarn script as one of
            // its source scripts.
            File.Exists(projectPath).Should().BeTrue();

            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(projectPath);
            var projectImporter = AssetImporter.GetAtPath(projectPath) as YarnProjectImporter;

            scriptAsset.Should().BeOfType<TextAsset>();
            project.Should().NotBeNull();
            projectImporter.Should().NotBeNull();
            projectImporter!.ImportData.Should().NotBeNull();
            projectImporter.ImportData!.yarnFiles.Should().Contain((scriptAsset as TextAsset)!);
            importer!.DestinationProjects.Should().Contain(project);

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

            projectImporter.Should().NotBeNull();
            projectImporter!.ImportData.Should().NotBeNull();

            var scriptImporter = AssetImporter.GetAtPath(yarnScriptPath) as YarnImporter;
            var scriptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(yarnScriptPath);
            projectImporter.Should().NotBeNull();
            scriptImporter.Should().NotBeNull();
            scriptAsset.Should().NotBeNull();

            projectImporter.ImportData!.yarnFiles.Should().Contain(scriptAsset);

            scriptImporter!.DestinationProjects.Should().Contain(projectAsset);
            scriptImporter.DestinationProjectImporters.Should().Contain(projectImporter);
        }

        [Test]
        public void YarnImporter_OnCreatingScriptWithNoLineIDs_HasImplicitLineIDs()
        {
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

            importer.Should().NotBeNull();
            importer!.ImportData.Should().NotBeNull();

            importer.ImportData!.containsImplicitLineIDs.Should().BeTrue();
        }
        [Test]
        public void YarnImporter_OnCreatingScriptNoLineIDs_HasNoImplicitLineIDs()
        {
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

            importer.Should().NotBeNull();
            importer!.ImportData.Should().NotBeNull();

            importer.ImportData!.containsImplicitLineIDs.Should().BeFalse();
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
        public void YarnImporter_OnNonJSONProjectFormat_ProducesUsefulError()
        {

            //Given
            var outputPath = Path.Combine(YarnTestUtility.TestFilesDirectoryPath, "Project.yarnproject");

            LogAssert.Expect(LogType.Error, new Regex(".*needs to be upgraded.*"));

            // When
            File.WriteAllText(outputPath, OldStyleProjectText);
            AssetDatabase.Refresh();

            // Then
            var importer = AssetImporter.GetAtPath(outputPath) as YarnProjectImporter;

            importer.Should().NotBeNull();
            importer!.ImportData.Should().NotBeNull();

            Assert.That(importer.ImportData!.ImportStatus, Is.EqualTo(ProjectImportData.ImportStatusCode.NeedsUpgradeFromV1));
        }

        [Test]
        public void YarnImporter_OnNonJSONProjectFormat_CanUpgrade()
        {
            var outputPath = Path.Combine(YarnTestUtility.TestFilesDirectoryPath, "Project.yarnproject");

            // When

            File.WriteAllText(outputPath, OldStyleProjectText);

            // Expect an import error to be logged
            LogAssert.Expect(LogType.Error, new Regex(".*needs to be upgraded.*"));

            AssetDatabase.Refresh();
            var importer = AssetImporter.GetAtPath(outputPath) as YarnProjectImporter;

            importer.Should().NotBeNull();
            importer!.ImportData.Should().NotBeNull();

            importer.ImportData!.ImportStatus.Should().BeEqualTo(ProjectImportData.ImportStatusCode.NeedsUpgradeFromV1);

            YarnProjectUtility.UpgradeYarnProject(importer);

            AssetDatabase.Refresh();

            importer.ImportData.ImportStatus.Should().BeEqualTo(ProjectImportData.ImportStatusCode.Succeeded);
            importer.ImportData.serializedDeclarations.Should().HaveCount(3);
        }
        [Test]
        public void YarnImporter_ProgramCacheIsInvalidatedAfterReimport()
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
            // Act:
            // Get the cache's state after the project first gets imported.
            var before = project.Program;
            // Retrieve the Yarn script.
            var searchResults = AssetDatabase.FindAssets(
                $"t:{nameof(TextAsset)}", // Look for TextAssets...
                new[] { $"{YarnTestUtility.TestFilesDirectoryPath}" } // Under the test file directory.
            );
            // (Sanity check) There should only be one text asset (the script referenced by the Yarn project) here.
            searchResults.Should().HaveCount(1);
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
            Assert.AreNotSame(before, after);
        }

        [Test]
        public void YarnImporter_OnImportScriptWithShadowLines_CreatesShadowTable()
        {
            var project = SetUpProject(YarnTestUtility.TestYarnScriptSource);

            project.lineMetadata.Should().NotBeNull();

            var lineIDs = project.lineMetadata!.GetLineIDs();

            // At least one shadow line entry should exist in the metadata
            var shadowLineID = lineIDs.Should().Contain((id) => project.lineMetadata.GetShadowLineSource(id) != null).Subject;

            // The entry should map to the line "shadowsource"
            var sourceLineID = project.lineMetadata.GetShadowLineSource(shadowLineID);
            sourceLineID.Should().BeEqualTo("line:shadowsource");

            // The entry should have its own metadata, distinct from the source
            var sourceLineMetadata = project.lineMetadata.GetMetadata(sourceLineID!);
            var shadowLineMetadata = project.lineMetadata.GetMetadata(shadowLineID!);

            sourceLineMetadata!.Should().NotBeNull();
            shadowLineMetadata!.Should().NotBeNull();

            sourceLineMetadata!.Should().Contain("meta1");
            shadowLineMetadata!.Should().Contain("meta2");

            shadowLineMetadata!.Should().NotContain("meta1");
            sourceLineMetadata!.Should().NotContain("meta2");
        }
    }
}
