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
        // localization set to this script's base language 3. and also we
        // make sure that this project's language list includes this
        // program's base language.
        if (localizationDatabaseProperty.objectReferenceValue == null)
        {
            if (GUILayout.Button("Create New Localization Database"))
            {
                YarnImporterUtility.CreateNewLocalizationDatabase(serializedObject);
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
                    YarnImporterUtility.CreateLocalizationForLanguageInProgram(serializedObject, language);
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
                YarnImporterUtility.UpdateLocalizationCSVs(serializedObject);
            }

            EditorGUILayout.HelpBox("To add a new localization, select the Localization Database, and click Create New Localization.", MessageType.Info);
        }
    }

    

    
}

