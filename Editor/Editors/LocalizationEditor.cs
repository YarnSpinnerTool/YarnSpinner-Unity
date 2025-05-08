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
        private System.Type? assetType;

        public LocalizationEditor? Target { get; private set; }

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
        private SerializedProperty? entriesProperty;
        private SerializedProperty? usesUnityAddressablesProperty;
        private AudioClip? lastPreviewed;
        private List<Culture>? cultures;
        private int currentPickerWindow;

        private void OnEnable()
        {
            entriesProperty = serializedObject.FindProperty(nameof(Localization.entries));
            usesUnityAddressablesProperty = serializedObject.FindProperty(nameof(Localization._usesAddressableAssets));
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
#if USE_ADDRESSABLES
                    EditorGUILayout.PropertyField(usesUnityAddressablesProperty);
                    EditorGUILayout.Space();
#else
                    if (usesUnityAddressablesProperty != null && usesUnityAddressablesProperty.boolValue)
                    {
                        EditorGUILayout.HelpBox("This Localization uses Unity Addressables, but the package is not installed.", MessageType.Warning);
                        EditorGUILayout.Space();
                    }
#endif
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

#if USE_ADDRESSABLES
                                if (target.UsesAddressableAssets)
                                {
                                    // If we're using addressable assets, make
                                    // sure that the asset we just added has an
                                    // address
                                    EnsureAssetIsAddressable(asset, Localization.GetAddressForLine(lineID, target.name));
                                }
#endif
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

            var localizedLineContent = new List<(string ID, string Line, Object? Asset)>();

            var anyAssetsFound = false;

            foreach (var key in lineKeys)
            {

                if (target.entries.TryGetValue(key, out var entry) == false)
                {
                    // We somehow don't have a value for this line ID?
                    Debug.LogError($"Internal error: failed to find an entry for {key}");
                    EditorGUILayout.HelpBox($"Internal error: failed to find an entry for {key}", MessageType.Error);
                    return;
                }

                string? text = target.GetLocalizedString(key);
                Object? asset = null;

                if (target.UsesAddressableAssets)
                {
#if USE_ADDRESSABLES
                    asset = entry.localizedAssetReference?.editorAsset;
#endif
                }
                else
                {
                    asset = entry.localizedAsset;
                }

                anyAssetsFound |= asset != null;

                localizedLineContent.Add((key, text ?? string.Empty, asset));
            }

            foreach (var entry in localizedLineContent)
            {

                var idContent = new GUIContent(entry.ID);

                // Create a GUIContent that contains the string as its text
                // and also as its tooltip. This allows the user to mouse
                // over this line in the inspector and see more of it.
                var lineContent = new GUIContent(entry.Line, entry.Line);

                // Show the line ID and localized text
                EditorGUILayout.LabelField(idContent, lineContent);

                if (entry.Asset != null)
                {
                    // Asset references are never editable here - they're
                    // only updated by the Localization Database. Add a
                    // DisabledGroup here to make all ObjectFields be
                    // interactable, but read-only.
                    EditorGUI.BeginDisabledGroup(true);

                    // Show the object field
                    EditorGUILayout.ObjectField(" ", entry.Asset, typeof(UnityEngine.Object), false);

                    // for AudioClips, add a little play preview button
                    if (entry.Asset.GetType() == typeof(UnityEngine.AudioClip))
                    {
                        var rect = GUILayoutUtility.GetLastRect();

                        // Localization assets are displayed in an
                        // Inspector that's always disabled, so we need to
                        // manually set the enabled flag to 'true' in order
                        // to let this button be clickable. We'll restore
                        // it after we handle this button.
                        var wasEnabled = GUI.enabled;
                        GUI.enabled = true;

                        bool isPlaying = IsClipPlaying((AudioClip)entry.Asset);
                        if (lastPreviewed == (AudioClip)entry.Asset && isPlaying)
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
                                PlayClip((AudioClip)entry.Asset);
                                lastPreviewed = (AudioClip)entry.Asset;
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

            target.UsesAddressableAssets = project.baseLocalization.UsesAddressableAssets;

            foreach (var (id, entry) in project.baseLocalization.entries)
            {
                var localizedString = entry.localizedString;
                if (localizedString != null)
                {
                    target.AddLocalisedStringToAsset(id, localizedString);
                }

                Object? asset = null;

                if (project.baseLocalization.UsesAddressableAssets)
                {
#if USE_ADDRESSABLES
                    asset = entry.localizedAssetReference?.editorAsset;
#endif
                }
                else if (entry.localizedAsset != null)
                {
                    asset = entry.localizedAsset;
                }

                if (asset != null)
                {
                    target.AddLocalizedObjectToAsset(id, asset);
                }

            }

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

                foreach (var entry in stringTable)
                {
                    target.AddLocalisedStringToAsset(entry.ID, entry.Text ?? string.Empty);
                }

                serializedObject.Update();

                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);

            }
            catch (System.ArgumentException e)
            {
                Debug.LogWarning($"Failed to import localization from CSV because an error was encountered during text parsing: {e}");
            }
        }

#if USE_ADDRESSABLES
        internal static void EnsureAssetIsAddressable(Object asset, string defaultAddress)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _) == false)
            {
                Debug.LogError($"Can't make {asset} addressable: no GUID found", asset);
                return;
            }

            // Find the existing entry for this asset, if it has one.
            UnityEditor.AddressableAssets.Settings.AddressableAssetEntry entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);

            if (entry != null)
            {
                // The asset already has an entry. Nothing to do.
                return;
            }

            // This asset didn't have an entry. Create one in the default group.
            Debug.Log($"Marking asset {AssetDatabase.GetAssetPath(asset)} as addressable", asset);

            entry = AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, AddressableAssetSettingsDefaultObject.Settings.DefaultGroup);

            // Update the entry's address.
            entry.SetAddress(defaultAddress);
        }
#endif
    }
}
