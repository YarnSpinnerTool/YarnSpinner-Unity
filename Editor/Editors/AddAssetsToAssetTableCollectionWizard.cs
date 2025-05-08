/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.AssetImporters;
    using UnityEditorInternal;
    using UnityEngine;
    using Yarn.Compiler;

#if USE_UNITY_LOCALIZATION
    using UnityEditor.Localization;
    using UnityEngine.Localization.Tables;
    using UnityEngine.Localization;

    public class AddAssetsToAssetTableCollectionWizard : EditorWindow
    {

        private YarnProject? yarnProject;

        private AssetTableCollection? assetTableCollection;

        private Type? _assetType;
        private Type AssetType
        {
            get
            {
                if (_assetType == null)
                {
                    var typeName = EditorPrefs.GetString("YarnSpinner_AddAssets_AssetType", string.Empty);
                    if (typeName != string.Empty)
                    {
                        _assetType = System.Type.GetType(typeName, throwOnError: false, ignoreCase: false);
                    }
                }
                _assetType ??= typeof(AudioClip);
                return _assetType;
            }
            set
            {
                _assetType = value;
                EditorPrefs.SetString("YarnSpinner_AddAssets_AssetType", _assetType?.AssemblyQualifiedName ?? string.Empty);
            }
        }

        private Dictionary<string, DefaultAsset?> localesToFolders = new Dictionary<string, DefaultAsset?>();
        private Type[] allTypes = Array.Empty<Type>();
        private GUIContent[] allTypeContents = Array.Empty<GUIContent>();
        System.Collections.ObjectModel.ReadOnlyCollection<UnityEngine.Localization.Locale>? locales = null;

        private Dictionary<LocaleIdentifier, LocalizationTable> _cachedTables = new();

        const string description = "This tool searches Asset Folders for assets of the selected type, and then adds them to an Asset Table Collection. Line Providers, like Unity Localised Line Provider, can then fetch these assets at run-time.";

        [MenuItem("Window/Yarn Spinner/Add Assets to Table Collection")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = CreateWindow<AddAssetsToAssetTableCollectionWizard>();
            window.ShowPopup();
            window.titleContent = new GUIContent("Add Assets to Table Collection");

            if (Selection.activeObject is AssetTableCollection collection)
            {
                window.assetTableCollection = collection;
            }
        }

        void OnEnable()
        {
            allTypes = TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().OrderBy(t => t.FullName).ToArray();
            allTypeContents = allTypes.Select(t => new GUIContent(
                        t.FullName.Replace(".", "/"),
                        EditorGUIUtility.ObjectContent(null, t).image
                    )).ToArray();
            locales = LocalizationEditorSettings.GetLocales();
        }

        void OnGUI()
        {

            using (new GUILayout.VerticalScope())
            {

                EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);

                EditorGUILayout.HelpBox(description, MessageType.Info);

                assetTableCollection = EditorGUILayout.ObjectField(new GUIContent("Asset Table Collection", "The asset table collection to add assets to"), assetTableCollection, typeof(AssetTableCollection), allowSceneObjects: false) as AssetTableCollection;


                yarnProject = EditorGUILayout.ObjectField(new GUIContent("Yarn Project", "The Yarn Project to add assets for."), yarnProject, typeof(YarnProject), allowSceneObjects: false) as YarnProject;

                var selectedIndex = Array.IndexOf(allTypes, AssetType);

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var newSelection = EditorGUILayout.Popup(new GUIContent("Asset Type", "The type of assets to find in the Asset Folders"), selectedIndex, allTypeContents);

                    if (change.changed)
                    {
                        AssetType = allTypes[newSelection];
                    }
                }

                var headerStyle = EditorStyles.boldLabel;

                if (assetTableCollection != null)
                {
                    EditorGUILayout.LabelField("Asset Folders", headerStyle);

                    EditorGUI.indentLevel += 1;

                    if (locales == null)
                    {
                        EditorGUILayout.HelpBox("No locales were found in your project. Is Unity Localization installed and correctly configured?", MessageType.Error);
                        return;
                    }

                    foreach (var locale in locales)
                    {
                        if (_cachedTables.TryGetValue(locale.Identifier, out var localizationTable) == false || localizationTable == null)
                        {
                            if (assetTableCollection != null)
                            {
                                localizationTable = assetTableCollection.GetTable(locale.Identifier);

                                _cachedTables[locale.Identifier] = localizationTable;
                            }
                        }

                        if (localizationTable == null)
                        {
                            EditorGUILayout.LabelField(new GUIContent(locale.LocaleName), new GUIContent("No table in collection"));
                            continue;
                        }

                        using (new EditorGUI.DisabledGroupScope(localizationTable == null))
                        {
                            localesToFolders.TryGetValue(locale.Identifier.Code, out var currentFolder);

                            localesToFolders[locale.Identifier.Code] = EditorGUILayout.ObjectField(
                                new GUIContent(locale.LocaleName, $"The folder to search for {AssetType.Name} assets for the '{locale.LocaleName}' locale"),
                                currentFolder,
                                typeof(DefaultAsset),
                                allowSceneObjects: false) as DefaultAsset;
                        }
                    }

                    EditorGUI.indentLevel -= 1;
                }

                GUILayout.FlexibleSpace();

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    var readyToAddAssets = assetTableCollection != null && yarnProject != null && localesToFolders.Where(kv => kv.Value != null).Count() > 0;

                    using (new EditorGUI.DisabledGroupScope(!readyToAddAssets))
                    {
                        if (GUILayout.Button("Add Assets"))
                        {
                            AddAssets(assetTableCollection!, yarnProject!, localesToFolders!, AssetType);
                            // this.Close();
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        private static void AddAssets(AssetTableCollection assetTableCollection, YarnProject yarnProject, IReadOnlyDictionary<string, DefaultAsset> localesToFolders, System.Type assetType)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(yarnProject)) as YarnProjectImporter;

            if (importer == null)
            {
                throw new InvalidOperationException("Failed to get an importer for Yarn Project");
            }

            var stringTable = importer.GenerateStringsTable();

            var lineIDs = stringTable.Select(e => e.ID);

            int totalCount = 0;
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                int perLocaleCount = 0;
                if (localesToFolders.TryGetValue(locale.Identifier.Code, out var folder) == false
                    || folder == null)
                {
                    // No folder given for this locale. Skip it!
                    Debug.Log($"Skipping {locale.LocaleName} because no folder was provided");
                    continue;
                }

                if (assetTableCollection.ContainsTable(locale.Identifier) == false)
                {
                    // No table in this collection for this locale! Skip it!
                    Debug.Log($"Skipping {locale.LocaleName} because no table exists");
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(folder);

                var idsToAssetPaths = YarnProjectUtility.FindAssetPathsForLineIDs(lineIDs, AssetDatabase.GetAssetPath(folder), assetType);

                foreach (var (lineID, assetPath) in idsToAssetPaths)
                {
                    var asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType);

                    if (asset == null)
                    {
                        // Not the type of asset we're looking for
                        continue;
                    }

                    assetTableCollection.AddAssetToTable(locale.Identifier, lineID, asset);

                    perLocaleCount += 1;
                    totalCount += 1;
                }
                EditorUtility.SetDirty(assetTableCollection);
                Debug.Log($"Added {perLocaleCount} assets to {locale.LocaleName}");
            }
            Debug.Log($"Added {totalCount} assets to asset table collection");

        }
    }
#endif
}
