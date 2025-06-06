/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    public abstract class SerializableDictionaryBase
    {
        public abstract class Storage { }

        protected class Dictionary<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, TValue>
        {
            public Dictionary() { }
            public Dictionary(IDictionary<TKey, TValue> dict) : base(dict) { }
            public Dictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }

    [Serializable]
    public abstract class SerializableDictionaryBase<TKey, TValue, TValueStorage> : SerializableDictionaryBase, IDictionary<TKey, TValue>, IDictionary, ISerializationCallbackReceiver, IDeserializationCallback, ISerializable
    {
        Dictionary<TKey, TValue> m_dict;
        [SerializeField]
        TKey[]? m_keys;
        [SerializeField]
        TValueStorage[]? m_values;

        public SerializableDictionaryBase()
        {
            m_dict = new Dictionary<TKey, TValue>();
        }

        public SerializableDictionaryBase(IDictionary<TKey, TValue> dict)
        {
            m_dict = new Dictionary<TKey, TValue>(dict);
        }

        protected abstract void SetValue(TValueStorage[] storage, int i, TValue value);
        protected abstract TValue GetValue(TValueStorage[] storage, int i);

        public void CopyFrom(IDictionary<TKey, TValue> dict)
        {
            m_dict.Clear();
            foreach (var kvp in dict)
            {
                m_dict[kvp.Key] = kvp.Value;
            }
        }

        public void OnAfterDeserialize()
        {
            if (m_keys != null && m_values != null && m_keys.Length == m_values.Length)
            {
                m_dict.Clear();
                int n = m_keys.Length;
                for (int i = 0; i < n; ++i)
                {
                    m_dict[m_keys[i]] = GetValue(m_values, i);
                }

                m_keys = null;
                m_values = null;
            }
        }

        public void OnBeforeSerialize()
        {
            int n = m_dict.Count;
            m_keys = new TKey[n];
            m_values = new TValueStorage[n];

            int i = 0;
            foreach (var kvp in m_dict)
            {
                m_keys[i] = kvp.Key;
                SetValue(m_values, i, kvp.Value);
                ++i;
            }
        }

        #region IDictionary<TKey, TValue>

        public ICollection<TKey> Keys { get { return ((IDictionary<TKey, TValue>)m_dict).Keys; } }
        public ICollection<TValue> Values { get { return ((IDictionary<TKey, TValue>)m_dict).Values; } }
        public int Count { get { return ((IDictionary<TKey, TValue>)m_dict).Count; } }
        public bool IsReadOnly { get { return ((IDictionary<TKey, TValue>)m_dict).IsReadOnly; } }

        public TValue this[TKey key]
        {
            get { return ((IDictionary<TKey, TValue>)m_dict)[key]; }
            set { ((IDictionary<TKey, TValue>)m_dict)[key] = value; }
        }

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)m_dict).Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)m_dict).ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)m_dict).Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>)m_dict).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)m_dict).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<TKey, TValue>)m_dict).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)m_dict).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)m_dict).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)m_dict).Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)m_dict).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)m_dict).GetEnumerator();
        }

        #endregion

        #region IDictionary

        bool IDictionary.IsFixedSize { get { return ((IDictionary)m_dict).IsFixedSize; } }
        ICollection IDictionary.Keys { get { return ((IDictionary)m_dict).Keys; } }
        ICollection IDictionary.Values { get { return ((IDictionary)m_dict).Values; } }
        bool ICollection.IsSynchronized { get { return ((IDictionary)m_dict).IsSynchronized; } }
        object ICollection.SyncRoot { get { return ((IDictionary)m_dict).SyncRoot; } }

        object IDictionary.this[object key]
        {
            get { return ((IDictionary)m_dict)[key]; }
            set { ((IDictionary)m_dict)[key] = value; }
        }

        void IDictionary.Add(object key, object value)
        {
            ((IDictionary)m_dict).Add(key, value);
        }

        bool IDictionary.Contains(object key)
        {
            return ((IDictionary)m_dict).Contains(key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)m_dict).GetEnumerator();
        }

        void IDictionary.Remove(object key)
        {
            ((IDictionary)m_dict).Remove(key);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((IDictionary)m_dict).CopyTo(array, index);
        }

        #endregion

        #region IDeserializationCallback

        public void OnDeserialization(object sender)
        {
            ((IDeserializationCallback)m_dict).OnDeserialization(sender);
        }

        #endregion

        #region ISerializable

        protected SerializableDictionaryBase(SerializationInfo info, StreamingContext context)
        {
            m_dict = new Dictionary<TKey, TValue>(info, context);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ((ISerializable)m_dict).GetObjectData(info, context);
        }

        #endregion
    }

    public static class SerializableDictionary
    {
        public class Storage<T> : SerializableDictionaryBase.Storage
        {
            [System.Diagnostics.CodeAnalysis.AllowNull]
            public T data;
        }
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : SerializableDictionaryBase<TKey, TValue, TValue>
    {
        public SerializableDictionary() { }
        public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict) { }
        protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

        protected override TValue GetValue(TValue[] storage, int i)
        {
            return storage[i];
        }

        protected override void SetValue(TValue[] storage, int i, TValue value)
        {
            storage[i] = value;
        }
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue, TValueStorage> : SerializableDictionaryBase<TKey, TValue, TValueStorage> where TValueStorage : SerializableDictionary.Storage<TValue>, new()
    {
        public SerializableDictionary() { }
        public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict) { }
        protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

        protected override TValue GetValue(TValueStorage[] storage, int i)
        {
            return storage[i].data;
        }

        protected override void SetValue(TValueStorage[] storage, int i, TValue value)
        {
            storage[i] = new TValueStorage();
            storage[i].data = value;
        }
    }
}

#if UNITY_EDITOR
namespace Yarn.Unity.Editor
{

    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(SerializableDictionaryBase), true)]
    public class SerializableDictionaryPropertyDrawer : PropertyDrawer
    {
        const string KeysFieldName = "m_keys";
        const string ValuesFieldName = "m_values";
        protected const float IndentWidth = 15f;

        static GUIContent s_iconPlus = IconContent("Toolbar Plus", "Add entry");
        static GUIContent s_iconMinus = IconContent("Toolbar Minus", "Remove entry");
        static GUIContent s_warningIconConflict = IconContent("console.warnicon.sml", "Conflicting key, this entry will be lost");
        static GUIContent s_warningIconOther = IconContent("console.infoicon.sml", "Conflicting key");
        static GUIContent s_warningIconNull = IconContent("console.warnicon.sml", "Null key, this entry will be lost");
        static GUIStyle s_buttonStyle = GUIStyle.none;
        static GUIContent s_tempContent = new GUIContent();


        class ConflictState
        {
            public object? conflictKey = null;
            public object? conflictValue = null;
            public int conflictIndex = -1;
            public int conflictOtherIndex = -1;
            public bool conflictKeyPropertyExpanded = false;
            public bool conflictValuePropertyExpanded = false;
            public float conflictLineHeight = 0f;
        }

        struct PropertyIdentity
        {
            public PropertyIdentity(SerializedProperty property)
            {
                this.instance = property.serializedObject.targetObject;
                this.propertyPath = property.propertyPath;
            }

            public UnityEngine.Object instance;
            public string propertyPath;
        }

        static Dictionary<PropertyIdentity, ConflictState> s_conflictStateDict = new Dictionary<PropertyIdentity, ConflictState>();

        enum Action
        {
            None,
            Add,
            Remove
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            Action buttonAction = Action.None;
            int buttonActionIndex = 0;

            var keyArrayProperty = property.FindPropertyRelative(KeysFieldName);
            var valueArrayProperty = property.FindPropertyRelative(ValuesFieldName);

            ConflictState conflictState = GetConflictState(property);

            if (conflictState.conflictIndex != -1)
            {
                keyArrayProperty.InsertArrayElementAtIndex(conflictState.conflictIndex);
                var keyProperty = keyArrayProperty.GetArrayElementAtIndex(conflictState.conflictIndex);
                SetPropertyValue(keyProperty, conflictState.conflictKey);
                keyProperty.isExpanded = conflictState.conflictKeyPropertyExpanded;

                if (valueArrayProperty != null)
                {
                    valueArrayProperty.InsertArrayElementAtIndex(conflictState.conflictIndex);
                    var valueProperty = valueArrayProperty.GetArrayElementAtIndex(conflictState.conflictIndex);
                    SetPropertyValue(valueProperty, conflictState.conflictValue);
                    valueProperty.isExpanded = conflictState.conflictValuePropertyExpanded;
                }
            }

            var buttonWidth = s_buttonStyle.CalcSize(s_iconPlus).x;

            var labelPosition = position;
            labelPosition.height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
            {
                labelPosition.xMax -= s_buttonStyle.CalcSize(s_iconPlus).x;
            }

            EditorGUI.PropertyField(labelPosition, property, label, false);
            // property.isExpanded = EditorGUI.Foldout(labelPosition,
            // property.isExpanded, label);
            if (property.isExpanded)
            {
                var buttonPosition = position;
                buttonPosition.xMin = buttonPosition.xMax - buttonWidth;
                buttonPosition.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.BeginDisabledGroup(conflictState.conflictIndex != -1);
                if (GUI.Button(buttonPosition, s_iconPlus, s_buttonStyle))
                {
                    buttonAction = Action.Add;
                    buttonActionIndex = keyArrayProperty.arraySize;
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel++;
                var linePosition = position;
                linePosition.y += EditorGUIUtility.singleLineHeight;
                linePosition.xMax -= buttonWidth;

                foreach (var entry in EnumerateEntries(keyArrayProperty, valueArrayProperty))
                {
                    var keyProperty = entry.keyProperty;
                    var valueProperty = entry.valueProperty;
                    int i = entry.index;

                    float lineHeight = DrawKeyValueLine(keyProperty, valueProperty, linePosition, i);

                    buttonPosition = linePosition;
                    buttonPosition.x = linePosition.xMax;
                    buttonPosition.height = EditorGUIUtility.singleLineHeight;
                    if (GUI.Button(buttonPosition, s_iconMinus, s_buttonStyle))
                    {
                        buttonAction = Action.Remove;
                        buttonActionIndex = i;
                    }

                    if (i == conflictState.conflictIndex && conflictState.conflictOtherIndex == -1)
                    {
                        var iconPosition = linePosition;
                        iconPosition.size = s_buttonStyle.CalcSize(s_warningIconNull);
                        GUI.Label(iconPosition, s_warningIconNull);
                    }
                    else if (i == conflictState.conflictIndex)
                    {
                        var iconPosition = linePosition;
                        iconPosition.size = s_buttonStyle.CalcSize(s_warningIconConflict);
                        GUI.Label(iconPosition, s_warningIconConflict);
                    }
                    else if (i == conflictState.conflictOtherIndex)
                    {
                        var iconPosition = linePosition;
                        iconPosition.size = s_buttonStyle.CalcSize(s_warningIconOther);
                        GUI.Label(iconPosition, s_warningIconOther);
                    }


                    linePosition.y += lineHeight;
                }

                EditorGUI.indentLevel--;
            }

            if (buttonAction == Action.Add)
            {
                keyArrayProperty.InsertArrayElementAtIndex(buttonActionIndex);
                if (valueArrayProperty != null)
                {
                    valueArrayProperty.InsertArrayElementAtIndex(buttonActionIndex);
                }
            }
            else if (buttonAction == Action.Remove)
            {
                DeleteArrayElementAtIndex(keyArrayProperty, buttonActionIndex);
                if (valueArrayProperty != null)
                {
                    DeleteArrayElementAtIndex(valueArrayProperty, buttonActionIndex);
                }
            }

            conflictState.conflictKey = null;
            conflictState.conflictValue = null;
            conflictState.conflictIndex = -1;
            conflictState.conflictOtherIndex = -1;
            conflictState.conflictLineHeight = 0f;
            conflictState.conflictKeyPropertyExpanded = false;
            conflictState.conflictValuePropertyExpanded = false;

            foreach (var entry1 in EnumerateEntries(keyArrayProperty, valueArrayProperty))
            {
                var keyProperty1 = entry1.keyProperty;
                int i = entry1.index;
                object keyProperty1Value = GetPropertyValue(keyProperty1);

                if (keyProperty1Value == null)
                {
                    var valueProperty1 = entry1.valueProperty;
                    SaveProperty(keyProperty1, valueProperty1, i, -1, conflictState);
                    DeleteArrayElementAtIndex(keyArrayProperty, i);
                    if (valueArrayProperty != null)
                    {
                        DeleteArrayElementAtIndex(valueArrayProperty, i);
                    }

                    break;
                }


                foreach (var entry2 in EnumerateEntries(keyArrayProperty, valueArrayProperty, i + 1))
                {
                    var keyProperty2 = entry2.keyProperty;
                    int j = entry2.index;
                    object keyProperty2Value = GetPropertyValue(keyProperty2);

                    if (ComparePropertyValues(keyProperty1Value, keyProperty2Value))
                    {
                        var valueProperty2 = entry2.valueProperty;
                        SaveProperty(keyProperty2, valueProperty2, j, i, conflictState);
                        DeleteArrayElementAtIndex(keyArrayProperty, j);
                        if (valueArrayProperty != null)
                        {
                            DeleteArrayElementAtIndex(valueArrayProperty, j);
                        }

                        goto breakLoops;
                    }
                }
            }
        breakLoops:

            EditorGUI.EndProperty();
        }

        static float DrawKeyValueLine(SerializedProperty keyProperty, SerializedProperty valueProperty, Rect linePosition, int index)
        {
            bool keyCanBeExpanded = CanPropertyBeExpanded(keyProperty);

            if (valueProperty != null)
            {
                bool valueCanBeExpanded = CanPropertyBeExpanded(valueProperty);

                if (!keyCanBeExpanded && valueCanBeExpanded)
                {
                    return DrawKeyValueLineExpand(keyProperty, valueProperty, linePosition);
                }
                else
                {
                    var keyLabel = keyCanBeExpanded ? ("Key " + index.ToString()) : "";
                    var valueLabel = valueCanBeExpanded ? ("Value " + index.ToString()) : "";
                    return DrawKeyValueLineSimple(keyProperty, valueProperty, keyLabel, valueLabel, linePosition);
                }
            }
            else
            {
                if (!keyCanBeExpanded)
                {
                    return DrawKeyLine(keyProperty, linePosition, null);
                }
                else
                {
                    var keyLabel = string.Format("{0} {1}", ObjectNames.NicifyVariableName(keyProperty.type), index);
                    return DrawKeyLine(keyProperty, linePosition, keyLabel);
                }
            }
        }

        static float DrawKeyValueLineSimple(SerializedProperty keyProperty, SerializedProperty valueProperty, string keyLabel, string valueLabel, Rect linePosition)
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            float labelWidthRelative = labelWidth / linePosition.width;

            float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
            var keyPosition = linePosition;
            keyPosition.height = keyPropertyHeight;
            keyPosition.width = labelWidth - IndentWidth;
            EditorGUIUtility.labelWidth = keyPosition.width * labelWidthRelative;
            EditorGUI.PropertyField(keyPosition, keyProperty, TempContent(keyLabel), true);

            float valuePropertyHeight = EditorGUI.GetPropertyHeight(valueProperty);
            var valuePosition = linePosition;
            valuePosition.height = valuePropertyHeight;
            valuePosition.xMin += labelWidth;
            EditorGUIUtility.labelWidth = valuePosition.width * labelWidthRelative;
            EditorGUI.indentLevel--;
            EditorGUI.PropertyField(valuePosition, valueProperty, TempContent(valueLabel), true);
            EditorGUI.indentLevel++;

            EditorGUIUtility.labelWidth = labelWidth;

            return Mathf.Max(keyPropertyHeight, valuePropertyHeight);
        }

        static float DrawKeyValueLineExpand(SerializedProperty keyProperty, SerializedProperty valueProperty, Rect linePosition)
        {
            float labelWidth = EditorGUIUtility.labelWidth;

            float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
            var keyPosition = linePosition;
            keyPosition.height = keyPropertyHeight;
            keyPosition.width = labelWidth - IndentWidth;
            EditorGUI.PropertyField(keyPosition, keyProperty, GUIContent.none, true);

            float valuePropertyHeight = EditorGUI.GetPropertyHeight(valueProperty);
            var valuePosition = linePosition;
            valuePosition.height = valuePropertyHeight;
            EditorGUI.PropertyField(valuePosition, valueProperty, GUIContent.none, true);

            EditorGUIUtility.labelWidth = labelWidth;

            return Mathf.Max(keyPropertyHeight, valuePropertyHeight);
        }

        static float DrawKeyLine(SerializedProperty keyProperty, Rect linePosition, string? keyLabel)
        {
            float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
            var keyPosition = linePosition;
            keyPosition.height = keyPropertyHeight;
            keyPosition.width = linePosition.width;

            var keyLabelContent = keyLabel != null ? TempContent(keyLabel) : GUIContent.none;
            EditorGUI.PropertyField(keyPosition, keyProperty, keyLabelContent, true);

            return keyPropertyHeight;
        }

        static bool CanPropertyBeExpanded(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Quaternion:
                    return true;
                default:
                    return false;
            }
        }

        static void SaveProperty(SerializedProperty keyProperty, SerializedProperty? valueProperty, int index, int otherIndex, ConflictState conflictState)
        {
            conflictState.conflictKey = GetPropertyValue(keyProperty);
            conflictState.conflictValue = valueProperty != null ? GetPropertyValue(valueProperty) : null;
            float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
            float valuePropertyHeight = valueProperty != null ? EditorGUI.GetPropertyHeight(valueProperty) : 0f;
            float lineHeight = Mathf.Max(keyPropertyHeight, valuePropertyHeight);
            conflictState.conflictLineHeight = lineHeight;
            conflictState.conflictIndex = index;
            conflictState.conflictOtherIndex = otherIndex;
            conflictState.conflictKeyPropertyExpanded = keyProperty.isExpanded;
            conflictState.conflictValuePropertyExpanded = valueProperty != null ? valueProperty.isExpanded : false;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float propertyHeight = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                var keysProperty = property.FindPropertyRelative(KeysFieldName);
                var valuesProperty = property.FindPropertyRelative(ValuesFieldName);

                foreach (var entry in EnumerateEntries(keysProperty, valuesProperty))
                {
                    var keyProperty = entry.keyProperty;
                    var valueProperty = entry.valueProperty;
                    float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
                    float valuePropertyHeight = valueProperty != null ? EditorGUI.GetPropertyHeight(valueProperty) : 0f;
                    float lineHeight = Mathf.Max(keyPropertyHeight, valuePropertyHeight);
                    propertyHeight += lineHeight;
                }

                ConflictState conflictState = GetConflictState(property);

                if (conflictState.conflictIndex != -1)
                {
                    propertyHeight += conflictState.conflictLineHeight;
                }
            }

            return propertyHeight;
        }

        static ConflictState GetConflictState(SerializedProperty property)
        {
            ConflictState conflictState;
            PropertyIdentity propId = new PropertyIdentity(property);
            if (!s_conflictStateDict.TryGetValue(propId, out conflictState))
            {
                conflictState = new ConflictState();
                s_conflictStateDict.Add(propId, conflictState);
            }
            return conflictState;
        }

        static Dictionary<SerializedPropertyType, PropertyInfo> s_serializedPropertyValueAccessorsDict;

        static SerializableDictionaryPropertyDrawer()
        {
            Dictionary<SerializedPropertyType, string> serializedPropertyValueAccessorsNameDict = new Dictionary<SerializedPropertyType, string>() {
            { SerializedPropertyType.Integer, "intValue" },
            { SerializedPropertyType.Boolean, "boolValue" },
            { SerializedPropertyType.Float, "floatValue" },
            { SerializedPropertyType.String, "stringValue" },
            { SerializedPropertyType.Color, "colorValue" },
            { SerializedPropertyType.ObjectReference, "objectReferenceValue" },
            { SerializedPropertyType.LayerMask, "intValue" },
            { SerializedPropertyType.Enum, "intValue" },
            { SerializedPropertyType.Vector2, "vector2Value" },
            { SerializedPropertyType.Vector3, "vector3Value" },
            { SerializedPropertyType.Vector4, "vector4Value" },
            { SerializedPropertyType.Rect, "rectValue" },
            { SerializedPropertyType.ArraySize, "intValue" },
            { SerializedPropertyType.Character, "intValue" },
            { SerializedPropertyType.AnimationCurve, "animationCurveValue" },
            { SerializedPropertyType.Bounds, "boundsValue" },
            { SerializedPropertyType.Quaternion, "quaternionValue" },
        };
            Type serializedPropertyType = typeof(SerializedProperty);

            s_serializedPropertyValueAccessorsDict = new Dictionary<SerializedPropertyType, PropertyInfo>();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            foreach (var kvp in serializedPropertyValueAccessorsNameDict)
            {
                PropertyInfo propertyInfo = serializedPropertyType.GetProperty(kvp.Value, flags);
                s_serializedPropertyValueAccessorsDict.Add(kvp.Key, propertyInfo);
            }
        }

        static GUIContent IconContent(string name, string tooltip)
        {
            var builtinIcon = EditorGUIUtility.IconContent(name);
            return new GUIContent(builtinIcon.image, tooltip);
        }

        static GUIContent TempContent(string text)
        {
            s_tempContent.text = text;
            return s_tempContent;
        }

        static void DeleteArrayElementAtIndex(SerializedProperty arrayProperty, int index)
        {
            var property = arrayProperty.GetArrayElementAtIndex(index);
            // if(arrayProperty.arrayElementType.StartsWith("PPtr<$"))
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = null;
            }

            arrayProperty.DeleteArrayElementAtIndex(index);
        }

        public static object GetPropertyValue(SerializedProperty p)
        {
            PropertyInfo propertyInfo;
            if (s_serializedPropertyValueAccessorsDict.TryGetValue(p.propertyType, out propertyInfo))
            {
                return propertyInfo.GetValue(p, null);
            }
            else
            {
                if (p.isArray)
                {
                    return GetPropertyValueArray(p);
                }
                else
                {
                    return GetPropertyValueGeneric(p);
                }
            }
        }

        static void SetPropertyValue(SerializedProperty p, object? v)
        {
            PropertyInfo propertyInfo;
            if (s_serializedPropertyValueAccessorsDict.TryGetValue(p.propertyType, out propertyInfo))
            {
                propertyInfo.SetValue(p, v, null);
            }
            else
            {
                if (p.isArray && v != null)
                {
                    SetPropertyValueArray(p, v);
                }
                else
                {
                    SetPropertyValueGeneric(p, v);
                }
            }
        }

        static object GetPropertyValueArray(SerializedProperty property)
        {
            object[] array = new object[property.arraySize];
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty item = property.GetArrayElementAtIndex(i);
                array[i] = GetPropertyValue(item);
            }
            return array;
        }

        static object GetPropertyValueGeneric(SerializedProperty property)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            var iterator = property.Copy();
            if (iterator.Next(true))
            {
                var end = property.GetEndProperty();
                do
                {
                    string name = iterator.name;
                    object value = GetPropertyValue(iterator);
                    dict.Add(name, value);
                } while (iterator.Next(false) && iterator.propertyPath != end.propertyPath);
            }
            return dict;
        }

        static void SetPropertyValueArray(SerializedProperty property, object v)
        {
            object[] array = (object[])v;
            property.arraySize = array.Length;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty item = property.GetArrayElementAtIndex(i);
                SetPropertyValue(item, array[i]);
            }
        }

        static void SetPropertyValueGeneric(SerializedProperty property, object? v)
        {
            Dictionary<string, object>? dict = (Dictionary<string, object>?)v;
            var iterator = property.Copy();
            if (iterator.Next(true))
            {
                var end = property.GetEndProperty();
                do
                {
                    string name = iterator.name;
                    SetPropertyValue(iterator, dict?[name]);
                } while (iterator.Next(false) && iterator.propertyPath != end.propertyPath);
            }
        }

        static bool ComparePropertyValues(object value1, object value2)
        {
            if (value1 is Dictionary<string, object> && value2 is Dictionary<string, object>)
            {
                var dict1 = (Dictionary<string, object>)value1;
                var dict2 = (Dictionary<string, object>)value2;
                return CompareDictionaries(dict1, dict2);
            }
            else
            {
                return object.Equals(value1, value2);
            }
        }

        static bool CompareDictionaries(Dictionary<string, object> dict1, Dictionary<string, object> dict2)
        {
            if (dict1.Count != dict2.Count)
            {
                return false;
            }

            foreach (var kvp1 in dict1)
            {
                var key1 = kvp1.Key;
                object value1 = kvp1.Value;

                object value2;
                if (!dict2.TryGetValue(key1, out value2))
                {
                    return false;
                }

                if (!ComparePropertyValues(value1, value2))
                {
                    return false;
                }
            }

            return true;
        }

        struct EnumerationEntry
        {
            public SerializedProperty keyProperty;
            public SerializedProperty valueProperty;
            public int index;

            public EnumerationEntry(SerializedProperty keyProperty, SerializedProperty valueProperty, int index)
            {
                this.keyProperty = keyProperty;
                this.valueProperty = valueProperty;
                this.index = index;
            }
        }

        static IEnumerable<EnumerationEntry> EnumerateEntries(SerializedProperty keyArrayProperty, SerializedProperty? valueArrayProperty, int startIndex = 0)
        {
            if (valueArrayProperty == null)
            {
                yield break;
            }

            if (keyArrayProperty.arraySize > startIndex)
            {
                int index = startIndex;
                var keyProperty = keyArrayProperty.GetArrayElementAtIndex(startIndex);
                var valueProperty = valueArrayProperty != null ? valueArrayProperty.GetArrayElementAtIndex(startIndex) : null;
                var endProperty = keyArrayProperty.GetEndProperty();

                do
                {
                    if (valueProperty != null)
                    {
                        yield return new EnumerationEntry(keyProperty, valueProperty, index);
                    }
                    index++;
                } while (keyProperty.Next(false)
                    && (valueProperty != null ? valueProperty.Next(false) : true)
                    && !SerializedProperty.EqualContents(keyProperty, endProperty));
            }
        }
    }

    [CustomPropertyDrawer(typeof(SerializableDictionaryBase.Storage), true)]
    public class SerializableDictionaryStoragePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.Next(true);
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            property.Next(true);
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}
#endif
