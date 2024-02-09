/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

#if USE_UNITY_LOCALIZATION
using UnityEngine.Localization.Tables;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement;
using UnityEngine.Localization.Metadata;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

#if USE_UNITASK
    using Cysharp.Threading.Tasks;
    using YarnTask = Cysharp.Threading.Tasks.UniTask;
    using YarnIntTask = Cysharp.Threading.Tasks.UniTask<int>;
    using YarnLineTask = Cysharp.Threading.Tasks.UniTask<LocalizedLine>;
    using YarnObjectTask = Cysharp.Threading.Tasks.UniTask<UnityEngine.Object>;
#else
    using YarnTask = System.Threading.Tasks.Task;
    using YarnLineTask = System.Threading.Tasks.Task<Yarn.Unity.LocalizedLine>;
    using YarnObjectTask = System.Threading.Tasks.Task<UnityEngine.Object>;
    using System.Threading;
    using System.Threading.Tasks;
using Yarn.Unity.UnityLocalization;
#endif

namespace Yarn.Unity.UnityLocalization
{

#if USE_UNITY_LOCALIZATION
    using UnityEngine.Localization.Tables;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Settings;
    using UnityEngine.Localization.Metadata;
    
    public class LineMetadata : IMetadata
    {
        public string nodeName = "";
        public string[] tags = System.Array.Empty<string>();
    }

    public class UnityLocalisedLineProvider : LineProviderBehaviour
    {
        // the string table asset that has all of our (hopefully) localised strings inside
        [SerializeField] internal LocalizedStringTable stringsTable;
        [SerializeField] internal LocalizedAssetTable assetTable;

        public override string LocaleCode
        {
            get
            {
                return LocalizationSettings.SelectedLocale.Identifier.Code;
            }
            set {
                Locale? locale = LocalizationSettings.AvailableLocales.GetLocale(value);
                if (locale == null) {
                    throw new System.InvalidOperationException($"Can't set locale to {value}: no such locale has been configured");
                }
                LocalizationSettings.SelectedLocale = locale;
            }
        }

        private Dictionary<string, Object> currentlyLoadedAssets = new Dictionary<string, Object>();

        private StringTable? currentStringTable;
        private AssetTable? currentAssetTable;

        public override void Start()
        {
            if (stringsTable.IsEmpty != false)
            {
                stringsTable.TableChanged += (table) => this.currentStringTable = table;
            }
            if (stringsTable.IsEmpty != false)
            {
                assetTable.TableChanged += (table) => this.currentAssetTable = table;
            }
        }

        public override async YarnLineTask GetLocalizedLineAsync(Line line, IMarkupParser markupParser, CancellationToken cancellationToken)
        {
            if (stringsTable.IsEmpty)
            {
                throw new System.InvalidOperationException($"Tried to get localised line for {line.ID}, but no string table has been set.");
            }

            // Ensure that our string tables are loaded before attempting to
            // fetch anything out of them.
            await EnsureStringsTableLoaded(cancellationToken);
            await EnsureAssetTableLoaded(cancellationToken);

            if (currentStringTable == null)
            {
                throw new System.InvalidOperationException($"Tried to get localised line for {line.ID}, but the string table failed to load.");
            }


            // Fetch the localised text and metadata from the string table.
            var text = line.ID;
            text = currentStringTable[line.ID]?.LocalizedValue ?? $"!! Error: Missing localisation for line {line.ID} in string table {currentStringTable.LocaleIdentifier}";

            var markup = markupParser.ParseMarkup(Dialogue.ExpandSubstitutions(text, line.Substitutions), currentStringTable.LocaleIdentifier.Code);

            // Construct the localized line
            LocalizedLine localizedLine = new LocalizedLine()
            {
                Text = markup,
                TextID = line.ID,
                Substitutions = line.Substitutions,
                RawText = text,
            };

            // Attempt to fetch metadata tags for this line from the string
            // table
            var metadata = currentStringTable[line.ID]?.SharedEntry.Metadata.GetMetadata<LineMetadata>();

            if (metadata != null)
            {
                localizedLine.Metadata = metadata.tags;
            }

            if (this.currentAssetTable != null)
            {
                // Fetch the asset for this line, if one is available.
                localizedLine.Asset = await YarnAsync.WaitForAsyncOperation(this.currentAssetTable.GetAssetAsync<Object>(line.ID), cancellationToken);
            }

            return localizedLine;
        }

        async YarnTask EnsureStringsTableLoaded(CancellationToken cancellationToken)
        {
            if (currentStringTable == null && stringsTable.IsEmpty == false)
            {
                // We haven't loaded our table yet. Wait for the table to load.
                this.currentStringTable = await YarnAsync.WaitForAsyncOperation(stringsTable.GetTableAsync(), cancellationToken);
            }
        }

        async YarnTask EnsureAssetTableLoaded(CancellationToken cancellationToken)
        {
            if (currentAssetTable == null && assetTable.IsEmpty == false)
            {
                // We haven't loaded our table yet. Wait for the table to load.
                this.currentAssetTable = await YarnAsync.WaitForAsyncOperation(assetTable.GetTableAsync(), cancellationToken);
            }
            if (currentAssetTable != null)
            {
                // We have an asset table. Ensure that it's finished preloading.
                await YarnAsync.WaitForAsyncOperation(this.currentAssetTable.PreloadOperation, cancellationToken);
            }
        }

        public override async YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            // Asynchronously ensure that all tables are loaded, and that the
            // asset table has finished preloading.

            // Wait for both of the tables to be ready.
            await YarnTask.WhenAll(new[] {
                EnsureStringsTableLoaded(cancellationToken),
                EnsureAssetTableLoaded(cancellationToken)
            });
        }
    }

#else
    public class UnityLocalisedLineProvider : LineProviderBehaviour
    {
        public override string LocaleCode => "error";


        private readonly string NotInstalledError = $"{nameof(UnityLocalisedLineProvider)} requires that the Unity Localization package is installed in the project. To fix this, install Unity Localization.";

        public override YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            Debug.LogError(NotInstalledError);
            return Task.CompletedTask;
        }

        public override void Start()
        {
            Debug.LogError(NotInstalledError);
        }

        public override YarnLineTask GetLocalizedLineAsync(Yarn.Line line, CancellationToken cancellationToken)
        {
            Debug.LogError($"{nameof(UnityLocalisedLineProvider)}: Can't create a localised line for ID {line.ID} because the Unity Localization package is not installed in this project. To fix this, install Unity Localization.");

            return YarnTask.FromResult(new LocalizedLine()
            {
                TextID = line.ID,
                RawText = $"{line.ID}: Unable to create a localised line, because the Unity Localization package is not installed in this project.",
                Substitutions = line.Substitutions,
            };
        }
#endif
    }

#if USE_UNITY_LOCALIZATION
    public class LineMetadata : IMetadata {
        public string nodeName = "";
        public string[] tags = System.Array.Empty<string>();
    }
#endif  

#if UNITY_EDITOR
    [CustomEditor(typeof(UnityLocalisedLineProvider))]
    public class UnityLocalisedLineProviderEditor : Editor {
        private SerializedProperty stringsTableProperty;
        private SerializedProperty assetTableProperty;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(stringsTableProperty);

            var stringTableName = stringsTableProperty.FindPropertyRelative("m_TableReference").FindPropertyRelative("m_TableCollectionName").stringValue;

            if (string.IsNullOrEmpty(stringTableName)) {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.HelpBox("Choose a strings table to make this line provider able to deliver line text.", MessageType.Warning);
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.PropertyField(assetTableProperty);

            serializedObject.ApplyModifiedProperties();
        }
        public void OnEnable() {
            #if USE_UNITY_LOCALIZATION
            this.stringsTableProperty = serializedObject.FindProperty(nameof(UnityLocalisedLineProvider.stringsTable));
            this.assetTableProperty = serializedObject.FindProperty(nameof(UnityLocalisedLineProvider.assetTable));
            #endif
        }
    }
#endif

