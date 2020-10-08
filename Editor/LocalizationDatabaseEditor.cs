using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

#if ADDRESSABLES
using UnityEditor.AddressableAssets;
#endif

namespace Yarn.Unity
{


    [CustomEditor(typeof(LocalizationDatabase))]
    [CanEditMultipleObjects]
    public class LocalizationDatabaseEditor : Editor
    {
        SerializedProperty localizationsProperty;
        SerializedProperty trackedProgramsProperty;

        private void OnEnable()
        {
            localizationsProperty = serializedObject.FindProperty("_localizations");
            trackedProgramsProperty = serializedObject.FindProperty("_trackedScripts");
        }

        /// <summary>
        /// Creates a menu containing items for each available language,
        /// and for adding new languages.
        /// </summary>
        /// <returns>The prepared menu.</returns>
        private GenericMenu CreateLanguageMenu()
        {
            var menu = new GenericMenu();

            // Build the list of locales that are present in the currently
            // selected LocalizationDatabase(s).
            var locales = new List<string>();
            foreach (SerializedProperty localization in localizationsProperty)
            {
                if (localization.objectReferenceValue == null)
                {
                    continue;
                }
                var localeCode = (localization.objectReferenceValue as Localization).LocaleCode;

                locales.Add(localeCode);
            }

            foreach (var languageName in ProjectSettings.TextProjectLanguages)
            {
                var culture = Cultures.GetCulture(languageName);

                // A GUIContent for showing the display name of the culture
                GUIContent languageDisplayNameContent = new GUIContent(culture.DisplayName);

                // Does this language already exist in the selected
                // database(s)?
                if (locales.Contains(languageName))
                {
                    // Then add a disabled menu item to represent the fact
                    // that it's a valid language, but can't be added again
                    menu.AddDisabledItem(languageDisplayNameContent);
                }
                else
                {
                    menu.AddItem(
                        languageDisplayNameContent,
                        false,
                        CreateLocalizationWithLanguage,
                        languageName);
                }
            }

            // If there were zero languages available to add, include a
            // note in this menu
            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("No languages available"));
            }

            // Finally, add a quick way to get to the screen that lets us
            // add new languages
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Add More Languages..."), false, ShowYarnSpinnerProjectSettings);

            return menu;
        }

        /// <summary>
        /// Displays the Yarn Spinner project settings. Invoked from the
        /// menu created in <see cref="CreateLanguageMenu"/>.
        /// </summary>
        private void ShowYarnSpinnerProjectSettings()
        {
            SettingsService.OpenProjectSettings("Project/Yarn Spinner");
        }

        
        /// <summary>
        /// Invoked by the menu system to create a new localization on the
        /// currently selected objects with a given language.
        /// </summary>
        /// <param name="language">The language to use. This must be a
        /// string.</param>
        private void CreateLocalizationWithLanguage(object language)
        {
            if (!(language is string theLanguage))
            {
                throw new ArgumentException("Expected to receive a string", nameof(language));
            }

            if (serializedObject.isEditingMultipleObjects)
            {
                Debug.LogWarning($"Can't create a new {nameof(Localization)} when multiple objects are selected");
                return;
            }

            LocalizationDatabaseUtility.CreateLocalizationWithLanguage(serializedObject, theLanguage);
        }

        

        public override void OnInspectorGUI()
        {
            // If true, at least one of the Localizations in the selected
            // LocalizationDatabases are null references; in this case, the
            // Add New Localization button will be disabled (it will be
            // re-enabled when the empty field is filled)
            bool anyLocalizationsAreNull = false;

            if (localizationsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox($"This {ObjectNames.NicifyVariableName(nameof(LocalizationDatabase)).ToLowerInvariant()} has no {ObjectNames.NicifyVariableName(nameof(Localization)).ToLowerInvariant()}s. Create a new one, or add an existing one.", MessageType.Info);
            }

            foreach (SerializedProperty property in localizationsProperty)
            {
                // The locale code for this localization ("en-AU")
                string localeCode = null;

                // The human-friendly code for this localization ("English
                // (Australia)")
                string localeCodeDisplayName = null;

                // If true, a localization asset is present in this
                // property
                bool localizationPresent = false;

                // If true, the locale code for this localization asset
                // exists inside the Cultures class's list
                bool cultureIsValid = false;

                if (property.objectReferenceValue != null)
                {
                    localeCode = (property.objectReferenceValue as Localization).LocaleCode;
                    localizationPresent = true;

                    cultureIsValid = Cultures.HasCulture(localeCode);

                    if (cultureIsValid)
                    {
                        localeCodeDisplayName = Cultures.GetCulture(localeCode).DisplayName;
                    }
                    else
                    {
                        localeCodeDisplayName = "Invalid";
                    }
                }
                else
                {
                    // This property is empty. We'll end up drawing an
                    // empty object field here; record this so that we know
                    // to disable the 'Add Existing' button later.
                    anyLocalizationsAreNull = true;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    // If a localization is present, show the display name
                    // and locale code; otherwise, show null (which will
                    // make the corresponding empty field take up the whole
                    // width)
                    string labelContents;
                    if (localizationPresent)
                    {
                        labelContents = localeCodeDisplayName + $" ({localeCode})";
                    }
                    else
                    {
                        labelContents = "";
                    }

                    // Show the property field for this element in the
                    // array
                    EditorGUILayout.PropertyField(property, new GUIContent(labelContents));

                    // Show a button that removes this element
                    if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        // Remove the element from the slot in the array...
                        property.DeleteCommand();
                        // ... and remove the empty slot from the array.
                        property.DeleteCommand();
                    }
                }

                // Is a locale code set that's invalid?
                if (localeCode == null)
                {
                    EditorGUILayout.HelpBox($"Drag and drop a {nameof(Localization)} to this field to add it to this localization database.", MessageType.Info);
                }
                else
                {

                    if (cultureIsValid == false)
                    {
                        // A locale code was set, but this locale code
                        // isn't valid. Show a warning.
                        EditorGUILayout.HelpBox($"'{localeCode}' is not a valid locale. This localization's contents won't be available in the game.", MessageType.Warning);
                    }
                    else if (ProjectSettings.TextProjectLanguages.Contains(localeCode) == false)
                    {
                        // The locale is valid, but the project settings
                        // don't include this language. Show a warning (the
                        // user won't be able to select this localization)
                        const string fixButtonLabel = "Add to Language List";

                        EditorGUILayout.HelpBox($"{localeCodeDisplayName} is not in this project's language list. This localization's contents won't be available in the game.\n\nClick {fixButtonLabel} to fix this issue.", MessageType.Warning);

                        if (GUILayout.Button(fixButtonLabel))
                        {
                            ProjectSettings.AddNewTextLanguage(localeCode);

                            // This will resolve the error, so we'll
                            // immediately repaint 
                            Repaint();
                        }

                        // Nice little space to visually associate the
                        // 'add' button with the field and reduce confusion
                        EditorGUILayout.Space();
                    }
                }
            }

            // Show the buttons for adding and creating localizations only
            // if we're not editing multiple databases.            
            if (serializedObject.isEditingMultipleObjects == false)
            {

                // Disable the 'add existing' button if there's already an
                // empty field. (Clicking this button adds a new empty field,
                // so we don't want to end up creating multiples.)
                using (new EditorGUI.DisabledScope(anyLocalizationsAreNull))
                {
                    // Show the 'add existing' button, which adds a new
                    // empty field for the user to drop an existing
                    // Localization asset into.
                    if (GUILayout.Button("Add Existing Localisation"))
                    {
                        localizationsProperty.InsertArrayElementAtIndex(localizationsProperty.arraySize);
                        localizationsProperty.GetArrayElementAtIndex(localizationsProperty.arraySize - 1).objectReferenceValue = null;
                    }
                }

                // Show the 'create new' button, which displays the menu of
                // available languages; selecting a language causes a new
                // localization asset to be created with that language, and
                // adds it to this database.
                if (GUILayout.Button("Create New Localization"))
                {
                    var languageMenu = CreateLanguageMenu();

                    languageMenu.ShowAsContext();
                }
            }

            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            // Show information about the scripts we're pulling data from,
            // and offer to update the database now
            if (trackedProgramsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No Yarn scripts currently use this database.\n\nTo make a Yarn script use this database, select one, and set its Localization Database to this file.", MessageType.Info);
            }
            else
            {

                if (serializedObject.isEditingMultipleObjects == false)
                {
                    EditorGUILayout.HelpBox("This database will automatically update when the contents of these scripts change. If you modify the .csv files for other translations, modify any locale-specific assets, or if you need to manually update the database, click Update Database.", MessageType.Info);

                    if (GUILayout.Button("Update Database"))
                    {
                        foreach (LocalizationDatabase target in serializedObject.targetObjects)
                        {
                            LocalizationDatabaseUtility.UpdateContents(target);
                        }
                    }

                    EditorGUILayout.LabelField("Uses lines from:");

                    EditorGUI.indentLevel += 1;

                    // List every tracked program, but disable it (we don't
                    // change them here, they're changed in the inspector for
                    // the Yarn script.)
                    using (new EditorGUI.DisabledScope(true))
                    {
                        foreach (SerializedProperty trackedProgramProperty in trackedProgramsProperty)
                        {
                            EditorGUILayout.PropertyField(trackedProgramProperty, new GUIContent());
                        }
                    }

                    EditorGUI.indentLevel -= 1;

                    

                    
                }

                

#if ADDRESSABLES
                // Give a helpful note if addressables are availalbe, but
                // haven't been set up. (In this circumstance,
                // Localizations won't be able to store references to the
                // assets they find.)
                if (AddressableAssetSettingsDefaultObject.SettingsExists == false)
                {
                    EditorGUILayout.HelpBox("The Addressable Assets package has been added, but it hasn't been set up yet. Assets associated with lines won't be included in this database.\n\nTo set up Addressable Assets, choose Window > Asset Management > Addressables > Groups.", MessageType.Info);
                }
#endif
            }

            serializedObject.ApplyModifiedProperties();
        }

        
    }
}
