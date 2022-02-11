using UnityEngine;
using UnityEditor;
using Yarn.Unity;
using System.Linq;
using System.Collections.Generic;

#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
#endif

namespace Yarn.Unity.Editor
{
    [CustomEditor(typeof(Localization))]
    [CanEditMultipleObjects]
    public class LocalizationEditor : UnityEditor.Editor
    {

        private SerializedProperty languageNameProperty;
        private AudioClip lastPreviewed;

        private void OnEnable()
        {
            languageNameProperty = serializedObject.FindProperty("_LocaleCode");
            lastPreviewed = null;
        }

        public override void OnInspectorGUI()
        {


            var cultures = Cultures.GetCultures().ToList();

            var cultureDisplayName = cultures.Where(c => c.Name == languageNameProperty.stringValue)
                                             .Select(c => c.DisplayName)
                                             .DefaultIfEmpty("Development")
                                             .FirstOrDefault();

            EditorGUILayout.LabelField("Language", cultureDisplayName);

            EditorGUILayout.Space();

            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox($"Select a single {nameof(Localization).ToLowerInvariant()} to view its contents.", MessageType.None);
            }
            else
            {
                var target = this.target as Localization;

                DrawLocalizationContents(target);
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
        private void DrawLocalizationContents(Localization target)
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

            if (target.ContainsLocalizedAssets && target.UsesAddressableAssets) {
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

                if (allAddressEntries.TryGetValue(address, out var addressableAssetEntry)) {
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

#if UNITY_2020_2_OR_NEWER
            methodName = "PlayPreviewClip";
#else
        methodName = "PlayClip";
#endif

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
            string methodName;

#if UNITY_2020_2_OR_NEWER
            methodName = "StopAllPreviewClips";
#else
        methodName = "StopAllClips";
#endif

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

            // The name of the method we want to invoke AND its parameters
            // changed in 2020.2, so we'll do a little version testing here
#if UNITY_2020_2_OR_NEWER
            System.Reflection.MethodInfo method = audioUtilClass.GetMethod(
                "IsPreviewClipPlaying",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
            );
            return (bool)method.Invoke(
                null,
                null
            );
#else
        System.Reflection.MethodInfo method = audioUtilClass.GetMethod(
            "IsClipPlaying",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
            null,
            new System.Type[] { typeof(AudioClip) },
            null
        );
        return (bool)method.Invoke(
            null,
            new object[] { clip }
        );
#endif
        }
    }
}
