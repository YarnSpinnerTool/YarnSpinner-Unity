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

namespace Yarn.Unity.UnityLocalization
{
    public class UnityLocalisedLineProvider : LineProviderBehaviour
    {
#if USE_UNITY_LOCALIZATION
        // the string table asset that has all of our (hopefully) localised strings inside
        [SerializeField] internal LocalizedStringTable stringsTable;
        [SerializeField] internal LocalizedAssetTable assetTable;

        /// Should assets for lines be automatically unloaded or not.
        /// Most of the time you want to leave this as true.
        [SerializeField] internal bool AutomaticallyUnloadUnusedLineAssets = true;

        // the runtime table we actually get our strings out of
        // this changes at runtime depending on the language
        private StringTable currentStringsTable;
        private AssetTable currentAssetTable;

        private List<AsyncOperationHandle<Object>> pendingLoadOperations = new List<AsyncOperationHandle<Object>>();
        private Dictionary<string, Object> loadedAssets = new Dictionary<string, Object>();

        public override string LocaleCode => LocalizationSettings.SelectedLocale.Identifier.Code;

        public override bool LinesAvailable
        {
            get
            {
                // If a strings table wasn't set, we'll never have lines
                if (this.stringsTable.IsEmpty)
                {
                    return false;
                }

                // If we haven't finished loading the strings table, we don't
                // have lines yet
                if (this.currentStringsTable == null)
                {
                    return false;
                }

                // If we have an asset table, then we need to check some things
                // about it
                if (this.assetTable.IsEmpty == false)
                {
                    // If the table hasn't finished loading yet, then lines
                    // aren't available yet
                    if (this.currentAssetTable == null)
                    {
                        return false;
                    }

                    // If we're pending the load of certain assets, then lines
                    // aren't available yet
                    if (pendingLoadOperations.Count > 0)
                    {
                        return false;
                    }
                }

                // We're good to go!
                return true;
            }
        }

        public override LocalizedLine GetLocalizedLine(Yarn.Line line)
        {
            var text = line.ID;
            if (currentStringsTable != null)
            {
                text = currentStringsTable[line.ID]?.LocalizedValue ?? $"Error: Missing localisation for line {line.ID} in string table {currentStringsTable.LocaleIdentifier}";
            }

            // Construct the localized line
            LocalizedLine localizedLine = new LocalizedLine()
            {
                TextID = line.ID,
                RawText = text,
                Substitutions = line.Substitutions,
            };

            // Attempt to fetch metadata tags for this line from the string
            // table
            var metadata = currentStringsTable[line.ID]?.SharedEntry.Metadata.GetMetadata<UnityLocalization.LineMetadata>();

            if (metadata != null)
            {
                localizedLine.Metadata = metadata.tags;
            }

            // Attempt to fetch a loaded asset for this line
            // If we have a loaded asset associated with this line, return it
            if (loadedAssets.TryGetValue(line.ID, out var asset))
            {
                localizedLine.Asset = asset;
            }

            return localizedLine;
        }

        public override void Start()
        {
            // doing an initial load of the strings
            if (stringsTable != null)
            {
                // Adding an event handler to TableChanged will trigger an
                // initial async load of the string table, which will call the
                // handler on completion. So, we don't set currentStringsTable
                // here, but instead we only ever do it in OnStringTableChanged.
                stringsTable.TableChanged += OnStringTableChanged;
            }

            if (assetTable != null)
            {
                // Same logic for asset table as for strings table, above.
                assetTable.TableChanged += OnAssetTableChanged;
            }
        }

        // We've been notified that a new strings table has been loaded.
        private void OnStringTableChanged(StringTable newTable)
        {
            currentStringsTable = newTable;
        }

        // We've been notified that a new asset table has been loaded.
        private void OnAssetTableChanged(AssetTable value)
        {
            currentAssetTable = value;
        }

        /// <summary>
        /// Clears all loaded assets from the cache.
        /// </summary>
        /// <remarks>
        /// If you do this you either know what you are doing or are ok with having weird things happen.
        /// </remarks>
        public void ClearLoadedAssets()
        {
            loadedAssets.Clear();
            RunAfterComplete(assetTable.GetTableAsync(), (loadedAssetTable) =>
            {
                loadedAssetTable.ReleaseAssets();
            });
        }

        public override void PrepareForLines(IEnumerable<string> lineIDs)
        {
            if (assetTable.IsEmpty != true)
            {
                // We have an asset table, so 1. ensure that the locale-specific asset table is loaded, and then 2. ensure that each asset that we care about has been loaded.

                if (currentAssetTable == null)
                {
                    // The asset table hasn't yet loaded, so get it
                    // asynchronously and then start preloading from it
                    RunAfterComplete(assetTable.GetTableAsync(), (loadedAssetTable) =>
                    {
                        PreloadLinesFromTable(loadedAssetTable, lineIDs);
                    });
                }
                else
                {
                    // The asset table has already loaded, so start preloading
                    // now
                    PreloadLinesFromTable(currentAssetTable, lineIDs);
                }
            }

            void PreloadLinesFromTable(AssetTable table, IEnumerable<string> lineIDs)
            {
                // first we need to unload any assets that are loaded but unneeded
                // if you are managing the release of assets yourself this won't happen
                if (AutomaticallyUnloadUnusedLineAssets)
                {
                    // Remove and release the lines that have been previously loaded
                    // but aren't in this set of lines to expect - they're not
                    // needed now
                    var assetKeysToUnload = new HashSet<string>(loadedAssets.Keys);
                    assetKeysToUnload.ExceptWith(lineIDs);
                    foreach (var assetKeyToUnload in assetKeysToUnload)
                    {
                        var entryToRelease = table.GetEntry(assetKeyToUnload);

                        if (entryToRelease != null)
                        {
                            table.ReleaseAsset(entryToRelease);

                            loadedAssets.Remove(assetKeyToUnload);
                        }
                    }
                }

                // next we only need to load the assets that aren't already loaded
                var nonLoadedAssetIDs = new HashSet<string>(lineIDs);
                nonLoadedAssetIDs.ExceptWith(loadedAssets.Keys);

                // Load all assets that we need
                foreach (var id in nonLoadedAssetIDs)
                {
                    var entry = table.GetEntry(id);
                    if (entry == null)
                    {
                        // This ID doesn't exist in the asset table - nothing to
                        // load!
                        continue;
                    }

                    var loadOperation = table.GetAssetAsync<Object>(entry.KeyId);

                    if (loadOperation.IsDone == true)
                    {
                        // If the load operation has already completed, there's
                        // no need to wait - we can use its result now.
                        loadedAssets[id] = loadOperation.Result;
                    }
                    else
                    {
                        // Wait until this load operation completes, and then
                        // get its result.
                        pendingLoadOperations.Add(loadOperation);
                        loadOperation.Completed += (operation) =>
                        {
                            pendingLoadOperations.Remove(loadOperation);
                            if (operation.Status == AsyncOperationStatus.Succeeded)
                            {
                                loadedAssets[id] = operation.Result;
                            }
                            else
                            {
                                Debug.LogError($"Asset load operation for ID {id} failed!");
                            }
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Waits until an asynchronous operation has completed, and then calls
        /// a completion handler when it's done.
        /// </summary>
        /// <typeparam name="T">The type of object that <paramref
        /// name="operation"/> will return when it completes.</typeparam>
        /// <param name="operation">The asynchonous operation to wait
        /// for.</param>
        /// <param name="onComplete">A method to call when the operation
        /// completes successfully.</param>
        /// <param name="onFailure">A method to call when the operation
        /// fails.</param>
        private void RunAfterComplete<T>(AsyncOperationHandle<T> operation, System.Action<T> onComplete, System.Action onFailure = null)
        {
            if (onComplete is null)
            {
                throw new System.ArgumentNullException(nameof(onComplete));
            }

            StartCoroutine(RunAfterCompleteImpl(operation, onComplete));

            IEnumerator RunAfterCompleteImpl(AsyncOperationHandle<T> operation, System.Action<T> onComplete)
            {
                yield return operation;

                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    onComplete(operation.Result);
                }
                else
                {
                    onFailure?.Invoke();
                }
            }
        }        
#else
        public override string LocaleCode => "error";

        public override void PrepareForLines(IEnumerable<string> lineIDs) {
            Debug.LogError($"{nameof(UnityLocalisedLineProvider)} requires that the Unity Localization package is installed in the project. To fix this, install Unity Localization.");
        }

        public override bool LinesAvailable => true; // likewise later we should check that it has actually loaded the string table

        public override void Start()
        {
            Debug.LogError($"{nameof(UnityLocalisedLineProvider)} requires that the Unity Localization package is installed in the project. To fix this, install Unity Localization.");
        }
        public override LocalizedLine GetLocalizedLine(Yarn.Line line)
        {
            Debug.LogError($"{nameof(UnityLocalisedLineProvider)}: Can't create a localised line for ID {line.ID} because the Unity Localization package is not installed in this project. To fix this, install Unity Localization.");
            
            return new LocalizedLine()
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
        public string nodeName;
        public string[] tags;
    }
#endif  

#if UNITY_EDITOR
    [CustomEditor(typeof(UnityLocalisedLineProvider))]
    public class UnityLocalisedLineProviderEditor : Editor
    {
        private SerializedProperty stringsTableProperty;
        private SerializedProperty assetTableProperty;
        private SerializedProperty automaticAssetUnloadingProperty;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(stringsTableProperty);

            var stringTableName = stringsTableProperty.FindPropertyRelative("m_TableReference").FindPropertyRelative("m_TableCollectionName").stringValue;

            if (string.IsNullOrEmpty(stringTableName))
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.HelpBox("Choose a strings table to make this line provider able to deliver line text.", MessageType.Warning);
                EditorGUI.indentLevel -= 1;
            }
            EditorGUILayout.PropertyField(assetTableProperty);
            EditorGUILayout.PropertyField(automaticAssetUnloadingProperty, new GUIContent("Manage Assets"));

            serializedObject.ApplyModifiedProperties();
        }
        public void OnEnable()
        {
            #if USE_UNITY_LOCALIZATION
            this.stringsTableProperty = serializedObject.FindProperty(nameof(UnityLocalisedLineProvider.stringsTable));
            this.assetTableProperty = serializedObject.FindProperty(nameof(UnityLocalisedLineProvider.assetTable));
            this.automaticAssetUnloadingProperty = serializedObject.FindProperty(nameof(UnityLocalisedLineProvider.AutomaticallyUnloadUnusedLineAssets));
            #endif
        }
    }
#endif

}

