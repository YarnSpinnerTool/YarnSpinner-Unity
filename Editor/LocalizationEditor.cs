using UnityEngine;
using UnityEditor;
using Yarn.Unity;
using System.Linq;
using System.Collections.Generic;

#if ADDRESSABLES
using UnityEditor.AddressableAssets;
#endif

[CustomEditor(typeof(Localization))]
[CanEditMultipleObjects]
public class LocalizationEditor : Editor {

    private SerializedProperty languageNameProperty;
    private SerializedProperty assetSourceFolderProperty;

    private void OnEnable() {
        languageNameProperty = serializedObject.FindProperty("_LocaleCode");
        assetSourceFolderProperty = serializedObject.FindProperty("assetSourceFolder");
    }

    public override void OnInspectorGUI() {
        
        
        var cultures = Cultures.GetCultures().ToList();

        var cultureDisplayNames = cultures.Select(c => c.DisplayName);

        var selectedIndex = cultures.FindIndex(c => c.Name == languageNameProperty.stringValue);
        if (languageNameProperty.hasMultipleDifferentValues) {
            selectedIndex = -1;
        }

        using (new EditorGUI.DisabledScope(selectedIndex == -1)) // disable popup if multiple values present
        using (var change = new EditorGUI.ChangeCheckScope()) {
            selectedIndex = EditorGUILayout.Popup("Language", selectedIndex, cultureDisplayNames.ToArray());

            if (change.changed) {
                languageNameProperty.stringValue = cultures[selectedIndex].Name;
            }            
        }

        EditorGUILayout.PropertyField(assetSourceFolderProperty);

        EditorGUILayout.Space();

        if (serializedObject.isEditingMultipleObjects) {
            EditorGUILayout.HelpBox($"Select a single {nameof(Localization).ToLowerInvariant()} to view its contents.", MessageType.None);
        } else {
            var target = this.target as Localization;

            var hasAssets = assetSourceFolderProperty.objectReferenceValue != null;

            DrawLocalizationContents(target, hasAssets);
        }
        
        if (serializedObject.hasModifiedProperties) {
            serializedObject.ApplyModifiedProperties();
        }
        
    }

    private struct LocalizedLineEntry {
        public string id;
        public string text;
        public Object asset;
    }

    /// <summary>
    /// Displays the contents of <paramref name="target"/> as a table.
    /// </summary>
    /// <param name="target">The <see cref="Localization"/> to show the
    /// contents of.</param>
    /// <param name="showAssets">If true, this method will show any assets
    /// or addressable assets. If false, this method will only show the
    /// localized text.</param>
    private void DrawLocalizationContents(Localization target, bool showAssets)
    {
        var lineKeys = target.GetLineIDs();

        // Early out if we don't have any lines
        if (lineKeys.Count() == 0) {
            EditorGUILayout.HelpBox($"This {nameof(Localization).ToLowerInvariant()} does not contain any lines.", MessageType.Info);
            return;
        }

        var localizedLineContent = new List<LocalizedLineEntry>();

        var anyAssetsFound = false;

        foreach (var key in lineKeys) {
            
            var entry = new LocalizedLineEntry();

            entry.id = key;

            // Get the localized text for this line.
            entry.text = target.GetLocalizedString(key);

            if (showAssets)
            {
                if (ProjectSettings.AddressableVoiceOverAudioClips)
                {
#if ADDRESSABLES
                    if (target.ContainsLocalizedObjectAddress(key)) {
                        var address = target.GetLocalizedObjectAddress(key);

                        var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(address.AssetGUID));

                        entry.asset = asset;

                        anyAssetsFound = true;                        
                    }
#endif
                }
                else
                {
                    if (target.ContainsLocalizedObject<Object>(key))
                    {
                        var o = target.GetLocalizedObject<Object>(key);
                        entry.asset = o;
                        anyAssetsFound = true;
                    }
                }

            }

            localizedLineContent.Add(entry);
        }

        if (showAssets && anyAssetsFound == false) {
            // We were asked to show asset references, but didn't find any.
            // This Localization has had its source assets folder set, but
            // the LocalizationDatabase hasn't run an update to populate
            // its asset table, or, if it has, it found no assets. Assume
            // the former case and show a help box instructing the user to
            // update the database.

            string localizationName = nameof(Localization).ToLowerInvariant();
            string assetSourceFolderName = ObjectNames.NicifyVariableName(nameof(Localization.AssetSourceFolder)).ToLowerInvariant();
            string localizationDatabaseName = ObjectNames.NicifyVariableName(nameof(LocalizationDatabase)).ToLowerInvariant();
            
            var message = $"This {localizationName} has an {assetSourceFolderName}, but no assets for any of its lines. Select the {localizationDatabaseName} that uses this {localizationName}, and click Update.\n\nIf you still see this message, check that the files in the {assetSourceFolderName} include your script's line IDs in their file names.";

            EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        foreach (var entry in localizedLineContent) {

            var idContent = new GUIContent(entry.id);

            // Create a GUIContent that contains the string as its text and
            // also as its tooltip. This allows the user to mouse over this
            // line in the inspector and see more of it.
            var lineContent = new GUIContent(entry.text, entry.text);

            // Show the line ID and localized text
            EditorGUILayout.LabelField(idContent, lineContent);

            if (showAssets) {
                // Asset references are never editable here - they're only
                // updated by the Localization Database. Add a
                // DisabledGroup here to make all ObjectFields be
                // interactable, but read-only.
                EditorGUI.BeginDisabledGroup(true);

                // Show the object field
                EditorGUILayout.ObjectField(" ", entry.asset, typeof(UnityEngine.Object), false);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space();
            }

        }
    }
}
