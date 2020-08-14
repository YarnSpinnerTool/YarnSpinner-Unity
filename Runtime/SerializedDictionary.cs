using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Yarn.Unity
{
    /// <summary>
    /// An <see cref="IDictionary{TKey,TValue}"/> that can be serialized as
    /// part of a Unity object.
    /// </summary>
    /// <remarks>
    /// Prior to Unity 2020, dictionaries cannot be directly serialized by
    /// Unity. This class is a workaround; it provides an API identical to
    /// <see cref="Dictionary{TKey, TValue}"/>, and stores its contents as
    /// two <see cref="List{T}"/>s: one for <typeparamref name="TKey"/>,
    /// and one for <typeparamref name="TValue"/>.
    /// </remarks>
    /// <typeparam name="TKey">The type of key used in the
    /// dictionary.</typeparam>
    /// <typeparam name="TValue">The type of value used in the
    /// dictionary.</typeparam>
    /// <inheritdoc cref="IDictionary{TKey, TValue}"/>
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
}


