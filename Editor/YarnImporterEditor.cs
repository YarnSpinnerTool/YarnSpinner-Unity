using UnityEngine;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Yarn.Unity;

[CustomEditor(typeof(YarnImporter))]
public class YarnImporterEditor : ScriptedImporterEditor
{
    private SerializedProperty baseLanguageIdProperty;
    private SerializedProperty baseLanguageProperty;
    private SerializedProperty localizationDatabaseProperty;
    private SerializedProperty isSuccessfullyCompiledProperty;
    private SerializedProperty compilationErrorMessageProperty;
    private SerializedProperty localizationsProperty;

    public string DestinationProgramError => destinationYarnProjectImporter?.compileError ?? null;

    private YarnProject destinationYarnPrject;
    private YarnProjectImporter destinationYarnProjectImporter;

    public override void OnEnable()
    {
        base.OnEnable();

        baseLanguageIdProperty = serializedObject.FindProperty("baseLanguageID");
        baseLanguageProperty = serializedObject.FindProperty("baseLanguage");
        localizationDatabaseProperty = serializedObject.FindProperty("localizationDatabase");
        isSuccessfullyCompiledProperty = serializedObject.FindProperty("isSuccesfullyParsed");
        compilationErrorMessageProperty = serializedObject.FindProperty("parseErrorMessage");
        localizationsProperty = serializedObject.FindProperty("localizations");

        UpdateDestinationProgram();
    }

    private void UpdateDestinationProgram()
    {
        destinationYarnPrject = (target as YarnImporter).DestinationProject;

        if (destinationYarnPrject != null)
        {
            destinationYarnProjectImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(destinationYarnPrject)) as YarnProjectImporter;
        }
    }

    public override void OnInspectorGUI() {
        
        serializedObject.Update();
        EditorGUILayout.Space();

        // If there's a parse error in any of the selected objects, show an
        // error. If the selected objects have the same destination
        // program, and there's a compile error in it, show that. 
        if (string.IsNullOrEmpty(compilationErrorMessageProperty.stringValue) == false) {
            if (serializedObject.isEditingMultipleObjects) {
                EditorGUILayout.HelpBox("Some of the selected scripts have errors.", MessageType.Error);
            } else {
                EditorGUILayout.HelpBox(compilationErrorMessageProperty.stringValue, MessageType.Error);
            }                  
        } else if (string.IsNullOrEmpty(DestinationProgramError) == false) {
            
            EditorGUILayout.HelpBox(DestinationProgramError, MessageType.Error);
            
        }

        EditorGUILayout.PropertyField(baseLanguageIdProperty);

        if (destinationYarnPrject == null) {
            EditorGUILayout.HelpBox("This script is not currently part of a Yarn Project, so it can't be compiled or loaded into a Dialogue Runner. Either click Create New Yarn Project, or Assign to Existing Yarn Project.", MessageType.Info);
            if (GUILayout.Button("Create New Yarn Project...")) {
                YarnImporterUtility.CreateYarnProject(target as YarnImporter);
                
                UpdateDestinationProgram();

            }
            if (GUILayout.Button("Assign to Existing Yarn Program...")) {
                var programPath = EditorUtility.OpenFilePanelWithFilters("Select an existing Yarn Project", Application.dataPath, new string[] {"Yarn Program (.yarnprogram)", "yarnprogram"} );
                programPath = "Assets" + programPath.Substring( Application.dataPath.Length );
                if ( !string.IsNullOrEmpty(programPath) ) {
                    YarnImporterUtility.AssignScriptToProgram( (target as YarnImporter).assetPath, programPath);
                }
                UpdateDestinationProgram();
            }
            
        } else {
            using (new EditorGUI.DisabledGroupScope(true)) {
                EditorGUILayout.ObjectField("Program", destinationYarnPrject, typeof(YarnProjectImporter), false);
            }
        }

        EditorGUILayout.Space();


        // We can do localization work if all of the selected objects have
        // strings, and none of them have implicitly-created strings.
        var canCreateLocalization = serializedObject.targetObjects
            .Cast<YarnImporter>()
            .All(importer => importer.StringsAvailable && importer.AnyImplicitStringIDs == false);

        if (canCreateLocalization)
        {
            // We can work with localizations! Draw our
            // localization-related UI!
            DrawLocalizationGUI();
        }
        else if (string.IsNullOrEmpty(compilationErrorMessageProperty.stringValue))
        {
            // We have no parse errors. We can offer to add new line tags.

            string message;

            bool showReadOnlyLocalizationUI = false;

            if (localizationDatabaseProperty.objectReferenceValue != null)
            {
                // We don't have a line tag for every string, BUT we have a
                // localization database attached. This can happen when
                // we've done some loc work, and added new lines, but
                // haven't tagged those new lines. Draw a read-only view of
                // the localization database so that it's clear that the
                // setup hasn't been lost, and offer to update the line
                // tags.

                message = $"This script is set up to be localized, but not all lines have line tags. Click Add Line Tags to add them.";

                showReadOnlyLocalizationUI = true;
            }
            else
            {
                // Offer to add line tags.
                message = $"The selected {(serializedObject.isEditingMultipleObjects ? "scripts" : "script")} can't be localized, because not every line has a line tag. Click Add Line Tags to add them, or add them yourself in a text editor.";
            }

            EditorGUILayout.HelpBox(message.ToString(), MessageType.Info);

            if (GUILayout.Button("Add Line Tags"))
            {
                AddLineTagsToSelectedObject();
            }

            if (showReadOnlyLocalizationUI)
            {
                EditorGUILayout.Space();
                DrawLocalizationGUI(true);
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
        // YarnProjects, and by extension their importers, and get the
        // string tags that they found.

        var allLineTags = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.yarn") // get all yarn scripts 
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

                File.WriteAllText(assetPath, taggedVersion, System.Text.Encoding.UTF8);

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

    private void DrawLocalizationGUI(bool onlyAllowEditingDatabaseField = false)
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
                        var guid = AssetDatabase.AssetPathToGUID(importer.assetPath);
                        
                        previousLocalizationDatabase.RemoveTrackedProject(guid);
                        
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
                        var guid = AssetDatabase.AssetPathToGUID(importer.assetPath);
                        database.AddTrackedProject(guid);

                        // Mark that the localization database should save
                        // changes
                        if (previousLocalizationDatabase != null)
                        {
                            EditorUtility.SetDirty(previousLocalizationDatabase);
                        }
                    }
                } 

            }
        }

        EditorGUI.BeginDisabledGroup(onlyAllowEditingDatabaseField);

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
                var languageDisplayName = $"{Cultures.GetCulture(importer.baseLanguageID).DisplayName} (Base)";

                EditorGUILayout.PropertyField(baseLanguageProperty, new GUIContent(languageDisplayName));

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

                    bool wantsDelete = false;

                    if (assetReferenceProperty.objectReferenceValue == null) {
                        wantsDelete = true;
                    }

                    if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        wantsDelete = true;
                    }

                    if (wantsDelete) {
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
                var languageName = Cultures.GetCulture(language).DisplayName;
                
                using (new EditorGUILayout.HorizontalScope()) {
                    var rect = EditorGUILayout.GetControlRect();
                    
                    var remaining = EditorGUI.PrefixLabel(rect, new GUIContent(languageName));
                    

                    var leftSide = remaining;
                    leftSide.width /= 2;

                    var rightSide = remaining;
                    rightSide.width /= 2;
                    rightSide.x += leftSide.width;

                    var indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0; // ObjectField will try and indent, which we don't want, so temporarily unset it here
                    var droppedAsset = EditorGUI.ObjectField(leftSide, (Object)null, typeof(TextAsset), allowSceneObjects:false);

                    // Draw an object field on the left hand side
                    if (droppedAsset != null) {
                        (serializedObject.targetObject as YarnImporter).AddLocalization(language, droppedAsset as TextAsset);
                    }
                    
                    // Draw a 'create new' button on the right hand side
                    if (GUI.Button(rightSide, "Create New")) {
                        YarnImporterUtility.CreateLocalizationForLanguageInProgram(serializedObject, language);
                    }

                    // Restore cached indent level
                    EditorGUI.indentLevel = indent;

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

            if (localizationsProperty.arraySize > 0) {
                EditorGUILayout.HelpBox("If you have modified the script, click Update Localizations to update the files for the other languages.", MessageType.Info);

                if (GUILayout.Button("Update Localizations"))
                {
                    YarnImporterUtility.UpdateLocalizationCSVs(serializedObject);
                }

            }


            EditorGUILayout.HelpBox("To add a localization for a new language, select the Localization Database, and click Create New Localization.", MessageType.Info);
        }

        EditorGUI.EndDisabledGroup();
    }

    

    
}

