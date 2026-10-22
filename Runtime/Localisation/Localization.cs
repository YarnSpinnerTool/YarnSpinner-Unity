/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// A localization is a bundle of strings and assets for a particular culture
    /// </summary>
    [CreateAssetMenu(fileName = "NewLocalization", menuName = "Yarn Spinner/Built-In Localization/Localization", order = 105)]
    public class Localization : ScriptableObject
    {
        /// <summary>
        /// Returns the address that should be used to fetch an asset suitable
        /// for a specific line in a specific language.
        /// </summary>
        /// <remarks>
        /// This method is useful for creating an address for use with the
        /// Addressable Assets system.
        /// </remarks>
        /// <param name="lineID">The line ID to use when generating the
        /// address.</param>
        /// <param name="language">The language to use when generating the
        /// address.</param>
        /// <returns>The address to use.</returns>
        internal static string GetAddressForLine(string lineID, string language)
        {
            return $"line_{language}_{lineID.Replace("line:", "")}";
        }

        [System.Serializable]
        public sealed class LocalizationTableEntry
        {
            public string? localizedString;
            public UnityEngine.Object? localizedAsset;

#if USE_ADDRESSABLES
            public UnityEngine.AddressableAssets.AssetReference? localizedAssetReference;
#endif
        }

        [SerializeField] internal SerializableDictionary<string, LocalizationTableEntry> entries = new();

        private Dictionary<string, string> _runtimeStringTable = new Dictionary<string, string>();

        /// <summary>
        /// Gets a value indicating whether this <see cref="Localization"/>
        /// makes use of Addressable Assets (<see langword="true"/>), or if it
        /// stores its assets as direct references (<see langword="false"/>).
        /// </summary>
        /// <remarks>
        /// If this property is <see langword="true"/>, <see
        /// cref="GetLocalizedObjectAsync"/> and <see
        /// cref="ContainsLocalizedObject"/> should not be used to retrieve
        /// localised objects. Instead, the Addressable Assets API should be
        /// used.
        /// </remarks>
        public bool UsesAddressableAssets { get => _usesAddressableAssets; internal set => _usesAddressableAssets = value; }

        [SerializeField]
        private bool _containsLocalizedAssets;

        [SerializeField]
        internal bool _usesAddressableAssets;

        #region Localized Strings
        public string? GetLocalizedString(string key)
        {
            if (_runtimeStringTable.TryGetValue(key, out string result))
            {
                return result;
            }

            if (entries.TryGetValue(key, out var entry))
            {
                return entry.localizedString;
            }

            return null;
        }

        /// <summary>
        /// Returns a boolean value indicating whether this <see
        /// cref="Localization"/> contains a string with the given key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns><see langword="true"/> if this Localization has a string
        /// for the given key; <see langword="false"/> otherwise.</returns>
        public bool ContainsLocalizedString(string key) => _runtimeStringTable.ContainsKey(key) || entries.ContainsKey(key);

#if UNITY_EDITOR
        /// <summary>
        /// Adds a new string to the string table.
        /// </summary>
        /// <remarks>
        /// <para>This method updates the localisation asset on disk. It is not
        /// recommended to call this method during play mode, because changes
        /// will persist after you leave and may cause conflicts. </para>
        /// <para>This method is only available in the Editor.</para>
        /// </remarks>
        /// <param name="key">The key for this string (generally, the line
        /// ID.)</param>
        /// <param name="value">The user-facing text for this string, in the
        /// language specified by <see cref="LocaleCode"/>.</param>
        internal void AddLocalisedStringToAsset(string key, string value)
        {
            GetOrCreateEntry(key).localizedString = value;
        }
#endif

        /// <summary>
        /// Adds a new string to the runtime string table.
        /// </summary>
        /// <remarks>
        /// This method updates the localisation's runtime string table, which
        /// is useful for adding or changing the localisation during gameplay or
        /// in a built player. It doesn't modify the asset on disk, and any
        /// changes made will be lost when gameplay ends.
        /// </remarks>
        /// <param name="key">The key for this string (generally, the line
        /// ID.)</param>
        /// <param name="value">The user-facing text for this string, in the
        /// language specified by <see cref="LocaleCode"/>.</param>
        public void AddLocalizedString(string key, string value)
        {
            _runtimeStringTable.Add(key, value);
        }

        /// <summary>
        /// Adds a collection of strings to the runtime string table.
        /// </summary>
        /// <inheritdoc cref="AddLocalizedString(string, string)"
        /// path="/remarks"/>
        /// <param name="strings">The collection of keys and strings to
        /// add.</param>
        public void AddLocalizedStrings(IEnumerable<KeyValuePair<string, string>> strings)
        {
            foreach (var entry in strings)
            {
                AddLocalizedString(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Adds a collection of strings to the runtime string table.
        /// </summary>
        /// <inheritdoc cref="AddLocalizedString(string, string)"
        /// path="/remarks"/>
        /// <param name="strings">The collection of <see
        /// cref="StringTableEntry"/> objects to add.</param>
        public void AddLocalizedStrings(IEnumerable<StringTableEntry> stringTableEntries)
        {
            foreach (var entry in stringTableEntries)
            {
                if (entry.Text != null)
                {
                    AddLocalizedString(entry.ID, entry.Text);
                }
            }
        }

        #endregion

        #region Localised Objects

#if USE_ADDRESSABLES
        /// <summary>
        /// Attempts to load an asset for the localised line.
        /// </summary>
        /// <remarks>
        /// This method does not take in a cancellation token because there is no mechanism to pass it through to the underlying addressables load request
        /// </remarks>
        /// <typeparam name="T">The expected type of the asset</typeparam>
        /// <param name="key">the line id of the localised entry whos asset you're after</param>
        /// <returns>The localised asset, or null if there wasn't one found</returns>
        public async YarnTask<T?> GetLocalizedObjectAsync<T>(string key) where T : UnityEngine.Object
        {
            if (!entries.TryGetValue(key, out var entry))
            {
                return null;
            }

            if (_usesAddressableAssets)
            {
                if (entry.localizedAssetReference == null || entry.localizedAssetReference.RuntimeKeyIsValid() == false) { return null; }

                // Try to fetch the referenced asset
                return await UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(entry.localizedAssetReference).Task;
            }

            if (entry.localizedAsset is T resultAsTargetObject)
            {
                return resultAsTargetObject;
            }

            return null;
        }
#else
        /// <summary>
        /// Attempts to load an asset for the localised line.
        /// </summary>
        /// <remarks>
        /// While this is called GetLocalizedObjectAsync and is a YarnTask because this is loading using the normal file IO in unity it will be a synchronous method
        /// </remarks>
        /// <typeparam name="T">The expected type of the asset</typeparam>
        /// <param name="key">the line id of the localised entry whos asset you're after</param>
        /// <returns>The localised asset, or null if there wasn't one found</returns>
        public YarnTask<T?> GetLocalizedObjectAsync<T>(string key) where T : UnityEngine.Object
        {
            if (!entries.TryGetValue(key, out var entry))
            {
                return YarnTask<T?>.FromResult(null);
            }

            if (entry.localizedAsset is T resultAsTargetObject)
            {
                return YarnTask.FromResult<T?>(resultAsTargetObject);
            }

            return YarnTask<T?>.FromResult(null);
        }
#endif


#if UNITY_EDITOR
        internal T? GetLocalizedObjectSync<T>(string key) where T : UnityEngine.Object
        {
            if (!entries.TryGetValue(key, out var entry))
            {
                return null;
            }

#if USE_ADDRESSABLES
            if (_usesAddressableAssets)
            {
                if (entry.localizedAssetReference == null || entry.localizedAssetReference.RuntimeKeyIsValid() == false) { return null; }

                // Try to fetch the referenced asset
                return entry.localizedAssetReference.editorAsset as T;
            }
#endif

            if (entry.localizedAsset is T resultAsTargetObject)
            {
                return resultAsTargetObject;
            }

            return null;
        }
#endif

        private LocalizationTableEntry GetOrCreateEntry(string key)
        {
            if (entries.TryGetValue(key, out var entry))
            {
                return entry;
            }
            entry = new LocalizationTableEntry();
            entries.Add(key, entry);
            return entry;
        }

        /// <summary>
        /// Checks if there is a localised asset of the requested type for the requested line
        /// </summary>
        /// <typeparam name="T">The expected asset type</typeparam>
        /// <param name="key">the id of the localised line</param>
        /// <returns>true if there is an asset of the requested type under the requested key, false otherwise.</returns>
        public bool ContainsLocalizedObject<T>(string key) where T : UnityEngine.Object => entries.TryGetValue(key, out var asset) && asset is T;

#if UNITY_EDITOR
        public void AddLocalizedObjectToAsset<T>(string key, T value) where T : UnityEngine.Object
        {
            var entry = GetOrCreateEntry(key);

#if USE_ADDRESSABLES
            if (this.UsesAddressableAssets)
            {
                // This Localization uses Addressables, so rather than storing a
                // direct reference to the asset, we'll use an indirect
                // AssetReference.
                entry.localizedAssetReference = new UnityEngine.AddressableAssets.AssetReference();
                entry.localizedAssetReference.SetEditorAsset(value);
                entry.localizedAsset = null;
                return;
            }
            else
            {
                // Addressables are available, but we're not using addressable
                // assets, so clear out any asset references.
                entry.localizedAssetReference = null;
            }
#endif
            entry.localizedAsset = value;
        }
#endif
        #endregion

        /// <summary>
        /// Removes all entries from the localisation
        /// </summary>
        public virtual void Clear()
        {
            entries.Clear();
            _runtimeStringTable.Clear();
        }

        /// <summary>
        /// Gets the line IDs present in this localization.
        /// </summary>
        /// <remarks>
        /// The line IDs can be used to access the localized text or asset
        /// associated with a line.
        /// </remarks>
        /// <returns>The line IDs.</returns>
        public IEnumerable<string> GetLineIDs()
        {
            var allKeys = new List<string>();

            var runtimeKeys = _runtimeStringTable.Keys;
            var compileTimeKeys = entries.Keys;

            allKeys.AddRange(runtimeKeys);
            allKeys.AddRange(compileTimeKeys);

            return allKeys;
        }
    }
}
