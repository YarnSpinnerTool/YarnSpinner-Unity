/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yarn.Unity;

#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
#endif

#nullable enable

namespace Yarn.Unity.Editor
{
    internal class ImportLocalizationFromAssetWindow : EditorWindow
    {
        private System.Type assetType;

        public LocalizationEditor Target { get; private set; }

        public string FieldLabel { get; set; } = "Source Asset";
        public string? HelpBox { get; set; }

        public static ImportLocalizationFromAssetWindow Show<T>(LocalizationEditor target, string windowTitle, System.Action<T> onImport) where T : UnityEngine.Object
        {
            var window = EditorWindow.GetWindow<ImportLocalizationFromAssetWindow>(true, windowTitle);
            window.Target = target;
            window.assetType = typeof(T);
            window.maxSize = new Vector2(300, 200);
            window.onImport = (Object obj) => onImport((T)obj);
            window.ShowUtility();
            return window;
        }

        UnityEngine.Object? asset = null;
        private System.Action<UnityEngine.Object>? onImport;

        public void OnGUI()
        {
            if (Target == null)
            {
                // Our target went away; close this window
                this.Close();
            }

            asset = EditorGUILayout.ObjectField(FieldLabel, asset, assetType, allowSceneObjects: false);

            if (string.IsNullOrEmpty(this.HelpBox) == false)
            {
                EditorGUILayout.HelpBox(this.HelpBox, MessageType.Info);
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(asset == null))
            {
                if (GUILayout.Button("Import") && asset != null)
                {
                    onImport?.Invoke(asset);
                    this.Close();
                }
            }
        }
    }
    [CustomEditor(typeof(Localization))]
    [CanEditMultipleObjects]
    public class LocalizationEditor : UnityEditor.Editor
    {
        private SerializedProperty entriesProperty;
        private AudioClip lastPreviewed;
        private List<Culture> cultures;
        private int currentPickerWindow;

        private void OnEnable()
        {
            entriesProperty = serializedObject.FindProperty(nameof(Localization.entries));
            lastPreviewed = null;
            cultures = Cultures.GetCultures().ToList();

        }

        public override void OnInspectorGUI()
        {
            var target = this.target as Localization;
            if (target == null)
            {
                throw new System.InvalidOperationException($"Target is not a {typeof(Localization)}");
            }

            var isSubAsset = AssetDatabase.IsSubAsset(target);

            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox($"Select a single {nameof(Localization).ToLowerInvariant()} to view its contents.", MessageType.None);
            }
            else
            {
                if (isSubAsset)
                {
                    DrawLocalizationContentsPreview(target);
                }
                else
                {
                    if (GUILayout.Button("Import String from Yarn Project"))
                    {
                        var window = ImportLocalizationFromAssetWindow.Show<YarnProject>(this, "Import from Yarn Project", ImportFromYarnProject);
                        window.FieldLabel = "Yarn Project";
                        window.HelpBox = $"The lines in the base localisation of the selected Yarn Project will be imported into this {nameof(Localization)}.";
                    }
                    if (GUILayout.Button("Import Strings from CSV"))
                    {
                        var window = ImportLocalizationFromAssetWindow.Show<TextAsset>(this, "Import from Yarn Project", ImportFromCSV);
                        window.FieldLabel = "CSV File";
                        window.HelpBox = $"The string table entries from the selected CSV file will be imported into this {nameof(Localization)}.\n\nYou can generate a CSV file to use by selecting the Yarn Project and clicking {YarnProjectImporterEditor.AddStringTagsButtonLabel}. You can then translate the CSV file into your target language, and then import it using this window.";
                    }
                    if (GUILayout.Button("Import Assets from Folder"))
                    {
                        var window = ImportLocalizationFromAssetWindow.Show<DefaultAsset>(this, "Import from Yarn Project", (folder) =>
                        {
                            var lineIDs = target.entries.Keys;
                            var paths = YarnProjectUtility.FindAssetPathsForLineIDs(lineIDs, AssetDatabase.GetAssetPath(folder), typeof(UnityEngine.Object));
                            foreach (var path in paths)
                            {
                                var lineID = path.Key;
                                var assetPath = path.Value;

                                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                                target.AddLocalizedObjectToAsset<UnityEngine.Object>(lineID, asset);
                            }
                            serializedObject.Update();

                            EditorUtility.SetDirty(target);
                            AssetDatabase.SaveAssetIfDirty(target);
                        });
                        window.FieldLabel = "Folder";
                    }
                    EditorGUILayout.PropertyField(entriesProperty);
                }

            }

            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }

        }

        private struct LocalizedLineEntry
        {
            public string id;
            public string text;
            public Object asset;
        }

        /// <summary>
        /// Displays the contents of <paramref name="target"/> as a table.
        /// </summary>
        /// <param name="target">The <see cref="Localization"/> to show the
        /// contents of.</param>
        /// <param name="showAssets">If true, this method will show any
        /// assets or addressable assets. If false, this method will only
        /// show the localized text.</param>
        private void DrawLocalizationContentsPreview(Localization target)
        {
            var lineKeys = target.GetLineIDs();

            // Early out if we don't have any lines
            if (lineKeys.Count() == 0)
            {
                EditorGUILayout.HelpBox($"This {nameof(Localization).ToLowerInvariant()} does not contain any lines.", MessageType.Info);
                return;
            }

            var localizedLineContent = new List<LocalizedLineEntry>();

            var anyAssetsFound = false;

#if USE_ADDRESSABLES
            Dictionary<string, UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> allAddressEntries = null;

            if (target.ContainsLocalizedAssets && target.UsesAddressableAssets)
            {
                allAddressEntries = AddressableAssetSettingsDefaultObject.Settings.groups.SelectMany(g => g.entries).ToDictionary(e => e.address);

            }
#endif

            foreach (var key in lineKeys)
            {

                var entry = new LocalizedLineEntry();

                entry.id = key;

                // Get the localized text for this line.
                entry.text = target.GetLocalizedString(key);

                if (target.ContainsLocalizedAssets && target.UsesAddressableAssets)
                {
#if USE_ADDRESSABLES
                    string address = Localization.GetAddressForLine(key, target.LocaleCode);

                    if (allAddressEntries.TryGetValue(address, out var addressableAssetEntry))
                    {
                        entry.asset = AssetDatabase.LoadAssetAtPath<Object>(addressableAssetEntry.AssetPath);
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



                localizedLineContent.Add(entry);
            }

            foreach (var entry in localizedLineContent)
            {

                var idContent = new GUIContent(entry.id);

                // Create a GUIContent that contains the string as its text
                // and also as its tooltip. This allows the user to mouse
                // over this line in the inspector and see more of it.
                var lineContent = new GUIContent(entry.text, entry.text);

                // Show the line ID and localized text
                EditorGUILayout.LabelField(idContent, lineContent);

                if (entry.asset != null)
                {

                    // Asset references are never editable here - they're
                    // only updated by the Localization Database. Add a
                    // DisabledGroup here to make all ObjectFields be
                    // interactable, but read-only.
                    EditorGUI.BeginDisabledGroup(true);

                    // Show the object field
                    EditorGUILayout.ObjectField(" ", entry.asset, typeof(UnityEngine.Object), false);

                    // for AudioClips, add a little play preview button
                    if (entry.asset.GetType() == typeof(UnityEngine.AudioClip))
                    {
                        var rect = GUILayoutUtility.GetLastRect();

                        // Localization assets are displayed in an
                        // Inspector that's always disabled, so we need to
                        // manually set the enabled flag to 'true' in order
                        // to let this button be clickable. We'll restore
                        // it after we handle this button.
                        var wasEnabled = GUI.enabled;
                        GUI.enabled = true;

                        bool isPlaying = IsClipPlaying((AudioClip)entry.asset);
                        if (lastPreviewed == (AudioClip)entry.asset && isPlaying)
                        {
                            rect.width = 54;
                            rect.x += EditorGUIUtility.labelWidth - 56;

                            if (GUI.Button(rect, "▣ Stop"))
                            {
                                StopAllClips();
                                lastPreviewed = null;
                            }
                        }
                        else
                        {
                            rect.width = 18;
                            rect.x += EditorGUIUtility.labelWidth - 20;
                            if (GUI.Button(rect, "▸"))
                            {
                                PlayClip((AudioClip)entry.asset);
                                lastPreviewed = (AudioClip)entry.asset;
                            }
                        }

                        // Restore the enabled state
                        GUI.enabled = wasEnabled;
                    }

                    EditorGUILayout.Space();
                }
                else if (anyAssetsFound)
                {
                    // Other entries have assets, but not this one. TODO:
                    // show a warning? probably need to make it really
                    // prominent, and possibly allow filtering this view to
                    // show only lines that have no assets?
                }

            }
        }

        // below is some terrible reflection needed for the AudioClip
        // preview terrible hack from
        // https://forum.unity.com/threads/way-to-play-audio-in-editor-using-an-editor-script.132042/#post-4767824
        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            System.Reflection.Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

            // The name of the method we want to invoke changed in 2020.2,
            // so we'll do a little version testing here
            string methodName;

            methodName = "PlayPreviewClip";

            System.Reflection.MethodInfo method = audioUtilClass.GetMethod(
                methodName,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );
            method.Invoke(
                null,
                new object[] { clip, startSample, loop }
            );
        }

        public static void StopAllClips()
        {
            System.Reflection.Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

            // The name of the method we want to invoke changed in 2020.2,
            // so we'll do a little version testing here
            string methodName = "StopAllPreviewClips";

            System.Reflection.MethodInfo method = audioUtilClass.GetMethod(
                methodName,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new System.Type[] { },
                null
            );
            method.Invoke(
                null,
                new object[] { }
            );
        }

        public static bool IsClipPlaying(AudioClip clip)
        {
            System.Reflection.Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

            System.Reflection.MethodInfo method = audioUtilClass.GetMethod(
                "IsPreviewClipPlaying",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
            );
            return (bool)method.Invoke(
                null,
                null
            );
        }

        internal void ImportFromYarnProject(YarnProject project)
        {
            var target = this.target as Localization;
            if (target == null)
            {
                return;
            }

            var lineIDs = project.baseLocalization.GetLineIDs();

            var allLocalisedStrings = lineIDs.Select(id => new KeyValuePair<string, string?>(id, project.baseLocalization.GetLocalizedString(id))).Where(kv => kv.Value != null);
            var allLocalisedObjects = lineIDs.Select(id => new KeyValuePair<string, Object?>(id, project.baseLocalization.GetLocalizedObject<Object>(id))).Where(kv => kv.Value != null);

            target.AddLocalizedStringsToAsset(allLocalisedStrings!);
            target.AddLocalizedObjectsToAsset<Object>(allLocalisedObjects!);

            serializedObject.Update();

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }

        internal void ImportFromCSV(TextAsset asset)
        {
            var target = this.target as Localization;
            if (target == null)
            {
                return;
            }

            try
            {
                var stringTable = StringTableEntry.ParseFromCSV(asset.text);

                target.AddLocalizedStringsToAsset(stringTable.Select(s => new KeyValuePair<string, string>(s.ID, s.Text ?? string.Empty)));

                serializedObject.Update();

                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);

            }
            catch (System.ArgumentException e)
            {
                Debug.LogWarning($"Failed to import localization from CSV because an error was encountered during text parsing: {e}");
            }

        }
    }
}
