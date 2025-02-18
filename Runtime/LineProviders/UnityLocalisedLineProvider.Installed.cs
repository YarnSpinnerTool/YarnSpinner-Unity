/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#if USE_UNITY_LOCALIZATION

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.Exceptions;
using Yarn.Markup;

#nullable enable

namespace Yarn.Unity.UnityLocalization
{

    /// <summary>
    /// Contains Yarn Spinner related metadata for Unity string table entries.
    /// </summary>
    public class LineMetadata : IMetadata
    {
        /// <summary>
        /// The name of the Yarn node that this line came from.
        /// </summary>
        public string nodeName = "";

        /// <summary>
        /// The <c>#hashtags</c> present on the line.
        public string[] tags = System.Array.Empty<string>();
        /// </summary>
    }

    /// <summary>
    /// A line provider that uses the Unity Localization system to get localized
    /// content for Yarn lines.
    /// </summary>
    public partial class UnityLocalisedLineProvider : LineProviderBehaviour
    {
        // the string table asset that has all of our (hopefully) localised
        // strings inside
        [SerializeField] internal LocalizedStringTable stringsTable;
        [SerializeField] internal LocalizedAssetTable assetTable;

        /// <inheritdoc/>
        public override string LocaleCode
        {
            get
            {
                return LocalizationSettings.SelectedLocale.Identifier.Code;
            }
            set
            {
                Locale? locale = LocalizationSettings.AvailableLocales.GetLocale(value);
                if (locale == null)
                {
                    throw new System.InvalidOperationException($"Can't set locale to {value}: no such locale has been configured");
                }
                LocalizationSettings.SelectedLocale = locale;
            }
        }

        private LineParser lineParser = new LineParser();
        private BuiltInMarkupReplacer builtInReplacer = new BuiltInMarkupReplacer();

        private Dictionary<string, Object> currentlyLoadedAssets = new Dictionary<string, Object>();

        private StringTable? currentStringTable;
        private AssetTable? currentAssetTable;

        void Awake()
        {
            lineParser.RegisterMarkerProcessor("select", builtInReplacer);
            lineParser.RegisterMarkerProcessor("plural", builtInReplacer);
            lineParser.RegisterMarkerProcessor("ordinal", builtInReplacer);
        }

        /// <inheritdoc/>
        public override void Start()
        {
            // When the strings table changes (for example, due to the user
            // changing locale), update our local references.
            stringsTable.TableChanged += (table) => this.currentStringTable = table;
            assetTable.TableChanged += (table) => this.currentAssetTable = table;
        }

        /// <inheritdoc/>
        public override async YarnTask<LocalizedLine> GetLocalizedLineAsync(Line line, CancellationToken cancellationToken)
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

            var markup = lineParser.ParseString(LineParser.ExpandSubstitutions(text, line.Substitutions), this.LocaleCode);

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
                try
                {
                    // Fetch the asset for this line, if one is available.
                    localizedLine.Asset = await YarnTask.WaitForAsyncOperation(this.currentAssetTable.GetAssetAsync<Object>(line.ID), cancellationToken);
                }
                catch (System.Exception e)
                {
                    // Failed to fetch an asset.
                    Debug.LogWarning($"Failed to fetch an asset for {line.ID}: " + e);
                }
            }

            return localizedLine;
        }

        /// <inheritdoc/>
        public override void RegisterMarkerProcessor(string attributeName, IAttributeMarkerProcessor markerProcessor)
        {
            lineParser.RegisterMarkerProcessor(attributeName, markerProcessor);
        }

        /// <inheritdoc/>
        public override void DeregisterMarkerProcessor(string attributeName)
        {
            lineParser.DeregisterMarkerProcessor(attributeName);
        }

        /// <inheritdoc/>
        async YarnTask EnsureStringsTableLoaded(CancellationToken cancellationToken)
        {
            if (currentStringTable == null && stringsTable.IsEmpty == false)
            {
                // We haven't loaded our table yet. Wait for the table to load.
                this.currentStringTable = await YarnTask.WaitForAsyncOperation(stringsTable.GetTableAsync(), cancellationToken);
            }
        }

        async YarnTask EnsureAssetTableLoaded(CancellationToken cancellationToken)
        {
            if (currentAssetTable == null && assetTable.IsEmpty == false)
            {
                // We haven't loaded our table yet. Wait for the table to load.
                this.currentAssetTable = await YarnTask.WaitForAsyncOperation(assetTable.GetTableAsync(), cancellationToken);
            }
            if (currentAssetTable != null)
            {
                // We now have an asset table. Ensure that it's finished preloading.
                await YarnTask.WaitForAsyncOperation(this.currentAssetTable.PreloadOperation, cancellationToken);
            }
        }

        /// <inheritdoc/>
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
}

#if UNITY_EDITOR
namespace Yarn.Unity.UnityLocalization.Editor
{
    using System;
    using UnityEditor;

    [CustomEditor(typeof(UnityLocalisedLineProvider))]
    internal class UnityLocalisedLineProviderEditor : UnityEditor.Editor
    {
        private SerializedProperty? stringsTableProperty;
        private SerializedProperty? assetTableProperty;

        /// <summary>
        /// Called by Unity to draw the inspector GUI.
        /// </summary>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(stringsTableProperty);

            var stringTableName = stringsTableProperty?.FindPropertyRelative("m_TableReference").FindPropertyRelative("m_TableCollectionName").stringValue;

            if (string.IsNullOrEmpty(stringTableName))
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.HelpBox("Choose a strings table to make this line provider able to deliver line text.", MessageType.Warning);
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.PropertyField(assetTableProperty);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Called by Unity when the editor is enabled to set up cached properties.
        /// </summary>
        protected void OnEnable()
        {
            this.stringsTableProperty = serializedObject.FindProperty(nameof(UnityLocalisedLineProvider.stringsTable));
            this.assetTableProperty = serializedObject.FindProperty(nameof(UnityLocalisedLineProvider.assetTable));
        }
    }
}
#endif // UNITY_EDITOR
#endif // USE_UNITY_LOCALIZATION
