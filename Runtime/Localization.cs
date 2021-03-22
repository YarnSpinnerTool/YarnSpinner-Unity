using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace Yarn.Unity
{

    public class Localization : ScriptableObject
    {
        /// <summary>
        /// Returns the address that should be used to fetch an asset
        /// suitable for a specific line in a specific language.
        /// </summary>
        /// <param name="lineID">The line ID to use when generating the address.</param>
        /// <param name="language">The language to use when generating the address.</param>
        /// <returns>The address to use.</returns>
        internal static string GetAddressForLine(string lineID, string language) {
            return $"line_{language}_{lineID.Replace("line:", "")}";
        }

        public string LocaleCode { get => _LocaleCode; set => _LocaleCode = value; }

        [SerializeField] private string _LocaleCode;

        [System.Serializable]
        class StringDictionary : SerializedDictionary<string, string> { }

        [System.Serializable]
        class AssetDictionary : SerializedDictionary<string, UnityEngine.Object> { }

        [SerializeField] private StringDictionary _stringTable = new StringDictionary();
        [SerializeField] private AssetDictionary _assetTable = new AssetDictionary();

        private Dictionary<string, string> _runtimeStringTable = new Dictionary<string, string>();

        public bool ContainsLocalizedAssets { get => _containsLocalizedAssets; internal set => _containsLocalizedAssets = value; }
        
        public bool UsesAddressableAssets { get => _usesAddressableAssets; internal set => _usesAddressableAssets = value; }

        [SerializeField]
        private bool _containsLocalizedAssets;

        [SerializeField]
        private bool _usesAddressableAssets;

        #region Localized Strings
        public string GetLocalizedString(string key)
        {
            string result;
            if (_runtimeStringTable.TryGetValue(key, out result))
            {
                return result;
            }

            if (_stringTable.TryGetValue(key, out result))
            {
                return result;
            }

            return null;
        }

        public bool ContainsLocalizedString(string key) => _runtimeStringTable.ContainsKey(key) || _stringTable.ContainsKey(key);

        public void AddLocalizedString(string key, string value)
        {
            if (Application.isPlaying)
            {
                _runtimeStringTable.Add(key, value);
            }
            else
            {
                _stringTable.Add(key, value);
            }
        }

        public void AddLocalizedStrings(IEnumerable<KeyValuePair<string, string>> strings)
        {
            foreach (var entry in strings)
            {
                AddLocalizedString(entry.Key, entry.Value);
            }
        }

        public void AddLocalizedStrings(IEnumerable<StringTableEntry> stringTableEntries)
        {
            foreach (var entry in stringTableEntries)
            {
                AddLocalizedString(entry.ID, entry.Text);
            }
        }

        #endregion

        #region Localised Objects

        public T GetLocalizedObject<T>(string key) where T : UnityEngine.Object
        {
            if (_usesAddressableAssets) {
                Debug.LogWarning($"Localization {name} uses addressable assets. Use the Addressable Assets API to load the asset.");
            }

            _assetTable.TryGetValue(key, out var result);

            if (result is T resultAsTargetObject)
            {
                return resultAsTargetObject;
            }

            return null;
        }

        public void SetLocalizedObject<T>(string key, T value) where T : UnityEngine.Object => _assetTable.Add(key, value);

        public bool ContainsLocalizedObject<T>(string key) where T : UnityEngine.Object => _assetTable.ContainsKey(key) && _assetTable[key] is T;

        public void AddLocalizedObject<T>(string key, T value) where T : UnityEngine.Object => _assetTable.Add(key, value);

        public void AddLocalizedObjects<T>(IEnumerable<KeyValuePair<string, T>> objects) where T : UnityEngine.Object
        {
            foreach (var entry in objects)
            {
                _assetTable.Add(entry.Key, entry.Value);
            }
        }
        #endregion

        public virtual void Clear()
        {
            _stringTable.Clear();
            _assetTable.Clear();
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
            var compileTimeKeys = _stringTable.Keys;

            allKeys.AddRange(runtimeKeys);
            allKeys.AddRange(compileTimeKeys);

            return allKeys;
        }
    }
}

#if UNITY_EDITOR
namespace Yarn.Unity
{
    /// <summary>
    /// Provides methods for finding voice over <see cref="AudioClip"/>s in
    /// the project matching a Yarn linetag/string ID and a language ID.
    /// </summary>
    public static class FindVoiceOver
    {
        /// <summary>
        /// Finds all voice over <see cref="AudioClip"/>s in the project
        /// with a filename matching a Yarn linetag and a language ID.
        /// </summary>
        /// <param name="linetag">The linetag/string ID the voice over
        /// filename should match.</param>
        /// <param name="language">The language ID the voice over filename
        /// should match.</param>
        /// <returns>A string array with GUIDs of all matching <see
        /// cref="AudioClip"/>s.</returns>
        public static string[] GetMatchingVoiceOverAudioClip(string linetag, string language)
        {
            var lineTagContents = linetag.Replace("line:", "");

            string[] result = null;
            string[] searchPatterns = new string[] {
                $"t:AudioClip {lineTagContents} ({language})",
                $"t:AudioClip {lineTagContents}  {language}",
                $"t:AudioClip {lineTagContents}"
            };

            foreach (var searchPattern in searchPatterns)
            {
                result = SearchAssetDatabase(searchPattern, language);
                if (result.Length > 0)
                {
                    return result;
                }
            }

            return result;
        }

        public static string[] SearchAssetDatabase(string searchPattern, string language)
        {
            var result = AssetDatabase.FindAssets(searchPattern);
            // Check if result is ambiguous and try to improve the
            // situation
            if (result.Length > 1)
            {
                var assetsInMatchingLanguageDirectory = GetAsseetsInMatchingLanguageDirectory(result, language);
                // Check if this improved the situation
                if (assetsInMatchingLanguageDirectory.Length == 1 || (assetsInMatchingLanguageDirectory.Length != 0 && assetsInMatchingLanguageDirectory.Length < result.Length))
                {
                    result = assetsInMatchingLanguageDirectory;
                }
            }
            return result;
        }

        public static string[] GetAsseetsInMatchingLanguageDirectory(string[] result, string language)
        {
            var list = new List<string>();
            foreach (var assetId in result)
            {
                var testPath = AssetDatabase.GUIDToAssetPath(assetId);
                if (AssetDatabase.GUIDToAssetPath(assetId).Contains($"/{language}/"))
                {
                    list.Add(assetId);
                }
            }
            return list.ToArray();
        }
    }
}

#endif

