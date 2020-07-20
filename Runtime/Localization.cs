using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace Yarn.Unity
{
    [System.Serializable]
    public class SerializedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys;
        [SerializeField] private List<TValue> values;

        private readonly Dictionary<TKey, TValue> table = new Dictionary<TKey, TValue>();

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)table).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)table).Values;

        public int Count => ((ICollection<KeyValuePair<TKey, TValue>>)table).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)table).IsReadOnly;

        public TValue this[TKey key] { get => ((IDictionary<TKey, TValue>)table)[key]; set => ((IDictionary<TKey, TValue>)table)[key] = value; }

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)table).Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)table).ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)table).Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>)table).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)table).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)table).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)table).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)table).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)table).Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)table).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)table).GetEnumerator();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            keys = new List<TKey>();
            values = new List<TValue>();

            if (table == null)
            {
                return;
            }

            foreach (var kvp in table)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            table.Clear();
            for (int i = 0; i != Mathf.Min(keys.Count, values.Count); i++)
            {
                table.Add(keys[i], values[i]);
            }
        }
    }

    public class Localization : ScriptableObject
    {
        public string LocaleCode { get => _LocaleCode; set => _LocaleCode = value; }

        [SerializeField] private string _LocaleCode;

#if UNITY_EDITOR
        [SerializeField] DefaultAsset assetSourceFolder;
        public DefaultAsset AssetSourceFolder => assetSourceFolder;
#endif


        [System.Serializable]
        class StringDictionary : SerializedDictionary<string, string> { }

        [System.Serializable]
        class AssetDictionary : SerializedDictionary<string, UnityEngine.Object> { }

        [SerializeField] private StringDictionary _stringTable = new StringDictionary();
        [SerializeField] private AssetDictionary _assetTable = new AssetDictionary();


#if ADDRESSABLES
        [System.Serializable]
        class AddressDictionary : SerializedDictionary<string, UnityEngine.AddressableAssets.AssetReference> { }

        [SerializeField] private AddressDictionary _addressTable = new AddressDictionary();
#endif


        #region Localized Strings
        public string GetLocalizedString(string key)
        {
            _stringTable.TryGetValue(key, out var result);
            return result;
        }

        public void SetLocalizedString(string key, string value) => _stringTable.Add(key, value);

        public bool ContainsLocalizedString(string key) => _stringTable.ContainsKey(key);

        public void AddLocalizedString(string key, string  value) => _stringTable.Add(key, value);

        public void AddLocalizedStrings(IEnumerable<KeyValuePair<string, string>> strings)
        {
            foreach (var entry in strings)
            {
                _stringTable.Add(entry.Key, entry.Value);
            }
        }

        public void AddLocalizedStrings(IEnumerable<StringTableEntry> parsedStringTableEntries) {
            foreach (var entry in parsedStringTableEntries) {
                var id = entry.ID;//.Replace("line:", "");
                _stringTable.Add(id, entry.Text);
            }
        }

        #endregion

        #region Localised Objects

        public T GetLocalizedObject<T>(string key) where T : UnityEngine.Object
        {
            _assetTable.TryGetValue(key, out var result);

            if (result is T resultAsTargetObject)
            {
                return resultAsTargetObject;
            }
            return null;
        }

        public void SetLocalizedObject<T>(string key, T value) where T : UnityEngine.Object => _assetTable.Add(key, value);

        public bool ContainsLocalizedObject<T>(string key) where T : UnityEngine.Object => _assetTable.ContainsKey(key) && _assetTable[key] is T;

        public void AddLocalizedObject<T>(string key, T value) where T: UnityEngine.Object => _assetTable.Add(key, value);

        public void AddLocalizedObjects<T>(IEnumerable<KeyValuePair<string, T>> objects) where T : UnityEngine.Object
        {
            foreach (var entry in objects)
            {
                _assetTable.Add(entry.Key, entry.Value);
            }
        }
        #endregion

#if ADDRESSABLES
        #region Localized Addresses

        public UnityEngine.AddressableAssets.AssetReference GetLocalizedObjectAddress(string key)
        {
            _addressTable.TryGetValue(key, out var result);

            return result;
        }

        public void SetLocalizedObjectAddress(string key, UnityEngine.AddressableAssets.AssetReference value) => _addressTable.Add(key, value);

        public bool ContainsLocalizedObjectAddress(string key) => _addressTable.ContainsKey(key);

        public void AddLocalizedObjectAddress(string key, UnityEngine.AddressableAssets.AssetReference value) => _addressTable.Add(key, value);

        public void AddLocalizedObjectAddresses(IEnumerable<KeyValuePair<string, UnityEngine.AddressableAssets.AssetReference>> objects)
        {

            foreach (var entry in objects)
            {
                _addressTable.Add(entry.Key, entry.Value);
            }
        }

        #endregion
#endif

        public virtual void Clear()
        {
            _stringTable.Clear();
            _assetTable.Clear();
#if ADDRESSABLES
            _addressTable.Clear();
#endif
        }
    }
}

#if UNITY_EDITOR
namespace Yarn.Unity
{
    /// <summary>
    /// Provides methods for finding voice over <see cref="AudioClip"/>s in the project matching a Yarn linetag/string ID and a language ID.
    /// </summary>
    public static class FindVoiceOver
    {
        /// <summary>
        /// Finds all voice over <see cref="AudioClip"/>s in the project with a filename matching a Yarn linetag and a language ID.
        /// </summary>
        /// <param name="linetag">The linetag/string ID the voice over filename should match.</param>
        /// <param name="language">The language ID the voice over filename should match.</param>
        /// <returns>A string array with GUIDs of all matching <see cref="AudioClip"/>s.</returns>
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
            // Check if result is ambiguous and try to improve the situation
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

