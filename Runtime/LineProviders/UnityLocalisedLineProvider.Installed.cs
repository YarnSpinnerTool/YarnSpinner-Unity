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
        /// </summary>
        public string[] tags = System.Array.Empty<string>();

        /// <summary>
        /// Gets the line ID indicated by any shadow tag contained in this
        /// metadata, if present.
        /// </summary>
        public string? ShadowLineSource
        {
            get
            {
                foreach (var metadataEntry in tags)
                {
                    if (metadataEntry.StartsWith("shadow:") != false)
                    {
                        // This is a shadow line. Return the line ID that it's
                        // shadowing.
                        return "line:" + metadataEntry.Substring("shadow:".Length);
                    }
                }

                // The line had metadata, but it wasn't a shadow line.
                return null;
            }
        }
    }

    /// <summary>
    /// A line provider that uses the Unity Localization system to get localized
    /// content for Yarn lines.
    /// </summary>
    public partial class UnityLocalisedLineProvider : LineProviderBehaviour
    {
        // the string table asset that has all of our (hopefully) localised
        // strings inside
        [SerializeField] internal LocalizedStringTable? stringsTable;
        [SerializeField] internal LocalizedAssetTable? assetTable;

        /// <inheritdoc/>
        public override string LocaleCode
        {
            get => LocalizationSettings.SelectedLocale.Identifier.Code;
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

        void Awake()
        {
            lineParser.RegisterMarkerProcessor("select", builtInReplacer);
            lineParser.RegisterMarkerProcessor("plural", builtInReplacer);
            lineParser.RegisterMarkerProcessor("ordinal", builtInReplacer);
        }

        /// <inheritdoc/>
        public override async YarnTask<LocalizedLine> GetLocalizedLineAsync(Line line, CancellationToken cancellationToken)
        {
            if (stringsTable == null || stringsTable.IsEmpty)
            {
                throw new System.InvalidOperationException($"Tried to get localised line for {line.ID}, but no string table has been set.");
            }

            var getStringOp = LocalizationSettings.StringDatabase.GetTableEntryAsync(stringsTable.TableReference, line.ID, null, FallbackBehavior.UseFallback);
            var entry = await YarnTask.WaitForAsyncOperation(getStringOp, cancellationToken);

            // Attempt to fetch metadata tags for this line from the string
            // table
            var metadata = entry.Entry?.SharedEntry.Metadata.GetMetadata<LineMetadata>();

            // Get the text from the entry
            var text = entry.Entry?.LocalizedValue
                ?? $"!! Error: Missing localisation for line {line.ID} in string table {entry.Table.LocaleIdentifier}";

            string? shadowLineID = metadata?.ShadowLineSource;

            if (shadowLineID != null)
            {
                // This line actually shadows another line. Fetch that line, and
                // use its text (but not its metadata)
                var getShadowLineOp = LocalizationSettings.StringDatabase.GetTableEntryAsync(stringsTable.TableReference, shadowLineID, null, FallbackBehavior.UseFallback);
                var shadowEntry = await YarnTask.WaitForAsyncOperation(getShadowLineOp, cancellationToken);
                if (shadowEntry.Entry == null)
                {
                    Debug.LogWarning($"Line {line.ID} shadows line {shadowLineID}, but no such entry was found in the string table {stringsTable.TableReference}");
                }
                else
                {
                    text = shadowEntry.Entry.LocalizedValue;
                }
            }

            // We now have our text; parse it as markup
            var markup = lineParser.ParseString(LineParser.ExpandSubstitutions(text, line.Substitutions), this.LocaleCode);

            // Lastly, attempt to fetch an asset for this line
            Object? asset = null;

            if (this.assetTable != null && this.assetTable.IsEmpty == false)
            {
                // Fetch the asset for this line, if one is available.
                var loadOp = LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<Object>(assetTable.TableReference, shadowLineID ?? line.ID, null, FallbackBehavior.UseFallback);
                asset = await YarnTask.WaitForAsyncOperation(loadOp, cancellationToken);
            }

            // Construct the localized line
            LocalizedLine localizedLine = new LocalizedLine()
            {
                Text = markup,
                TextID = line.ID,
                Substitutions = line.Substitutions,
                RawText = text,
                Metadata = metadata?.tags ?? System.Array.Empty<string>(),
                Asset = asset,
            };

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
        public override YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            // Nothing to do. If a user wants to ensure ahead of time that
            // localized content is already in memory, they should use the Unity
            // Localization preload support.
            return YarnTask.CompletedTask;
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
