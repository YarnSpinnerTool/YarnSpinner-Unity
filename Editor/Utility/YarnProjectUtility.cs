/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Yarn.Unity;

#if USE_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

#nullable enable

namespace Yarn.Unity.Editor
{
    /// <summary>
    /// Contains methods for performing high-level operations on Yarn projects,
    /// and their associated localization files.
    /// </summary>
    internal static class YarnProjectUtility
    {

        /// <summary>
        /// Creates a new .yarnproject asset in the same directory as the Yarn
        /// script represented by <paramref name="initialSourceAsset"/>, and
        /// configures the script's importer to use the new Yarn Project.
        /// </summary>
        /// <param name="initialSourceAsset">An importer for an existing Yarn
        /// script.</param>
        /// <returns>The path to the created asset, relative to the Unity
        /// project root.</returns>
        internal static string CreateYarnProject(YarnImporter initialSourceAsset)
        {

            // Figure out where on disk this asset is
            var path = initialSourceAsset.assetPath;
            var directory = Path.GetDirectoryName(path);

            // Figure out a new, unique path for the localization we're creating
            var databaseFileName = $"Project.yarnproject";

            var destinationPath = Path.Combine(directory, databaseFileName);
            destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);

            // Create the program
            var newProject = YarnProjectUtility.CreateDefaultYarnProject();

            newProject.SaveToFile(destinationPath);

            AssetDatabase.ImportAsset(destinationPath);
            AssetDatabase.SaveAssets();

            return destinationPath;
        }

        /// <summary>
        /// Creates a Unity tweaked default Yarn Project.
        /// </summary>
        /// <remarks>
        /// This is just a default Yarn Project with the exclusion file pattern
        /// set up to ignore ~ folders.
        /// </remarks>
        /// <returns>A Unity default Yarn Project</returns>
        internal static Yarn.Compiler.Project CreateDefaultYarnProject()
        {
            // Create the program
            var newProject = new Yarn.Compiler.Project();

            // Follow Unity's behaviour - exclude any content in a folder whose
            // name ends with a tilde
            // and also ignoring anything that is inside a sample folder
            newProject.ExcludeFilePatterns = new[] { "**/*~/*", "./Samples/Yarn Spinner*/*" };

            return newProject;
        }

        /// <summary>
        /// Updates every localization .CSV file associated with this
        /// .yarnproject file.
        /// </summary>
        /// <remarks>
        /// This method updates each localization file by performing the
        /// following operations:
        /// <list type="bullet">
        /// <item>Inserts new entries if they're present in the base
        /// localization and not in the translated localization</item>
        ///
        /// <item>Removes entries if they're present in the translated
        /// localization and not in the base localization</item>
        ///
        /// <item>Detects if a line in the base localization has changed its
        /// Lock value from when the translated localization was created, and
        /// update its Comment</item></list>
        /// </remarks>
        /// <param name="yarnProjectImporter">An importer for an existing Yarn
        /// script.</param>
        /// <returns>The path to the created asset, relative to the Unity
        /// project root.</returns>
        internal static void UpdateLocalizationCSVs(YarnProjectImporter yarnProjectImporter)
        {
            if (yarnProjectImporter.CanGenerateStringsTable == false)
            {
                Debug.LogError($"Can't update localization CSVs for Yarn Project \"{yarnProjectImporter.name}\" because not every line has a tag.");
                return;
            }

            var importData = yarnProjectImporter.ImportData;

            if (importData == null)
            {
                Debug.LogError($"Can't update localization CSVs for Yarn Project \"{yarnProjectImporter.name}\" because it failed to compile.");
                return;
            }

            var job = yarnProjectImporter.GetCompilationJob();
            job.CompilationType = Compiler.CompilationJob.Type.StringsOnly;
            var result = Compiler.Compiler.Compile(job);

            var baseLocalizationStrings = yarnProjectImporter.GenerateStringsTable(result);

            if (baseLocalizationStrings == null)
            {
                Debug.LogError($"Can't update localization CSVs for Yarn Project \"{yarnProjectImporter.name}\" because it failed to compile.");
                return;
            }

            var localizations = importData.localizations;

            var modifiedFiles = new List<TextAsset>();

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var loc in localizations)
                {
                    if (loc.languageID == importData.baseLanguageName)
                    {
                        // This is the base language - no strings file to
                        // update.
                        continue;
                    }

                    if (loc.stringsFile == null)
                    {
                        Debug.LogWarning($"Can't update localization for {loc.languageID} because it doesn't have a strings file.", yarnProjectImporter);
                        continue;
                    }

                    var fileWasChanged = UpdateLocalizationFile(baseLocalizationStrings, loc.languageID, loc.stringsFile);

                    if (fileWasChanged)
                    {
                        modifiedFiles.Add(loc.stringsFile);
                    }
                }

                if (modifiedFiles.Count > 0)
                {
                    Debug.Log($"Updated the following files: {string.Join(", ", modifiedFiles.Select(f => AssetDatabase.GetAssetPath(f)))}");
                }
                else
                {
                    Debug.Log($"No files needed updating.");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        internal static void UpdateAssetAddresses(YarnProjectImporter importer)
        {
#if USE_ADDRESSABLES
            var job = importer.GetCompilationJob();
            job.CompilationType = Compiler.CompilationJob.Type.StringsOnly;
            var result = Compiler.Compiler.Compile(job);

            var lineIDs = importer.GenerateStringsTable(result).Select(s => s.ID);

            if (importer.ImportData == null)
            {
                throw new System.InvalidOperationException($"Can't update asset addresses: importer has no {nameof(importer.ImportData)}");
            }

            // Get a map of language IDs to (lineID, asset path) pairs
            var languageToAssets = importer
                // Get the languages-to-source-assets map
                .ImportData.localizations
                // Get the asset folder for them
                .Select(l => new { l.languageID, l.assetsFolder })
                // Only consider those that have an asset folder
                .Where(f => f.assetsFolder != null)
                // Get the path for the asset folder
                .Select(f => new { f.languageID, path = AssetDatabase.GetAssetPath(f.assetsFolder) })
                // Use that to get the assets inside these folders
                .Select(f => new { f.languageID, assetPaths = FindAssetPathsForLineIDs(lineIDs, f.path, typeof(UnityEngine.Object)) });

            var addressableAssetSettings = AddressableAssetSettingsDefaultObject.Settings;

            foreach (var languageToAsset in languageToAssets)
            {
                var assets = languageToAsset.assetPaths
                    .Select(pair => new { LineID = pair.Key, GUID = AssetDatabase.AssetPathToGUID(pair.Value) });

                foreach (var asset in assets)
                {
                    // Find the existing entry for this asset, if it has one.
                    AddressableAssetEntry entry = addressableAssetSettings.FindAssetEntry(asset.GUID);

                    if (entry == null)
                    {
                        // This asset didn't have an entry. Create one in the
                        // default group.
                        entry = addressableAssetSettings.CreateOrMoveEntry(asset.GUID, addressableAssetSettings.DefaultGroup);
                    }

                    // Update the entry's address.
                    entry.SetAddress(Localization.GetAddressForLine(asset.LineID, languageToAsset.languageID));
                }
            }
#else
            throw new System.NotSupportedException($"A method that requires the Addressable Assets package was called, but USE_ADDRESSABLES was not defined. Please either install Addressable Assets, or if you have already, add it to this project's compiler definitions.");
#endif
        }

        internal static Dictionary<string, string> FindAssetPathsForLineIDs(IEnumerable<string> lineIDs, string assetsFolderPath, System.Type assetType)
        {
            // Find _all_ files in this director that are not .meta files and
            // whose main asset is equal to (or derived from) assetType
            var allFiles = Directory.EnumerateFiles(assetsFolderPath, "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".meta") == false)
                .Where(path => assetType.IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(path)));

            // Match files with those whose filenames contain a line ID
            var matchedFilesAndPaths = lineIDs.GroupJoin(
                // the elements we're matching lineIDs to
                allFiles,
                // the key for lineIDs (being strings, it's just the line ID
                // itself)
                lineID => lineID,
                // the key for assets (the filename without the path)
                assetPath => Path.GetFileName(assetPath),
                // the way we produce the result (a key-value pair)
                (lineID, assetPaths) =>
                {
                    if (assetPaths.Count() > 1)
                    {
                        Debug.LogWarning($"Line {lineID} has {assetPaths.Count()} possible assets.\n{string.Join(", ", assetPaths)}");
                    }
                    return new { lineID, assetPaths };
                },
                // the way we test to see if two elements should be joined (does
                // the filename contain the line ID?)
                Compare.By<string>((fileName, lineID) =>
                {
                    var lineIDWithoutPrefix = lineID.Replace("line:", "");
                    return Path.GetFileNameWithoutExtension(fileName).Contains(lineIDWithoutPrefix);
                })
                )
                // Discard any pair where no asset was found
                .Where(pair => pair.assetPaths.Count() > 0)
                .ToDictionary(entry => entry.lineID, entry => entry.assetPaths.FirstOrDefault());

            return matchedFilesAndPaths;
        }

        /// <summary>
        /// Verifies the TextAsset referred to by <paramref
        /// name="destinationLocalizationAsset"/>, and updates it if necessary.
        /// </summary>
        /// <param name="baseLocalizationStrings">A collection of <see
        /// cref="StringTableEntry"/></param>
        /// <param name="language">The language that <paramref
        /// name="destinationLocalizationAsset"/> provides strings
        /// for.false</param>
        /// <param name="destinationLocalizationAsset">A TextAsset containing
        /// localized strings in CSV format.</param>
        /// <returns>Whether <paramref name="destinationLocalizationAsset"/> was
        /// modified.</returns>
        private static bool UpdateLocalizationFile(IEnumerable<StringTableEntry> baseLocalizationStrings, string language, TextAsset destinationLocalizationAsset)
        {
            var translatedStrings = StringTableEntry.ParseFromCSV(destinationLocalizationAsset.text);

            // Convert both enumerables to dictionaries, for easier lookup
            var baseDictionary = baseLocalizationStrings.ToDictionary(entry => entry.ID);
            var translatedDictionary = translatedStrings.ToDictionary(entry => entry.ID);

            // The list of line IDs present in each localisation
            var baseIDs = baseLocalizationStrings.Select(entry => entry.ID);
            var translatedIDs = translatedStrings.Select(entry => entry.ID);

            // The list of line IDs that are ONLY present in each localisation
            var onlyInBaseIDs = baseIDs.Except(translatedIDs);
            var onlyInTranslatedIDs = translatedIDs.Except(baseIDs);

            // Tracks if the translated localisation needed modifications
            // (either new lines added, old lines removed, or changed lines
            // flagged)
            var modificationsNeeded = false;

            // Remove every entry whose ID is only present in the translated
            // set. This entry has been removed from the base localization.
            foreach (var id in onlyInTranslatedIDs.ToList())
            {
                translatedDictionary.Remove(id);
                modificationsNeeded = true;
            }

            // Conversely, for every entry that is only present in the base
            // localisation, we need to create a new entry for it.
            foreach (var id in onlyInBaseIDs)
            {
                StringTableEntry baseEntry = baseDictionary[id];
                var newEntry = new StringTableEntry(baseEntry)
                {
                    // Empty this text, so that it's apparent that a translated
                    // version needs to be provided.
                    Text = string.Empty,
                    Language = language,
                };
                translatedDictionary.Add(id, newEntry);
                modificationsNeeded = true;
            }

            // Finally, we need to check for any entries in the translated
            // localisation that:
            // 1. have the same line ID as one in the base, but
            // 2. have a different Lock (the hash of the text), which indicates
            //    that the base text has changed.

            // First, get the list of IDs that are in both base and translated,
            // and then filter this list to any where the lock values differ
            var outOfDateLockIDs = baseDictionary.Keys
                .Intersect(translatedDictionary.Keys)
                .Where(id => baseDictionary[id].Lock != translatedDictionary[id].Lock);

            // Now loop over all of these, and update our translated dictionary
            // to include a note that it needs attention
            foreach (var id in outOfDateLockIDs)
            {
                // Get the translated entry as it currently exists
                var entry = translatedDictionary[id];

                // Include a note that this entry is out of date
                entry.Text = $"(NEEDS UPDATE) {entry.Text}";

                // Update the lock to match the new one
                entry.Lock = baseDictionary[id].Lock;

                // Put this modified entry back in the table
                translatedDictionary[id] = entry;

                modificationsNeeded = true;
            }

            // We're all done!

            if (modificationsNeeded == false)
            {
                // No changes needed to be done to the translated string table
                // entries. Stop here.
                return false;
            }

            // We need to produce a replacement CSV file for the translated
            // entries.

            var outputStringEntries = translatedDictionary.Values
                .OrderBy(entry => entry.File)
                .ThenBy(entry => int.Parse(entry.LineNumber));

            var outputCSV = StringTableEntry.CreateCSV(outputStringEntries);

            // Write out the replacement text to this existing file, replacing
            // its existing contents
            var outputFile = AssetDatabase.GetAssetPath(destinationLocalizationAsset);
            File.WriteAllText(outputFile, outputCSV, System.Text.Encoding.UTF8);

            // Tell the asset database that the file needs to be reimported
            AssetDatabase.ImportAsset(outputFile);

            // Signal that the file was changed
            return true;
        }

        private static (List<string> AllExistingTags, List<string> ProjectImplicitTags) ExtantLineTags(YarnProjectImporter importer)
        {
            // First, gather all existing line tags across ALL yarn projects, so
            // that we don't accidentally overwrite an existing one. Do this by
            // finding all yarn projects, and get the string tags inside them.
            // By doing it in this way we get the same implicit tags from the
            // project as the importer would normally do, letting us then do a
            // direct comparision for them.
            var allYarnProjects =
                // get all yarn projects across the entire project
                AssetDatabase.FindAssets($"t:{nameof(YarnProject)}")
                // Get the path for each asset's GUID
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                // Get the importer for each asset at this path
                .Select(path => AssetImporter.GetAtPath(path))
                // Ensure it's a YarnProjectImporter
                .OfType<YarnProjectImporter>()
                // Ensure that its import data is present
                .Where(i => i.ImportData != null)
                // get the project out, and also flag if it is the project for
                // THIS importer
                .Select(i => (Project: i.GetProject()!, IsThisImporter: i == importer))
                // remove any nulls just in case any are found
                .Where(p => p.Project != null);

#if YARNSPINNER_DEBUG
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

            var allExistingTags = new List<string>();
            var projectImplicitTags = new List<string>();

            // Compile all of these, and get whatever existing string tags they
            // had. Do each in isolation so that we can continue even if a
            // project contains a parse error.
            foreach (var (Project, IsThisImporter) in allYarnProjects)
            {
                var project = Project;
                var compilationJob = Yarn.Compiler.CompilationJob.CreateFromFiles(project.SourceFiles);
                compilationJob.CompilationType = Yarn.Compiler.CompilationJob.Type.StringsOnly;

                var result = Yarn.Compiler.Compiler.Compile(compilationJob);
                bool containsErrors = result.Diagnostics.Any(d => d.Severity == Compiler.Diagnostic.DiagnosticSeverity.Error);
                if (containsErrors)
                {
                    Debug.LogWarning($"{project} has errors so cannot be scanned for tagging.");
                    continue;
                }
                allExistingTags.AddRange(result.StringTable.Where(i => i.Value.isImplicitTag == false).Select(i => i.Key));

                // we add the implicit lines IDs only for this project
                if (IsThisImporter)
                {
                    projectImplicitTags.AddRange(result.StringTable.Where(i => i.Value.isImplicitTag == true).Select(i => i.Key));
                }
            }

#if YARNSPINNER_DEBUG
            stopwatch.Stop();
            Debug.Log($"Checked {allYarnProjects.Count()} yarn files for line tags in {stopwatch.ElapsedMilliseconds}ms");
#endif
            return (allExistingTags, projectImplicitTags);
        }

        public static void AddLineTagsToFilesInYarnProject(YarnProjectImporter importer)
        {
            var (AllExistingTags, ProjectImplicitTags) = YarnProjectUtility.ExtantLineTags(importer);

#if USE_UNITY_LOCALIZATION
            // if we are using Unity localisation we need to first remove the
            // implicit tags for this project from the strings table
            if (importer.UseUnityLocalisationSystem && importer.UnityLocalisationStringTableCollection != null)
            {
                foreach (var implicitTag in ProjectImplicitTags)
                {
                    importer.UnityLocalisationStringTableCollection.RemoveEntry(implicitTag);
                }
            }
#endif

            if (importer.ImportData == null)
            {
                Debug.LogError($"Can't add line tags to {importer.assetPath}, because it failed to compile.");
                return;
            }

            var modifiedFiles = new List<string>();
            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var script in importer.ImportData.yarnFiles)
                {
                    var assetPath = AssetDatabase.GetAssetPath(script);
                    var contents = File.ReadAllText(assetPath);

                    // Produce a version of this file that contains line tags
                    // added where they're needed.
                    var tagged = Yarn.Compiler.Utility.TagLines(contents, AllExistingTags ?? new List<string>());
                    var taggedVersion = tagged.Item1;

                    // if the file has an error it returns null we want to bail
                    // out then otherwise we'd wipe the yarn file
                    if (taggedVersion == null)
                    {
                        continue;
                    }

                    // If this produced a modified version of the file, write it
                    // out and re-import it.
                    if (contents != taggedVersion)
                    {
                        modifiedFiles.Add(Path.GetFileNameWithoutExtension(assetPath));

                        File.WriteAllText(assetPath, taggedVersion, System.Text.Encoding.UTF8);
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);

                        AllExistingTags = tagged.Item2 as List<string>;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Encountered an error when updating scripts: {e}");
                return;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Report on the work we did.
            if (modifiedFiles.Count > 0)
            {
                Debug.Log($"Updated the following files: {string.Join(", ", modifiedFiles)}");
            }
            else
            {
                Debug.Log("No files needed updating.");
            }

        }

        /// <summary>
        /// Writes a .csv file to disk at the path indicated by <paramref
        /// name="destination"/>, containing all of the lines found in the
        /// scripts referred to by <paramref name="yarnProjectImporter"/>.
        /// </summary>
        /// <param name="yarnProjectImporter">The YarnProjectImporter to extract
        /// strings from.</param>
        /// <param name="destination">The path to write the file to.</param>
        /// <returns><see langword="true"/> if the file was written
        /// successfully, <see langword="false"/> otherwise.</returns>
        /// <exception cref="CsvHelper.CsvHelperException">Thrown when an error
        /// is encountered when generating the CSV data.</exception>
        /// <exception cref="IOException">Thrown when an error is encountered
        /// when writing the data to disk.</exception>
        internal static bool WriteStringsFile(string destination, YarnProjectImporter yarnProjectImporter)
        {
            // Perform a strings-only compilation to get a full strings table,
            // and generate the CSV. 
            var job = yarnProjectImporter.GetCompilationJob();
            job.CompilationType = Compiler.CompilationJob.Type.StringsOnly;
            var result = Compiler.Compiler.Compile(job);

            if (result.ContainsErrors)
            {
                // The project contains errors. Bail out.
                return false;
            }

            var stringTable = yarnProjectImporter.GenerateStringsTable(result);

            // If there was an error, bail out here
            if (stringTable == null)
            {
                return false;
            }

            // Convert the string tables to CSV...
            var outputCSV = StringTableEntry.CreateCSV(stringTable);

            // ...and write it to disk.
            File.WriteAllText(destination, outputCSV);

            return true;
        }

        /// <summary>
        /// Writes a .csv file to disk at the path indicated by <paramref
        /// name="destination"/>, containing all of the lines found in the
        /// scripts referred to by <paramref name="yarnProjectImporter"/> that
        /// contain any metadata associated with them.
        /// </summary>
        /// <param name="yarnProjectImporter">The YarnProjectImporter to extract
        /// strings from.</param>
        /// <param name="destination">The path to write the file to.</param>
        /// <returns><see langword="true"/> if the file was written
        /// successfully, <see langword="false"/> otherwise.</returns>
        /// <exception cref="CsvHelper.CsvHelperException">Thrown when an error
        /// is encountered when generating the CSV data.</exception>
        /// <exception cref="IOException">Thrown when an error is encountered
        /// when writing the data to disk.</exception>
        internal static bool WriteMetadataFile(string destination, YarnProjectImporter yarnProjectImporter)
        {
            var lineMetadataEntries = yarnProjectImporter.GenerateLineMetadataEntries();

            // If there was an error, bail out here.
            if (lineMetadataEntries == null)
            {
                return false;
            }

            var outputCSV = LineMetadataTableEntry.CreateCSV(lineMetadataEntries);
            File.WriteAllText(destination, outputCSV);

            return true;
        }

        /// <summary>
        /// Upgrades an old-style Yarn Project to JSON.
        /// </summary>
        /// <remarks>
        /// This method copies the text of the project to a new file adjacent to
        /// the project, and replaces the text of the project with a new empty
        /// JSON project.
        /// </remarks>
        /// <param name="importer">A YarnProjectImporter that represents the
        /// project that needs to be upgraded.</param>
        internal static void UpgradeYarnProject(YarnProjectImporter importer)
        {
            // We need to copy out the variable declarations from the old Yarn
            // project before we replace it.

            // Get the current text of the old project
            var existingText = File.ReadAllText(importer.assetPath);

            // Does the existing text contain anything besides the default?
            var defaultProjectPattern = new System.Text.RegularExpressions.Regex(@"^title:.*?\n---[\n\s]*===[\n\s]*$", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (defaultProjectPattern.IsMatch(existingText))
            {
                // The project contains no content, so there's no need to copy
                // it out.
            }
            else
            {
                // Create a unique path to store our variables
                var newFilePath = Path.GetDirectoryName(importer.assetPath) + "/Variables.yarn";
                newFilePath = AssetDatabase.GenerateUniqueAssetPath(newFilePath);

                // Write it out to the new file
                File.WriteAllText(newFilePath, existingText);
            }

            // Next, replace the existing project with a new one!
            var newProject = YarnProjectUtility.CreateDefaultYarnProject();
            File.WriteAllText(importer.assetPath, newProject.GetJson());

            // Finally, import the assets we've touched.
            AssetDatabase.Refresh();
        }

        [OnOpenAsset(OnOpenAssetAttributeMode.Execute)]
        public static bool OnOpenAsset(int instanceID)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(path);

            if (project == null)
            {
                return false;
            }

            var importer = AssetImporter.GetAtPath(path) as Yarn.Unity.Editor.YarnProjectImporter;
            if (importer == null)
            {
                return false;
            }

            var yp = Yarn.Compiler.Project.LoadFromFile(path);
            var files = yp.SourceFiles;

            if (files.Any())
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(System.IO.Path.GetDirectoryName(files.First()), 0);
            }

            return true;
        }

        [OnOpenAsset(OnOpenAssetAttributeMode.Validate)]
        public static bool OnValidateAsset(int instanceID)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(path);

            if (project == null)
            {
                return false;
            }

            return true;
        }
    }
}
