using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using CsvHelper;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
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

        private GenericMenu languageMenu;

        private void OnEnable()
        {
            localizationsProperty = serializedObject.FindProperty("_localizations");
            trackedProgramsProperty = serializedObject.FindProperty("_trackedPrograms");
        }

        private GenericMenu CreateLanguageMenu()
        {
            var menu = new GenericMenu();

            foreach (var languageName in ProjectSettings.TextProjectLanguages)
            {
                var culture = Cultures.GetCulture(languageName);
                menu.AddItem(new GUIContent(culture.DisplayName), false, CreateLocalizationWithLanguage, languageName);
            }

            return menu;
        }

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

            // Figure out where on disk this asset is
            var path = AssetDatabase.GetAssetPath(target);
            var directory = System.IO.Path.GetDirectoryName(path);

            // Figure out a new, unique path for the localization we're
            // creating
            var destinationPath = System.IO.Path.Combine(directory, $"{serializedObject.targetObject.name}-{theLanguage}.asset");
            destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);

            // Create the asset and set it up
            var localizationAsset = ScriptableObject.CreateInstance<Localization>();
            localizationAsset.LocaleCode = theLanguage;

            // Save it to disk
            AssetDatabase.CreateAsset(localizationAsset, destinationPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(destinationPath);


            // Now that it exists, add it to the LocalizationDatabase's
            // _localizations field
            localizationsProperty.InsertArrayElementAtIndex(localizationsProperty.arraySize);
            var newProp = localizationsProperty.GetArrayElementAtIndex(localizationsProperty.arraySize - 1);
            newProp.objectReferenceValue = localizationAsset;

            // And we're done!
            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();


            foreach (SerializedProperty property in localizationsProperty)
            {
                string name;

                if (property.objectReferenceValue == null)
                {
                    name = "Add a localization...";
                }
                else
                {
                    name = (property.objectReferenceValue as Localization).LocaleCode;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(property, new GUIContent(name));
                    if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        // Remove the element from the slot in the array...
                        property.DeleteCommand();
                        // ... and remove the empty slot from the array.
                        property.DeleteCommand();
                    }
                }

            }

            if (GUILayout.Button("Add Localisation"))
            {
                localizationsProperty.InsertArrayElementAtIndex(localizationsProperty.arraySize);
                localizationsProperty.GetArrayElementAtIndex(localizationsProperty.arraySize - 1).objectReferenceValue = null;
            }

            if (serializedObject.isEditingMultipleObjects == false)
            {
                if (GUILayout.Button("Create New Localization"))
                {
                    if (languageMenu == null)
                    {
                        languageMenu = CreateLanguageMenu();
                    }

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

                    EditorGUILayout.Space();
                
                    EditorGUILayout.HelpBox("This database will automatically update when the contents of these scripts change. If you modify the .csv files for other translations, modify any locale-specific assets, or if you need to manually update the database, click Update Database.", MessageType.Info);
                }

                if (GUILayout.Button("Update Database"))
                {
                    foreach (LocalizationDatabase target in serializedObject.targetObjects)
                    {
                        UpdateContents(target);
                    }
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

        public static void UpdateContents(LocalizationDatabase database)
        {
            var allTextAssets = database.TrackedPrograms
                .SelectMany(p => p.localizations)
                .Select(localization => new
                {
                    localization.languageName,
                    localization.text
                });
            
            foreach (var localization in database.Localizations)
            {
                if (localization == null)
                {
                    // Ignore any null entries
                    continue;
                }
                localization.Clear();
            }

            foreach (var localizedTextAsset in allTextAssets)
            {
                string languageName = localizedTextAsset.languageName;

                Localization localization;

                try
                {
                    localization = database.GetLocalization(languageName);
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogWarning($"{localizedTextAsset.text.name} is marked for language {languageName}, but this {nameof(LocalizationDatabase)} isn't set up for that language. TODO: offer a quick way to create one here.");
                    continue;
                }



                TextAsset textAsset = localizedTextAsset.text;

                if (textAsset == null)
                {
                    // A null reference. Early out here.
                    continue;
                }

                var records = StringTableEntry.ParseFromCSV(textAsset.text);

                foreach (var record in records)
                {
                    AddLineEntryToLocalization(localization, record);
                }

            }
        }

        private static void AddLineEntryToLocalization(Localization localization, StringTableEntry entry)
        {
            // Add the entry for this line's text

            try {
                localization.AddLocalizedString(entry.ID, entry.Text);
            } catch (ArgumentException) {
                // An ArgumentException will be thrown by the internal
                // dictionary of the Localization if there's a duplicate
                // key
                Debug.LogError($"Can't add line {entry.ID} (\"{entry.Text}\") because this localization already has an entry for this line. Are you trying to add two copies of the same file?");
                return;
            }
            

            if (localization.AssetSourceFolder == null)
            {
                // No asset source folder specified, so don't go looking
                // for assets to add
                return;
            }

            // Remove "line:" from the ID before looking for assets
            var id = entry.ID.Replace("line:", "");

            // Look inside assetSourceFolder for any asset that includes
            // this line ID
            var assetGUIDs = AssetDatabase.FindAssets(id, new[] { AssetDatabase.GetAssetPath(localization.AssetSourceFolder) });

            if (assetGUIDs.Length == 0)
            {
                // We didn't find any asset with this name, so early out
                // here
                return;
            }

            if (assetGUIDs.Length > 1)
            {
                string assetPaths = string.Join(", ", assetGUIDs.Select(g => AssetDatabase.GUIDToAssetPath(g)));
                Debug.LogWarning($"Multiple assets found for line {id} in language {localization.LocaleCode}: {assetPaths}");
            }

            // Select the GUID for the first asset that we've found for
            // this line
            var assetGUID = assetGUIDs[0];

#if ADDRESSABLES
            AddAddressableAssetReferenceToLocalization(localization, entry, assetGUID);
#else
            // Add the asset reference directly
            AddAssetReferenceToLocalization(localization, entry, assetGUID);
#endif

        }

        private static void AddAddressableAssetReferenceToLocalization(Localization localization, StringTableEntry entry, string assetGUID)
        {
            if (AddressableAssetSettingsDefaultObject.SettingsExists == false)
            {
                // Do nothing - the user hasn't set up their addressable
                // assets settings object, so we have no place to record
                // any new addresses.
                return;
            }
            
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            // Get or create the asset reference for this asset.
            var assetReference = settings.CreateAssetReference(assetGUID);

            // Get the entry in the addressable assets settings - we want
            // to update its address to be one that a LineProvider will ask
            // for
            var addressableAssetEntry = settings.FindAssetEntry(assetGUID);

            addressableAssetEntry?.SetAddress(entry.ID + "-" + localization.LocaleCode);

            localization.AddLocalizedObjectAddress(entry.ID, assetReference);
        }

        private static void AddAssetReferenceToLocalization(Localization localization, StringTableEntry entry, string assetGUID)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(assetGUID));

            localization.AddLocalizedObject(entry.ID, asset);
        }
    }

    public class LocalizationDatabaseUpdaterPostProcessor : AssetPostprocessor
    {
        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // TODO: The eventual intent here is to have a system that
            // detects when a YarnProgram, or a .csv TextAsset that it's
            // produced, is updated or deleted, and signals a relevant
            // LocalizationDatabase to update. 
            //
            // In the meantime, LocalizationDatabases are manually updated.

            return;

            var allLocalizationDatabases = AssetDatabase.FindAssets($"t:{nameof(LocalizationDatabase)}")
                                                        .Select(path => AssetDatabase.LoadAssetAtPath<LocalizationDatabase>(path));

            if (allLocalizationDatabases.Count() == 0)
            {
                // No databases to update! Early out here.
                return;
            }

            IEnumerable<string> GetEntriesOfType<T>(string[] paths) where T : UnityEngine.Object
            {
                return paths.Where(path => AssetDatabase.GetMainAssetTypeAtPath(path)?.IsAssignableFrom(typeof(T)) ?? false);
            }

            var allImportedTextDocuments = GetEntriesOfType<TextAsset>(importedAssets);

            var allImportedYarnPrograms = GetEntriesOfType<YarnProgram>(importedAssets);

            if (deletedAssets.Length == 0 && allImportedTextDocuments.Count() == 0 && allImportedYarnPrograms.Count() == 0)
            {
                // No items were deleted, and no TextAssets or YarnPrograms
                // were created or modified. Nothing to do!
                return;
            }

            // Ok, we've modified a TextAsset or YarnProgram, or deleted an
            // asset. We need to check our localized databases and update
            // their contents.


            // Deletion: We can't find the type of assets that no longer
            // exist, so we need to check all localization databases to see
            // if an asset they were referring to has been deleted. If it
            // has, it needs to rebuild its tables.



        }
    }
}
