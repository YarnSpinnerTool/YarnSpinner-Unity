using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Yarn.Unity;

/// <summary>
/// Contains methods for performing high-level operations on Yarn scripts,
/// and their associated localization files and localization databases.
/// </summary>
internal static class YarnImporterUtility
{
    /// <summary>
    /// Creates a new localization database asset adjacent to one of the
    /// selected objects, configures all selected objects to use it, and
    /// ensures that the project's text language list includes this
    /// program's base language. A Localization asset will also be created
    /// for the base language.
    /// </summary>
    /// <param name="serializedObject">A serialized object that represents
    /// a <see cref="YarnImporter"/>.</param>
    /// <returns>The paths to the newly created assets.</returns>
    internal static string[] CreateNewLocalizationDatabase(SerializedObject serializedObject)
    {
        if (serializedObject.isEditingMultipleObjects) {
            throw new System.InvalidOperationException($"Cannot invoke {nameof(CreateNewLocalizationDatabase)} when editing multiple objects");
        }

        var createdPaths = new List<string>();

        var localizationDatabaseProperty = serializedObject.FindProperty("localizationDatabase");

        var target = serializedObject.targetObjects[0];

        // Figure out where on disk this asset is
        var path = AssetDatabase.GetAssetPath(target);
        var directory = Path.GetDirectoryName(path);

        // Figure out a new, unique path for the localization we're
        // creating
        var databaseFileName = $"LocalizationDatabase.asset";
        var destinationPath = Path.Combine(directory, databaseFileName);
        destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);

        createdPaths.Add(destinationPath);

        // Create the asset and set it up
        var localizationDatabaseAsset = ScriptableObject.CreateInstance<LocalizationDatabase>();

        // The list of languages we needed to add to the project's text language
        // list
        var languagesAddedToProject = new List<string>();

        // Attach all selected programs to this new database
        foreach (YarnImporter importer in serializedObject.targetObjects)
        {        
            var guid = AssetDatabase.AssetPathToGUID(importer.assetPath);
            localizationDatabaseAsset.AddTrackedProgram(guid);

            // If this database doesn't currently have a localization
            // for the currently selected program, add one
            var theLanguage = importer.baseLanguageID;

            if (localizationDatabaseAsset.HasLocalization(theLanguage) == false)
            {
                var localizationPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(databaseFileName)}-{theLanguage}.asset");
                localizationPath = AssetDatabase.GenerateUniqueAssetPath(localizationPath);

                // Create the asset and set it up
                var localizationAsset = ScriptableObject.CreateInstance<Localization>();
                localizationAsset.LocaleCode = theLanguage;

                AssetDatabase.CreateAsset(localizationAsset, localizationPath);

                createdPaths.Add(localizationPath);

                localizationDatabaseAsset.AddLocalization(localizationAsset);
            }

            // Add this language to the project's text language list if
            // we need to, and remember that we did so
            if (ProjectSettings.TextProjectLanguages.Contains(importer.baseLanguageID) == false) {
                ProjectSettings.TextProjectLanguages.Add(importer.baseLanguageID);
                languagesAddedToProject.Add(importer.baseLanguageID);                                        
            }
            
        }

        // Log if we needed to update the project text language list
        if (languagesAddedToProject.Count > 0) {
            Debug.Log($"The following {(languagesAddedToProject.Count == 1 ? "language was" : "languages were")} added to the project's text language list: {string.Join(", ", languagesAddedToProject)}. To review this list, choose Edit > Project Settings > Yarn Spinner.");
        }

        // Populate the database's contents
        LocalizationDatabaseUtility.UpdateContents(localizationDatabaseAsset);

        // Save it to disk
        AssetDatabase.CreateAsset(localizationDatabaseAsset, destinationPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(destinationPath);

        // Associate this localization database with the object.
        localizationDatabaseProperty.objectReferenceValue = localizationDatabaseAsset;
        
        serializedObject.ApplyModifiedProperties(); 

        return createdPaths.ToArray();       
    }

    /// <summary>
    /// Creates a new .yarnprogram asset in the same directory as the Yarn
    /// script represented by serializedObject, and configures the script's
    /// /// importer to use the new Yarn Program.
    /// </summary>
    /// <param name="serializedObject">A serialized object that represents
    /// a <see cref="YarnImporter"/>.</param>
    /// <returns>The path to the created asset.</returns>
    internal static string CreateYarnProgram(YarnImporter initialSourceAsset)
    {
        
        // Figure out where on disk this asset is
        var path = initialSourceAsset.assetPath;
        var directory = Path.GetDirectoryName(path);

        // Figure out a new, unique path for the localization we're
        // creating
        var databaseFileName = $"Program.yarnprogram";

        var destinationPath = Path.Combine(directory, databaseFileName);
        destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);

        // Create the program
        YarnEditorUtility.CreateYarnAsset(destinationPath);
        
        AssetDatabase.ImportAsset(destinationPath);
        AssetDatabase.SaveAssets();

        var programImporter = AssetImporter.GetAtPath(destinationPath) as YarnProgramImporter;
        programImporter.sourceScripts.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(path));

        EditorUtility.SetDirty(programImporter);

        // Reimport the program to make it generate its default string
        // table, if needed
        programImporter.SaveAndReimport();

        return destinationPath;
        

    }

    /// <summary>
    /// Creates a new localization CSV file for the specified object, for
    /// the given language.
    /// </summary>
    /// <param name="serializedObject">A serialized object that represents
    /// a <see cref="YarnImporter"/>.</param>
    /// <param name="language">The language to generate a localization CSV
    /// for.</param>
    /// <returns>The path to the newly created CSV file, or null if there was an error.</returns>
    internal static string CreateLocalizationForLanguageInProgram(SerializedObject serializedObject, string language)
    {
        if (serializedObject.isEditingMultipleObjects)
        {
            Debug.LogError($"{nameof(CreateLocalizationForLanguageInProgram)} was called, but multiple objects were selected. Select a single object and try again.");
            return null;
        }

        IEnumerable<StringTableEntry> baseLanguageStrings = GetBaseLanguageStringsForSelectedObject(serializedObject);

        // Produce a new version of these string entries, but with
        // different languages
        var translatedStringTable = baseLanguageStrings.Select(s =>
        {
            // Copy the entry, but mark the new language
            return new StringTableEntry(s)
            {
                Language = language
            };
        });

        // Convert this new list to a CSV
        string generatedCSV;
        try
        {
            generatedCSV = StringTableEntry.CreateCSV(translatedStringTable);
        }
        catch (CsvHelper.CsvHelperException e)
        {
            Debug.LogError($"Error creating {language} CSV: {e}");
            return null;
        }

        // Write out this CSV to a file
        var path = AssetDatabase.GetAssetPath(serializedObject.targetObject);
        var directory = Path.GetDirectoryName(path);
        var csvFileName = $"{Path.GetFileNameWithoutExtension(path)} ({language}).csv";
        var destinationPath = Path.Combine(directory, csvFileName);
        destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);        
        File.WriteAllText(destinationPath, generatedCSV, System.Text.Encoding.UTF8);
        
        // Import this file as a TextAsset object
        AssetDatabase.ImportAsset(destinationPath);
        var newTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(destinationPath);

        // Store this TextAsset in the importer, at the end of the
        // localizations array
        (serializedObject.targetObject as YarnImporter).AddLocalization(language, newTextAsset);
        
        // Mark that we just changed the target object
        EditorUtility.SetDirty(serializedObject.targetObject);

        return destinationPath;
    }


    /// <summary>
    /// Updates every localization .CSV file associated with this .yarn
    /// file.
    /// </summary>
    /// <remarks>
    /// This method updates each localization file by performing the
    /// following operations:
    ///
    /// - Inserts new entries if they're present in the base localization
    /// and not in the translated localization
    ///
    /// - Removes entries if they're present in the translated localization
    /// and not in the base localization
    ///
    /// - Detects if a line in the base localization has changed its Lock
    /// value from when the translated localization was created, and update
    /// its Comment
    /// </remarks>
    /// <param name="serializedObject">A serialized object that represents
    /// a <see cref="YarnImporter"/>.</param>
    internal static void UpdateLocalizationCSVs(SerializedObject serializedObject)
    {
        var localizationDatabaseProperty = serializedObject.FindProperty("localizationDatabase");

        if (serializedObject.isEditingMultipleObjects) {
            Debug.LogError($"Can't update localization CSVs: multiple objects are being edited.");
            return;
        }
        
        var baseLocalizationStrings = GetBaseLanguageStringsForSelectedObject(serializedObject);

        var externalLocalizations = (serializedObject.targetObject as YarnImporter).ExternalLocalizations;

        var modifiedFiles = new List<TextAsset>();

        foreach (var loc in externalLocalizations) {
            var fileWasChanged  = UpdateLocalizationFile(baseLocalizationStrings, loc.languageName, loc.text);
            
            if (fileWasChanged) {
                modifiedFiles.Add(loc.text);
            }
        }

        if (modifiedFiles.Count > 0) {
            Debug.Log($"Updated the following files: {string.Join(", ", modifiedFiles.Select(f => f.name))}");
        } else {
            Debug.Log($"No files needed updating.");
            // Update our corresponding localization database.
            if (localizationDatabaseProperty.objectReferenceValue is LocalizationDatabase database) {
                LocalizationDatabaseUtility.UpdateContents(database);
            }
        }
    }

    
    /// <summary>
    /// Returns an <see cref="IEnumerable"/> containing the string table
    /// entries for the base language for the specified Yarn script.
    /// </summary>
    /// <param name="serializedObject">A serialized object that represents
    /// a <see cref="YarnScript"/>.</param>
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

    
    /// <summary>
    /// Verifies the TextAsset referred to by <paramref name="loc"/>, and
    /// updates it if necessary.
    /// </summary>
    /// <param name="baseLocalizationStrings">A collection of <see
    /// cref="StringTableEntry"/></param>
    /// <param name="language">The language that <paramref name="loc"/>
    /// provides strings for.false</param>
    /// <param name="loc">A TextAsset containing localized strings in CSV
    /// format.</param>
    /// <returns>Whether <paramref name="loc"/> was modified.</returns>
    private static bool UpdateLocalizationFile(IEnumerable<StringTableEntry> baseLocalizationStrings, string language, TextAsset loc)
    {
        var translatedStrings = StringTableEntry.ParseFromCSV(loc.text);

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
        var outputFile = AssetDatabase.GetAssetPath(loc);
        File.WriteAllText(outputFile, outputCSV, System.Text.Encoding.UTF8);

        // Tell the asset database that the file needs to be reimported
        AssetDatabase.ImportAsset(outputFile);

        // Signal that the file was changed
        return true;
    }

    
}
