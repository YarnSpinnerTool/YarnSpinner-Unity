using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Yarn.Unity;

[CustomEditor(typeof(YarnImporter))]
#if UNITY_2019_1_OR_NEWER
// Only permit editing multiple objects on Unity 2019 to avoid some Unity
// 2018 bugs
[CanEditMultipleObjects]
#endif
public class YarnImporterEditor : ScriptedImporterEditor
{
    private SerializedProperty baseLanguageIdProperty;
    private SerializedProperty baseLanguageProperty;
    private SerializedProperty localizationDatabaseProperty;
    private SerializedProperty isSuccessfullyCompiledProperty;
    private SerializedProperty compilationErrorMessageProperty;
    private SerializedProperty localizationsProperty;

    public override void OnEnable() {
        base.OnEnable();

        baseLanguageIdProperty = serializedObject.FindProperty("baseLanguageID");
        baseLanguageProperty = serializedObject.FindProperty("baseLanguage");
        localizationDatabaseProperty = serializedObject.FindProperty("localizationDatabase");
        isSuccessfullyCompiledProperty = serializedObject.FindProperty("isSuccesfullyCompiled");
        compilationErrorMessageProperty = serializedObject.FindProperty("compilationErrorMessage");
        localizationsProperty = serializedObject.FindProperty("localizations");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();
        EditorGUILayout.Space();

        // If there's a compilation error in any of the selected objects,
        // show an error and then stop.
        if (isSuccessfullyCompiledProperty.boolValue == false) {
            if (serializedObject.isEditingMultipleObjects) {
                EditorGUILayout.HelpBox("Some of the selected scripts have errors.", MessageType.Error);
            } else {
                EditorGUILayout.HelpBox($"Error in script:\n{compilationErrorMessageProperty.stringValue}", MessageType.Error);
            }      
            return;      
        }

        EditorGUILayout.PropertyField(baseLanguageIdProperty);

        EditorGUILayout.Space();

        // We can do localization work if all of the selected objects have
        // strings, and none of them have implicitly-created strings.
        var canCreateLocalization = serializedObject.targetObjects
            .Cast<YarnImporter>()
            .All(importer => importer.StringsAvailable && importer.AnyImplicitStringIDs == false);

        if (canCreateLocalization) {
            // We can work with localizations! Draw our
            // localization-related UI!
            DrawLocalizationGUI();
        } else {
            var message = new System.Text.StringBuilder();
            message.Append($"The selected {(serializedObject.isEditingMultipleObjects ? "scripts" : "script")} can't be localized, because not every line has a line tag. Click Add Line Tags to add them, or add them yourself in a text editor.");

            EditorGUILayout.HelpBox(message.ToString(), MessageType.Info);

            if (GUILayout.Button("Add Line Tags")) {
                AddLineTagsToSelectedObject();
            }
        }

        var hadChanges = serializedObject.ApplyModifiedProperties();

#if UNITY_2018
        // Unity 2018's ApplyRevertGUI is buggy, and doesn't automatically
        // detect changes to the importer's serializedObject. This means
        // that we'd need to track the state of the importer, and don't
        // have a way to present a Revert button. 
        //
        // Rather than offer a broken experience, on Unity 2018 we
        // immediately reimport the changes. This is slow (we're
        // serializing and writing the asset to disk on every property
        // change!) but ensures that the writes are done.
        if (hadChanges)
        {
            // Manually perform the same tasks as the 'Apply' button would
            ApplyAndImport();
        }
#endif

#if UNITY_2019_1_OR_NEWER
        // On Unity 2019 and newer, we can use an ApplyRevertGUI that works
        // identically to the built-in importer inspectors.
        ApplyRevertGUI();
#endif
    }

    private void AddLineTagsToSelectedObject()
    {
        // First, gather all existing line tags, so that we don't
        // accidentally overwrite an existing one. Do this by finding _all_
        // YarnPrograms, and by extension their importers, and get the
        // string tags that they found.

        var allLineTags = Resources.FindObjectsOfTypeAll<YarnProgram>() // get all yarn programs that have been imported
            .Select(asset => AssetDatabase.GetAssetOrScenePath(asset)) // get the path on disk
            .Select(path => AssetImporter.GetAtPath(path)) // get the asset importer for that path
            .OfType<YarnImporter>() // ensure that they're all YarnImporters
            .SelectMany(importer => importer.stringIDs) // Get all of the string IDs in the base localization
            .ToList(); // get all string IDs, flattened into one list 

        var modifiedFiles = new List<string>();

        foreach (var importer in serializedObject.targetObjects.Cast<YarnImporter>())
        {
            var assetPath = importer.assetPath;
            var contents = File.ReadAllText(assetPath);

            // Produce a version of this file that contains line tags added
            // where they're needed.
            var taggedVersion = Yarn.Compiler.Utility.AddTagsToLines(contents, allLineTags);

            // If this produced a modified version of the file, write it out and re-import it.
            if (contents != taggedVersion)
            {
                modifiedFiles.Add(Path.GetFileNameWithoutExtension(assetPath));

                File.WriteAllText(assetPath, taggedVersion);

                AssetDatabase.ImportAsset(assetPath);
            }
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

    private void DrawLocalizationGUI()
    {
        using (var changed = new EditorGUI.ChangeCheckScope())
        {
            var previousLocalizationDatabase = localizationDatabaseProperty.objectReferenceValue as LocalizationDatabase;

            // Show the 'localization database' property
            EditorGUILayout.PropertyField(localizationDatabaseProperty);

            // If this changed to a valid value, update that database so
            // that it tracks all selected programs
            if (changed.changed)
            {
                var newObjectReference = localizationDatabaseProperty.objectReferenceValue;

                if (previousLocalizationDatabase != null && previousLocalizationDatabase != newObjectReference ) {
                    // The property used to refer to a localization
                    // database, but that's changed. Tell the previous
                    // value to stop tracking this program.                    
                    foreach (YarnImporter importer in serializedObject.targetObjects)
                    {
                        if (importer.programContainer == null)
                        {
                            continue;
                        }
                        previousLocalizationDatabase.RemoveTrackedProgram(importer.programContainer);
                        
                        // Mark that the localization database has changed,
                        // so needs to be saved
                        EditorUtility.SetDirty(previousLocalizationDatabase);
                    }
                }

                // Tell the new database that it should track us
                if (newObjectReference is LocalizationDatabase database)
                {
                    foreach (YarnImporter importer in serializedObject.targetObjects)
                    {
                        // If we don't actually have a program (because of
                        // a compile error), there's nothing to do here
                        if (importer.programContainer == null)
                        {
                            continue;
                        }
                        database.AddTrackedProgram(importer.programContainer);

                        // Mark that the localization database should save
                        // changes
                        EditorUtility.SetDirty(previousLocalizationDatabase);
                    }
                } 

            }
        }

        // If no localization database is provided, offer a button that
        // will create a new one that 1. tracks this script 2. has a
        // localization set to this script's base language
        if (localizationDatabaseProperty.objectReferenceValue == null)
        {
            if (GUILayout.Button("Create New Localization Database"))
            {
                CreateNewLocalizationDatabase();
            }
        }

        // For every localization in the localization database:
        // - If we have a TextAsset for it, show it here
        // - If we don't, create a button that creates one
        //
        // We only do this if we're editing a single object, because each
        // separate script will have its own translations.
        if (serializedObject.isEditingMultipleObjects == false && localizationDatabaseProperty.objectReferenceValue != null)
        {
            EditorGUI.indentLevel += 1;
            var importer = serializedObject.targetObject as YarnImporter;
            var localizationDatabase = localizationDatabaseProperty.objectReferenceValue as LocalizationDatabase;

            var languagesList = new List<string>();
            languagesList.Add(importer.baseLanguageID);

            // Expose the base language asset in the inspector, but disable
            // it because it's always a derived sub-asset
            using (new EditorGUI.DisabledScope(true))
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(baseLanguageProperty, new GUIContent(importer.baseLanguageID));

                // Not actually used, but makes this base language item
                // visually consistent with the additional ones below
                GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false));
            }


            foreach (SerializedProperty localization in localizationsProperty)
            {
                var nameProperty = localization.FindPropertyRelative("languageName");
                var assetReferenceProperty = localization.FindPropertyRelative("text");
                var languageName = nameProperty.stringValue;
                var languageDisplayName = Cultures.GetCulture(languageName).DisplayName;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(assetReferenceProperty, new GUIContent(languageDisplayName));

                    if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        // We delete this property twice:
                        // - once to clear the value from the array entry
                        // - again to remove the cleared entry from the
                        //   array 
                        //
                        // (If the entry is already empty, the first delete
                        // will remove it; the second delete appears to be
                        // a no-op, so it's safe.)
                        localization.DeleteCommand();
                        localization.DeleteCommand();
                    }
                }


                // Mark that we've seen this language name
                languagesList.Add(languageName);
            }

            // For each language that's present in the localization
            // database but not present in this script, offer buttons that
            // create a CSV for that language
            var languagesMissing = localizationDatabase.GetLocalizationLanguages().Except(languagesList);

            foreach (var language in languagesMissing)
            {
                if (GUILayout.Button($"Create {language} Localization"))
                {
                    CreateLocalizationForLanguageInCurrentObject(language);
                }
            }

            // Show a warning for any languages that the script has a
            // localization for, but that the database doesn't call for
            var languagesExtraneous = languagesList.Except(localizationDatabase.GetLocalizationLanguages());

            if (languagesExtraneous.Count() > 0)
            {
                EditorGUILayout.HelpBox($"This script has localizations for the following languages, but the localization database isn't set up to use them: {string.Join(", ", languagesExtraneous)}", MessageType.Warning);
            }

            // TODO: is it possible to interleave the property fields for
            // existing localisations with buttons, in alphabetical order
            // of language code?

            EditorGUI.indentLevel -= 1;

            if (GUILayout.Button("Update Localizations"))
            {
                UpdateLocalizationCSVs();
            }

            EditorGUILayout.HelpBox("To add a new localization, select the Localization Database, and click Create New Localization.", MessageType.Info);
        }
    }

    private void UpdateLocalizationCSVs()
    {
        // Update every .CSV file associated with this .yarn file:
        // - Insert new entries if they're present in the base localization
        //   and not in the translated localization
        // - Remove entries if they're present in the translated
        //   localization and not in the base localization
        // - Detect if a line in the base localization has changed its Lock
        //   value from when the translated localization was created, and
        //   update its Comment

        if (serializedObject.isEditingMultipleObjects) {
            Debug.LogError($"Can't update localization CSVs: multiple objects are being edited.");
            return;
        }
        
        var baseLocalizationStrings = GetBaseLanguageStringsForSelectedObject();

        var localizations = (serializedObject.targetObject as YarnImporter).localizations;

        var modifiedFiles = new List<TextAsset>();

        foreach (var loc in localizations) {
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
                LocalizationDatabaseEditor.UpdateContents(database);
            }
        }
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
    private bool UpdateLocalizationFile(IEnumerable<StringTableEntry> baseLocalizationStrings, string language, TextAsset loc)
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
        File.WriteAllText(outputFile, outputCSV);

        // Tell the asset database that the file needs to be reimported
        AssetDatabase.ImportAsset(outputFile);

        // Signal that the file was changed
        return true;
    }

    private void CreateLocalizationForLanguageInCurrentObject(string language)
    {
        if (serializedObject.isEditingMultipleObjects)
        {
            Debug.LogError($"{nameof(CreateLocalizationForLanguageInCurrentObject)} was called, but multiple objects were selected. Select a single object and try again.");
            return;
        }

        IEnumerable<StringTableEntry> baseLanguageStrings = GetBaseLanguageStringsForSelectedObject();

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
            return;
        }

        // Write out this CSV to a file
        var path = AssetDatabase.GetAssetPath(serializedObject.targetObject);
        var directory = Path.GetDirectoryName(path);
        var csvFileName = $"{Path.GetFileNameWithoutExtension(path)} ({language}).csv";
        var destinationPath = Path.Combine(directory, csvFileName);
        destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);        
        File.WriteAllText(destinationPath, generatedCSV);
        
        // Import this file as a TextAsset object
        AssetDatabase.ImportAsset(destinationPath);
        var newTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(destinationPath);

        // Store this TextAsset in the importer, at the end of the
        // localizations array
        ArrayUtility.Add(ref (target as YarnImporter).localizations,  new YarnProgram.YarnTranslation(language, newTextAsset));

        // Mark that we just changed the target object
        EditorUtility.SetDirty(target);
    }

    private IEnumerable<StringTableEntry> GetBaseLanguageStringsForSelectedObject()
    {
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

    // Creates a new localization database asset adjacent to one of the
    // selected objects, and configures all selected objects to use it.
    private void CreateNewLocalizationDatabase()
    {
        var target = serializedObject.targetObjects[0];

        // Figure out where on disk this asset is
        var path = AssetDatabase.GetAssetPath(target);
        var directory = Path.GetDirectoryName(path);

        // Figure out a new, unique path for the localization we're
        // creating
        var databaseFileName = $"LocalizationDatabase.asset";
        var destinationPath = Path.Combine(directory, databaseFileName);
        destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);

        // Create the asset and set it up
        var localizationDatabaseAsset = CreateInstance<LocalizationDatabase>();

        // Attach all selected programs to this new database
        foreach (YarnImporter importer in serializedObject.targetObjects)
        {
            if (importer.programContainer != null)
            {
                localizationDatabaseAsset.AddTrackedProgram(importer.programContainer);

                // If this database doesn't currently have a localization
                // for the currently selected program, add one
                var theLanguage = importer.baseLanguageID;

                if (localizationDatabaseAsset.HasLocalization(theLanguage) == false)
                {
                    var localizationPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(databaseFileName)}-{theLanguage}.asset");
                    localizationPath = AssetDatabase.GenerateUniqueAssetPath(localizationPath);

                    // Create the asset and set it up
                    var localizationAsset = CreateInstance<Localization>();
                    localizationAsset.LocaleCode = theLanguage;

                    AssetDatabase.CreateAsset(localizationAsset, localizationPath);

                    localizationDatabaseAsset.AddLocalization(localizationAsset);
                }
            }
        }

        // Populate the database's contents
        LocalizationDatabaseEditor.UpdateContents(localizationDatabaseAsset);

        // Save it to disk
        AssetDatabase.CreateAsset(localizationDatabaseAsset, destinationPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(destinationPath);

        // Associate this localization database with the object.
        localizationDatabaseProperty.objectReferenceValue = localizationDatabaseAsset;
    }
}

