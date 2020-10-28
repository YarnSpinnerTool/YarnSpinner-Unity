/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Yarn.Unity;

namespace Yarn.Unity {

    /// <summary>
    /// An simple implementation of DialogueUnityVariableStorage, which
    /// stores everything in memory.
    /// </summary>
    /// <remarks>
    /// This class does not perform any saving or loading on its own, but
    /// you can enumerate over the variables by using a `foreach` loop:
    /// 
    /// <![CDATA[
    /// ```csharp    
    /// // 'storage' is an InMemoryVariableStorage    
    /// foreach (var variable in storage) {
    ///         string name = variable.Key;
    ///         Yarn.Value value = variable.Value;
    /// }   
    /// ```
    /// ]]>
    /// 
    /// </remarks>    
    public class InMemoryVariableStorage : VariableStorageBehaviour, IEnumerable<KeyValuePair<string, object>>
    {

        /// Where we actually keeping our variables
        [System.Serializable] class StringDictionary : SerializedDictionary<string, string> {} // serializable dictionary workaround
        private Dictionary<string, object> variables = new Dictionary<string, object>();
        private Dictionary<string, System.Type> variableTypes = new Dictionary<string, System.Type>();

        /// <summary>
        /// A default value to apply when the object wakes up, or when
        /// ResetToDefaults is called.
        /// </summary>
        [System.Serializable]
        public class DefaultVariable
        {
            /// <summary>
            /// The name of the variable.
            /// </summary>
            /// <remarks>
            /// Do not include the `$` prefix in front of the variable
            /// name. It will be added for you.
            /// </remarks>
            [Tooltip("don't include the $ prefix")] public string name;

            /// <summary>
            /// The value of the variable, as a string.
            /// </summary>
            /// <remarks>
            /// This string will be converted to the appropriate type,
            /// depending on the value of <see cref="type"/>.
            /// </remarks>
            public string value;

            /// <summary>
            /// The type of the variable.
            /// </summary>
            public Yarn.Type type;
        }

        /// <summary>
        /// The list of default variables that should be present in the
        /// InMemoryVariableStorage when the scene loads.
        /// </summary>
        [Tooltip("use DefaultVariables to override the default values of Yarn variables")] public DefaultVariable[] defaultVariables;
        public bool setDefaultVariablesOnStart = true;

        [Header("Optional debugging tools")]
        
        /// A UI.Text that can show the current list of all variables. Optional.
        [SerializeField] 
        internal UnityEngine.UI.Text debugTextView = null;

        internal void Start () {
            if ( setDefaultVariablesOnStart ) {
                SetDefaultVariables();
            }
        }

        /// If we have a debug view, show the list of all variables in it
        internal void Update ()
        {
            if (debugTextView != null) {
                var stringBuilder = new System.Text.StringBuilder ();
                foreach (KeyValuePair<string,object> item in variables) {
                    stringBuilder.AppendLine (string.Format ("{0} = {1}",
                                                            item.Key,
                                                            item.Value.ToString()));
                }
                debugTextView.text = stringBuilder.ToString ();
                debugTextView.SetAllDirty();
            }
        }


        #region Setters

        public void SetDefaultVariables () {
            foreach ( var defaultVar in defaultVariables ) {
                SetVariable( defaultVar.name, defaultVar.type, defaultVar.value );
            }
        }

        void SetVariable(string name, Yarn.Type type, string value) {
            switch (type)
            {
                case Type.Bool:
                    bool newBool;
                    if (bool.TryParse(value, out newBool))
                    {
                        SetValue(name, newBool);
                    }
                    else
                    {
                        throw new System.InvalidCastException($"Couldn't initialize default variable {name} with value {value} as Bool");
                    }
                    break;
                case Type.Number:
                    float newNumber;
                    if (float.TryParse(value, out newNumber))
                    { // TODO: this won't work for different cultures (e.g. French write "0.53" as "0,53")
                        SetValue(name, newNumber);
                    }
                    else
                    {
                        throw new System.InvalidCastException($"Couldn't initialize default variable {name} with value {value} as Number (Float)");
                    }
                    break;
                case Type.String:
                case Type.Undefined:
                default:
                    SetValue(name, value); // no special type conversion required
                    break;
            }
        }

        public override void SetValue(string variableName, string stringValue)
        {
            variables[variableName] = stringValue;
            variableTypes[variableName] = typeof(string);
        }

        public override void SetValue(string variableName, float floatValue)
        {
            variables[variableName] = floatValue;
            variableTypes[variableName] = typeof(float);
        }

        public override void SetValue(string variableName, bool boolValue)
        {
            variables[variableName] = boolValue;
            variableTypes[variableName] = typeof(bool);
        }

        /// <summary>
        /// Retrieves a <see cref="Value"/> by name.
        /// </summary>
        /// <param name="variableName">The name of the variable to retrieve
        /// the value of.</param>
        /// <returns>The <see cref="Value"/>. If a variable by the name of
        /// <paramref name="variableName"/> is not present, returns a value
        /// representing `null`.</returns>
        public override bool TryGetValue<T>(string variableName, out T result)
        {
            // If we don't have a variable with this name, return the null
            // value
            if (variables.ContainsKey(variableName) == false)
            {
                result = default;
                return false;
            }

            var resultObject = variables [variableName];
            
            if (typeof(T).IsAssignableFrom(resultObject.GetType())) {
                result = (T)resultObject;
                return true;
            } else {
                throw new System.InvalidCastException($"Variable {variableName} exists, but is the wrong type (expected {typeof(T)}, got {resultObject.GetType()}");             
            }
            
            
        }

        /// <summary>
        /// Removes all variables from storage.
        /// </summary>
        public override void Clear ()
        {
            variables.Clear();
            variableTypes.Clear();
        }

        public void ResetToDefaults ()
        {
            variables.Clear ();
            variableTypes.Clear();
            SetDefaultVariables();
        }

        #endregion
        

        #region Save/Load

        static string[] SEPARATOR = new string[] { "/" }; // used for serialization

        /// <summary>
        /// Export variable storage to a JSON string, like when writing save game data.
        /// </summary>
        public string SerializeAllVariablesToJSON(bool prettyPrint=false) {
            // "objects" aren't serializable by JsonUtility... 
            var serializableVariables = new StringDictionary();
            foreach ( var variable in variables) {
                var jsonType = variableTypes[variable.Key];
                var jsonKey = $"{jsonType}{SEPARATOR[0]}{variable.Key}"; // ... so we have to encode the System.Object type into the JSON key
                var jsonValue = System.Convert.ChangeType(variable.Value, jsonType); 
                serializableVariables.Add( jsonKey, jsonValue.ToString() );
            }
            var saveData = JsonUtility.ToJson(serializableVariables, prettyPrint);
            Debug.Log(saveData);
            return saveData;
        }

        /// <summary>
        /// Import a JSON string into variable storage, like when loading save game data.
        /// </summary>
        public void DeserializeAllVariablesFromJSON(string jsonData) {
            Debug.Log(jsonData);
            var serializedVariables = JsonUtility.FromJson<StringDictionary>( jsonData );
            foreach ( var variable in serializedVariables ) {
                var serializedKey = variable.Key.Split(SEPARATOR, 2, System.StringSplitOptions.None);
                var jsonType = System.Type.GetType(serializedKey[0]);
                var jsonKey = serializedKey[1];
                var jsonValue = variable.Value;
                SetVariable( jsonKey, TypeMappings[jsonType], jsonValue );
            }
        }

        const string PLAYER_PREFS_KEY = "YarnVariableStorage";

        /// <summary>
        /// Serialize all variables to JSON, then save data to Unity's built-in PlayerPrefs with playerPrefsKey.
        /// </summary>
        public void SaveToPlayerPrefs() {
            var saveData = SerializeAllVariablesToJSON();
            PlayerPrefs.SetString(PLAYER_PREFS_KEY, saveData);
            PlayerPrefs.Save();
            Debug.Log("Variables saved to PlayerPrefs.");
        }

        /// <summary>
        /// Serialize all variables to JSON, then write the data to a file.
        /// </summary>
        public void SaveToFile(string filepath) {
            var saveData = SerializeAllVariablesToJSON();
            System.IO.File.WriteAllText(filepath, saveData, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Load JSON data from Unity's built-in PlayerPrefs with playerPrefsKey, and deserialize as variables.
        /// </summary>
        public void LoadFromPlayerPrefs() {
            if ( PlayerPrefs.HasKey(PLAYER_PREFS_KEY)) {
                var saveData = PlayerPrefs.GetString(PLAYER_PREFS_KEY);
                DeserializeAllVariablesFromJSON(saveData);
                Debug.Log("Variables loaded from PlayerPrefs.");
            } else {
                Debug.LogWarning($"No PlayerPrefs key {PLAYER_PREFS_KEY} found, so no variables loaded.");
            }
        }

        /// <summary>
        /// Load JSON data from a file, then deserialize as variables.
        /// </summary>
        public void LoadFromFile(string filepath) {
            var saveData = System.IO.File.ReadAllText(filepath, System.Text.Encoding.UTF8);
            DeserializeAllVariablesFromJSON(saveData);
        }

        public static readonly Dictionary<System.Type, Yarn.Type> TypeMappings = new Dictionary<System.Type, Yarn.Type>
            {
                { typeof(string), Yarn.Type.String },
                { typeof(bool), Yarn.Type.Bool },
                { typeof(int), Yarn.Type.Number },
                { typeof(float), Yarn.Type.Number },
                { typeof(double), Yarn.Type.Number },
                { typeof(sbyte), Yarn.Type.Number },
                { typeof(byte), Yarn.Type.Number },
                { typeof(short), Yarn.Type.Number },
                { typeof(ushort), Yarn.Type.Number },
                { typeof(uint), Yarn.Type.Number },
                { typeof(long), Yarn.Type.Number },
                { typeof(ulong), Yarn.Type.Number },
                { typeof(decimal), Yarn.Type.Number },
            };
        
        #endregion

        /// <summary>
        /// Returns an <see cref="IEnumerator{T}"/> that iterates over all
        /// variables in this object.
        /// </summary>
        /// <returns>An iterator over the variables.</returns>
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object>>)variables).GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> that iterates over all
        /// variables in this object.
        /// </summary>
        /// <returns>An iterator over the variables.</returns>        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object>>)variables).GetEnumerator();
        }

        
    }
}
