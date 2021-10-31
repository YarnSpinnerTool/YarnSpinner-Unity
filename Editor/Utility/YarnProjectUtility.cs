using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Yarn.Unity;

#if USE_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace Yarn.Unity.Editor
{
    /// <summary>
    /// Contains methods for performing high-level operations on Yarn
    /// projects, and their associated localization files.
    /// </summary>
    internal static class YarnProjectUtility
    {

        /// <summary>
        /// Creates a new .yarnproject asset in the same directory as the
        /// Yarn script represented by serializedObject, and configures the
        /// script's importer to use the new Yarn Project.
        /// </summary>
        /// <param name="serializedObject">A serialized object that
        /// represents a <see cref="YarnImporter"/>.</param>
        /// <returns>The path to the created asset.</returns>
        internal static string CreateYarnProject(YarnImporter initialSourceAsset)
        {

            // Figure out where on disk this asset is
            var path = initialSourceAsset.assetPath;
            var directory = Path.GetDirectoryName(path);

            // Figure out a new, unique path for the localization we're
            // creating
            var databaseFileName = $"Project.yarnproject";

            var destinationPath = Path.Combine(directory, databaseFileName);
            destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);

            // Create the program
            YarnEditorUtility.CreateYarnAsset(destinationPath);
            
            AssetDatabase.ImportAsset(destinationPath);
            AssetDatabase.SaveAssets();

            AssignScriptToProject(path, destinationPath);

            return destinationPath;
        }

        /// <summary>
        /// Assign a .yarn <see cref="TextAsset"/> file found at <paramref
        /// name="sourcePath"/> to the <see cref="YarnProjectImporter"/>
        /// found at <paramref name="projectPath"/>.
        /// </summary>
        /// <param name="projectPath">The path to the .yarnproject
        /// asset.</param>
        /// <param name="sourcePath">The path to the source .yarn asset
        /// that should be added to the Yarn Project.</param>
        internal static void AssignScriptToProject(string sourcePath, string projectPath) {
            var newSourceScript = AssetDatabase.LoadAssetAtPath<TextAsset>(sourcePath);
            var programImporter = AssetImporter.GetAtPath(projectPath) as YarnProjectImporter;
            var scriptImporter = AssetImporter.GetAtPath(sourcePath) as YarnImporter;

            if (newSourceScript == null) {
                throw new FileNotFoundException($"{nameof(sourcePath)} couldn't be loaded as a {nameof(TextAsset)}.");
            }

            if (projectPath != null && programImporter == null) {
                throw new System.ArgumentException($"{nameof(projectPath)} is not a Yarn Project.");
            }

            if (scriptImporter == null) {
                throw new System.ArgumentException($"{nameof(projectPath)} is not a Yarn script.");
            }

            AssignScriptToProject(newSourceScript, scriptImporter, programImporter);
        }

        /// <summary>
        /// Assign a .yarn <see cref="TextAsset"/> file <paramref
        /// name="newSourceScript"/> to the <see
        /// cref="YarnProjectImporter"/> <paramref
        /// name="projectImporter"/>.
        /// </summary>
        /// <remarks>If <paramref name="projectImporter"/> is <see
        /// langword="null"/>, this script will be removed from its current
        /// project (if any) and not added to another.</remarks>
        /// <param name="newSourceScript">The script that should be
        /// assigned to the Yarn Project.</param>
        /// <param name="scriptImporter">The importer for <paramref
        /// name="newSourceScript"/>.</param>
        /// <param name="projectImporter">The importer for the project that
        /// newSourceScript should be made a part of, or null.</param>
        internal static void AssignScriptToProject(TextAsset newSourceScript, YarnImporter scriptImporter, YarnProjectImporter projectImporter) {

            if (scriptImporter.DestinationProject != null) {
                var existingProjectImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(scriptImporter.DestinationProject)) as YarnProjectImporter;

                existingProjectImporter.sourceScripts.Remove(newSourceScript);

                EditorUtility.SetDirty(existingProjectImporter);
                
                existingProjectImporter.SaveAndReimport();
            }
            
            if (projectImporter != null) {
                projectImporter.sourceScripts.Add(newSourceScript);
                EditorUtility.SetDirty(projectImporter);

                // Reimport the program to make it generate its default string
                // table, if needed
                projectImporter.SaveAndReimport();
            }

        }

        /// <summary>
        /// Updates every localization .CSV file associated with this
        /// .yarnproject file.
        /// </summary>
        /// <remarks>
        /// This method updates each localization file by performing the
        /// following operations:
        ///
        /// - Inserts new entries if they're present in the base
        /// localization and not in the translated localization
        ///
        /// - Removes entries if they're present in the translated
        /// localization and not in the base localization
        ///
        /// - Detects if a line in the base localization has changed its
        /// Lock value from when the translated localization was created,
        /// and update its Comment
        /// </remarks>
        /// <param name="serializedObject">A serialized object that
        /// represents a <see cref="YarnProjectImporter"/>.</param>
        internal static void UpdateLocalizationCSVs(YarnProjectImporter yarnProjectImporter)
        {
            if (yarnProjectImporter.CanGenerateStringsTable == false)
            {
                Debug.LogError($"Can't update localization CSVs for Yarn Project \"{yarnProjectImporter.name}\" because not every line has a tag.");
                return;
            }

            var baseLocalizationStrings = yarnProjectImporter.GenerateStringsTable();

            var localizations = yarnProjectImporter.languagesToSourceAssets;

            var modifiedFiles = new List<TextAsset>();

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var loc in localizations)
                {
                    if (loc.stringsFile == null) {
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


        /// <summary>
        /// Returns an <see cref="IEnumerable"/> containing the string
        /// table entries for the base language for the specified Yarn
        /// script.
        /// </summary>
        /// <param name="serializedObject">A serialized object that
        /// represents a <see cref="YarnScript"/>.</param>
        /// <returns>The string table entries.</returns>
        private static IEnumerable<StringTableEntry> GetBaseLanguageStringsForSelectedObject(SerializedObject serializedObject)
        {
            var baseLanguageProperty = serializedObject.FindProperty("baseLanguage");

            // Get the TextAsset that contains the base string table CSV
            TextAsset textAsset = baseLanguageProperty.objectReferenceValue as TextAsset;

            if (textAsset == null)
            {
                throw new System.NullReferenceException($"The base language table asset for {serializedObject.targetObject.name} is either null or not a TextAsset. Did the script fail to compile?");

            }

            var baseLanguageTableText = textAsset.text;

            // Parse this CSV into StringTableEntry structs
            return StringTableEntry.ParseFromCSV(baseLanguageTableText)
                                .OrderBy(entry => entry.File)
                                .ThenBy(entry => int.Parse(entry.LineNumber));
        }

        internal static void UpdateAssetAddresses(YarnProjectImporter importer)
        {
#if USE_ADDRESSABLES
            var lineIDs = importer.GenerateStringsTable().Select(s => s.ID);

            // Get a map of language IDs to (lineID, asset path) pairs
            var languageToAssets = importer
                // Get the languages-to-source-assets map
                .languagesToSourceAssets
                // Get the asset folder for them
                .Select(l => new {l.languageID, l.assetsFolder})
                // Only consider those that have an asset folder
                .Where(f => f.assetsFolder != null)
                // Get the path for the asset folder
                .Select(f => new {f.languageID, path = AssetDatabase.GetAssetPath(f.assetsFolder)})
                // Use that to get the assets inside these folders
                .Select(f => new {f.languageID, assetPaths = FindAssetPathsForLineIDs(lineIDs, f.path)});

            var addressableAssetSettings = AddressableAssetSettingsDefaultObject.Settings;

            foreach (var languageToAsset in languageToAssets) {
                var assets = languageToAsset.assetPaths
                    .Select(pair => new {LineID = pair.Key, GUID = AssetDatabase.AssetPathToGUID(pair.Value)});
                
                foreach (var asset in assets) {
                    // Find the existing entry for this asset, if it has
                    // one.
                    AddressableAssetEntry entry = addressableAssetSettings.FindAssetEntry(asset.GUID);

                    if (entry == null) {
                        // This asset didn't have an entry. Create one in
                        // the default group.
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

        internal static Dictionary<string, string> FindAssetPathsForLineIDs(IEnumerable<string> lineIDs, string assetsFolderPath)
        {
            // Find _all_ files in this director that are not .meta files
            var allFiles = Directory.EnumerateFiles(assetsFolderPath, "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".meta") == false);

            // Match files with those whose filenames contain a line ID
            var matchedFilesAndPaths = lineIDs.GroupJoin(
                // the elements we're matching lineIDs to
                allFiles,
                // the key for lineIDs (being strings, it's just the line
                // ID itself)
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
                // the way we test to see if two elements should be joined
                // (does the filename contain the line ID?)
                Compare.By<string>((fileName, lineID) =>
                {
                    var lineIDWithoutPrefix = lineID.Replace("line:", "");
                    return Path.GetFileNameWithoutExtension(fileName).Equals(lineIDWithoutPrefix);
                })
                )
                // Discard any pair where no asset was found
                .Where(pair => pair.assetPaths.Count() > 0)
                .ToDictionary(entry => entry.lineID, entry => entry.assetPaths.FirstOrDefault());

            return matchedFilesAndPaths;
        }

        /// <summary>
        /// Verifies the TextAsset referred to by <paramref
        /// name="destinationLocalizationAsset"/>, and updates it if
        /// necessary.
        /// </summary>
        /// <param name="baseLocalizationStrings">A collection of <see
        /// cref="StringTableEntry"/></param>
        /// <param name="language">The language that <paramref
        /// name="destinationLocalizationAsset"/> provides strings
        /// for.false</param>
        /// <param name="destinationLocalizationAsset">A TextAsset
        /// containing localized strings in CSV format.</param>
        /// <returns>Whether <paramref
        /// name="destinationLocalizationAsset"/> was modified.</returns>
        private static bool UpdateLocalizationFile(IEnumerable<StringTableEntry> baseLocalizationStrings, string language, TextAsset destinationLocalizationAsset)
        {
            var translatedStrings = StringTableEntry.ParseFromCSV(destinationLocalizationAsset.text);

            // Convert both enumerables to dictionaries, for easier lookup
            var baseDictionary = baseLocalizationStrings.ToDictionary(entry => entry.ID);
            var translatedDictionary = translatedStrings.ToDictionary(entry => entry.ID);

            // The list of line IDs present in each localisation
            var baseIDs = baseLocalizationStrings.Select(entry => entry.ID);
            var translatedIDs = translatedStrings.Select(entry => entry.ID);

            // The list of line IDs that are ONLY present in each
            // localisation
            var onlyInBaseIDs = baseIDs.Except(translatedIDs);
            var onlyInTranslatedIDs = translatedIDs.Except(baseIDs);

            // Tracks if the translated localisation needed modifications
            // (either new lines added, old lines removed, or changed lines
            // flagged)
            var modificationsNeeded = false;

            // Remove every entry whose ID is only present in the
            // translated set. This entry has been removed from the base
            // localization.
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
                    // Empty this text, so that it's apparent that a
                    // translated version needs to be provided.
                    Text = string.Empty,
                    Language = language,
                };
                translatedDictionary.Add(id, newEntry);
                modificationsNeeded = true;
            }

            // Finally, we need to check for any entries in the translated
            // localisation that:
            // 1. have the same line ID as one in the base, but
            // 2. have a different Lock (the hash of the text), which
            //    indicates that the base text has changed.

            // First, get the list of IDs that are in both base and
            // translated, and then filter this list to any where the lock
            // values differ
            var outOfDateLockIDs = baseDictionary.Keys
                .Intersect(translatedDictionary.Keys)
                .Where(id => baseDictionary[id].Lock != translatedDictionary[id].Lock);

            // Now loop over all of these, and update our translated
            // dictionary to include a note that it needs attention
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
                // No changes needed to be done to the translated string
                // table entries. Stop here.
                return false;
            }

            // We need to produce a replacement CSV file for the translated
            // entries.

            var outputStringEntries = translatedDictionary.Values
                .OrderBy(entry => entry.File)
                .ThenBy(entry => int.Parse(entry.LineNumber));

            var outputCSV = StringTableEntry.CreateCSV(outputStringEntries);

            // Write out the replacement text to this existing file,
            // replacing its existing contents
            var outputFile = AssetDatabase.GetAssetPath(destinationLocalizationAsset);
            File.WriteAllText(outputFile, outputCSV, System.Text.Encoding.UTF8);

            // Tell the asset database that the file needs to be reimported
            AssetDatabase.ImportAsset(outputFile);

            // Signal that the file was changed
            return true;
        }

        internal static void AddLineTagsToFilesInYarnProject(YarnProjectImporter importer)
        {
            // First, gather all existing line tags across ALL yarn
            // projects, so that we don't accidentally overwrite an
            // existing one. Do this by finding all yarn scripts in all
            // yarn projects, and get the string tags inside them.

            var allYarnFiles =
                // get all yarn projects across the entire project
                AssetDatabase.FindAssets($"t:{nameof(YarnProject)}")
                // Get the path for each asset's GUID
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                // Get the importer for each asset at this path
                .Select(path => AssetImporter.GetAtPath(path))
                // Ensure it's a YarnProjectImporter
                .OfType<YarnProjectImporter>()
                // Get all of their source scripts, as a single sequence
                .SelectMany(i => i.sourceScripts)
                // Get the path for each asset
                .Select(sourceAsset => AssetDatabase.GetAssetPath(sourceAsset))
                // get each asset importer for that path
                .Select(path => AssetImporter.GetAtPath(path))
                // ensure that it's a YarnImporter
                .OfType<YarnImporter>()
                // get the path for each importer's asset (the compiler
                // will use this)
                .Select(i => AssetDatabase.GetAssetPath(i))
                // remove any nulls, in case any are found
                .Where(path => path != null);

#if YARNSPINNER_DEBUG
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
#endif

            // Compile all of these, and get whatever existing string tags
            // they had. Do each in isolation so that we can continue even
            // if a file contains a parse error.
            var allExistingTags = allYarnFiles.SelectMany(path =>
            {
                // Compile this script in strings-only mode to get
                // string entries
                var compilationJob = Yarn.Compiler.CompilationJob.CreateFromFiles(path);
                compilationJob.CompilationType = Yarn.Compiler.CompilationJob.Type.StringsOnly;

                var result = Yarn.Compiler.Compiler.Compile(compilationJob);

                bool containsErrors = result.Diagnostics
                    .Any(d => d.Severity == Compiler.Diagnostic.DiagnosticSeverity.Error);

                if (containsErrors) {
                    Debug.LogWarning($"Can't check for existing line tags in {path} because it contains errors.");
                    return new string[] { };
                }
                
                return result.StringTable.Where(i => i.Value.isImplicitTag == false).Select(i => i.Key);
            }).ToList(); // immediately execute this query so we can determine timing information

#if YARNSPINNER_DEBUG
            stopwatch.Stop();
            Debug.Log($"Checked {allYarnFiles.Count()} yarn files for line tags in {stopwatch.ElapsedMilliseconds}ms");
#endif

            var modifiedFiles = new List<string>();

            try
            {

                AssetDatabase.StartAssetEditing();

                foreach (var script in importer.sourceScripts)
                {
                    var assetPath = AssetDatabase.GetAssetPath(script);
                    var contents = File.ReadAllText(assetPath);

                    // Produce a version of this file that contains line
                    // tags added where they're needed.
                    var taggedVersion = Yarn.Compiler.Utility.AddTagsToLines(contents, allExistingTags);

                    // If this produced a modified version of the file,
                    // write it out and re-import it.
                    if (contents != taggedVersion)
                    {
                        modifiedFiles.Add(Path.GetFileNameWithoutExtension(assetPath));

                        File.WriteAllText(assetPath, taggedVersion, System.Text.Encoding.UTF8);
                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);
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
        /// <remarks>
        /// The file generated is in a format ready to be added to the <see
        /// cref="YarnProjectImporter.languagesToSourceAssets"/> list.
        /// </remarks>
        /// <param name="yarnProjectImporter">The YarnProjectImporter to
        /// extract strings from.</param>
        /// <param name="destination">The path to write the file
        /// to.</param>
        /// <returns><see langword="true"/> if the file was written
        /// successfully, <see langword="false"/> otherwise.</returns>
        /// <exception cref="CsvHelper.CsvHelperException">Thrown when an
        /// error is encountered when generating the CSV data.</exception>
        /// <exception cref="IOException">Thrown when an error is
        /// encountered when writing the data to disk.</exception>
        internal static bool WriteStringsFile(string destination, YarnProjectImporter yarnProjectImporter)
        {
            // Perform a strings-only compilation to get a full strings
            // table, and generate the CSV. 
            var stringTable = yarnProjectImporter.GenerateStringsTable();

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

        internal static void ConvertImplicitVariableDeclarationsToExplicit(YarnProjectImporter yarnProjectImporter) {
            var allFilePaths = yarnProjectImporter.sourceScripts.Select(textAsset => AssetDatabase.GetAssetPath(textAsset));

            var library = new Library();
            ActionManager.RegisterFunctions(library);

            var explicitDeclarationsCompilerJob = Compiler.CompilationJob.CreateFromFiles(AssetDatabase.GetAssetPath(yarnProjectImporter));

            Compiler.CompilationResult explicitResult;

            try {
                explicitResult = Compiler.Compiler.Compile(explicitDeclarationsCompilerJob);
            } catch (System.Exception e) {
                Debug.LogError($"Compile error: {e}");
                return;
            }


            var implicitDeclarationsCompilerJob = Compiler.CompilationJob.CreateFromFiles(allFilePaths, library);
            implicitDeclarationsCompilerJob.CompilationType = Compiler.CompilationJob.Type.DeclarationsOnly;
            implicitDeclarationsCompilerJob.VariableDeclarations = explicitResult.Declarations;

            Compiler.CompilationResult implicitResult;

            try {
                implicitResult = Compiler.Compiler.Compile(implicitDeclarationsCompilerJob);
            } catch (System.Exception e) {
                Debug.LogError($"Compile error: {e}");
                return;
            }

            var implicitDeclarations = implicitResult.Declarations.Where(d => !(d.Type is Yarn.FunctionType) && d.IsImplicit);

            var output = Yarn.Compiler.Utility.GenerateYarnFileWithDeclarations(explicitResult.Declarations.Concat(implicitDeclarations), "Program");

            File.WriteAllText(yarnProjectImporter.assetPath, output, System.Text.Encoding.UTF8);
            AssetDatabase.ImportAsset(yarnProjectImporter.assetPath);

        }


    }
}
