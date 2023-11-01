/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Yarn.Unity;

namespace Yarn.Unity
{
    /// <summary>
    /// A simple implementation of VariableStorageBehaviour.
    /// </summary>
    /// <remarks>
    /// <para>This class stores variables in memory, and is erased when the game
    /// exits.</para>
    ///
    /// <para>This class also has basic serialization and save/load example functions.</para>
    ///
    /// <para>You can also enumerate over the variables by using a <c>foreach</c>
    /// loop:</para>
    ///
    /// <code lang="csharp">
    /// // 'storage' is an InMemoryVariableStorage
    /// foreach (var variable in storage) {
    ///     string name = variable.Key;
    ///     System.Object value = variable.Value;
    /// }
    /// </code>
    ///
    /// <para>Note that as of v2.0, this class no longer uses Yarn.Value, to
    /// enforce static typing of declared variables within the Yarn
    /// Program.</para>
    /// </remarks>
    [HelpURL("https://yarnspinner.dev/docs/unity/components/variable-storage/")]
    public class InMemoryVariableStorage : VariableStorageBehaviour, IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// Where we're actually keeping our variables
        /// </summary>
        private Dictionary<string, object> variables = new Dictionary<string, object>();
        private Dictionary<string, System.Type> variableTypes = new Dictionary<string, System.Type>(); // needed for serialization

        [Header("Optional debugging tools")]
        [HideInInspector] public bool showDebug;

        /// <summary>
        /// A <see cref="UnityEngine.UI.Text"/> that can show the current list
        /// of all variables in-game. Optional.
        /// </summary>
        [SerializeField, Tooltip("(optional) output list of variables and values to Text UI in-game")]
        internal UnityEngine.UI.Text debugTextView = null;

        internal void Update()
        {
            // If we have a debug view, show the list of all variables in it
            if (debugTextView != null)
            {
                debugTextView.text = GetDebugList();
                debugTextView.SetAllDirty();
            }
        }

        public string GetDebugList()
        {
            var stringBuilder = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, object> item in variables)
            {
                stringBuilder.AppendLine(string.Format("{0} = {1} ({2})",
                                                        item.Key,
                                                        item.Value.ToString(),
                                                        variableTypes[item.Key].ToString().Substring("System.".Length)));
            }
            return stringBuilder.ToString();
        }


        #region Setters

        /// <summary>
        /// Used internally by serialization functions to wrap around the
        /// SetValue() methods.
        /// </summary>
        void SetVariable(string name, Yarn.IType type, string value)
        {
            if (type == Yarn.BuiltinTypes.Boolean)
            {
                bool newBool;
                if (bool.TryParse(value, out newBool))
                {
                    SetValue(name, newBool);
                }
                else
                {
                    throw new System.InvalidCastException($"Couldn't initialize default variable {name} with value {value} as Bool");
                }
            }
            else if (type == Yarn.BuiltinTypes.Number)
            {
                float newNumber;
                if (float.TryParse(value, out newNumber))
                { // TODO: this won't work for different cultures (e.g. French write "0.53" as "0,53")
                    SetValue(name, newNumber);
                }
                else
                {
                    throw new System.InvalidCastException($"Couldn't initialize default variable {name} with value {value} as Number (Float)");
                }
            }
            else if (type == Yarn.BuiltinTypes.String)
            {
                SetValue(name, value); // no special type conversion required
            }
            else
            {
                throw new System.ArgumentOutOfRangeException($"Unsupported type {type.Name}");
            }
        }

        /// <summary>
        /// Throws a <see cref="System.ArgumentException"/> if <paramref
        /// name="variableName"/> is not a valid Yarn Spinner variable
        /// name.
        /// </summary>
        /// <param name="variableName">The variable name to test.</param>
        /// <exception cref="System.ArgumentException">Thrown when
        /// <paramref name="variableName"/> is not a valid variable
        /// name.</exception> 
        private void ValidateVariableName(string variableName)
        {
            if (variableName.StartsWith("$") == false)
            {
                throw new System.ArgumentException($"{variableName} is not a valid variable name: Variable names must start with a '$'. (Did you mean to use '${variableName}'?)");
            }
        }

        public override void SetValue(string variableName, string stringValue)
        {
            ValidateVariableName(variableName);

            variables[variableName] = stringValue;
            variableTypes[variableName] = typeof(string);
        }

        public override void SetValue(string variableName, float floatValue)
        {
            ValidateVariableName(variableName);
            
            variables[variableName] = floatValue;
            variableTypes[variableName] = typeof(float);
        }

        public override void SetValue(string variableName, bool boolValue)
        {
            ValidateVariableName(variableName);
            
            variables[variableName] = boolValue;
            variableTypes[variableName] = typeof(bool);
        }

        /// <summary>
        /// Retrieves a <see cref="Value"/> by name.
        /// </summary>
        /// <param name="variableName">The name of the variable to retrieve
        /// the value of. Don't forget to include the "$" at the
        /// beginning!</param>
        /// <returns>The <see cref="Value"/>. If a variable by the name of
        /// <paramref name="variableName"/> is not present, returns a value
        /// representing `null`.</returns>
        /// <exception cref="System.ArgumentException">Thrown when
        /// variableName is not a valid variable name.</exception>
        public override bool TryGetValue<T>(string variableName, out T result)
        {
            ValidateVariableName(variableName);

            // If we don't have a variable with this name, return the null
            // value
            if (variables.ContainsKey(variableName) == false)
            {
                result = default;
                return false;
            }

            var resultObject = variables[variableName];

            if (typeof(T).IsAssignableFrom(resultObject.GetType()))
            {
                result = (T)resultObject;
                return true;
            }
            else
            {
                throw new System.InvalidCastException($"Variable {variableName} exists, but is the wrong type (expected {typeof(T)}, got {resultObject.GetType()}");
            }
        }

        /// <summary>
        /// Removes all variables from storage.
        /// </summary>
        public override void Clear()
        {
            variables.Clear();
            variableTypes.Clear();
        }

        #endregion

        /// <summary>
        /// returns a boolean value representing if the particular variable is inside the variable storage
        /// </summary>
        public override bool Contains(string variableName)
        {
            return variables.ContainsKey(variableName);
        }

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

        #region Save/Load
        public override (Dictionary<string,float>,Dictionary<string,string>,Dictionary<string,bool>) GetAllVariables()
        {
            Dictionary<string, float> floatDict = new Dictionary<string, float>();
            Dictionary<string, string> stringDict = new Dictionary<string, string>();
            Dictionary<string, bool> boolDict = new Dictionary<string, bool>();
            
            foreach (var variable in variables)
            {
                var type = variableTypes[variable.Key];

                if (type == typeof(float))
                {
                    float value = System.Convert.ToSingle(variable.Value);
                    floatDict.Add(variable.Key, value);
                }
                else if (type == typeof(string))
                {
                    string value = System.Convert.ToString(variable.Value);
                    stringDict.Add(variable.Key, value);
                }
                else if (type == typeof(bool))
                {
                    bool value = System.Convert.ToBoolean(variable.Value);
                    boolDict.Add(variable.Key, value);
                }
                else
                {
                    Debug.Log($"{variable.Key} is not a valid type");
                }
            }

            return (floatDict, stringDict, boolDict);
        }

        public override void SetAllVariables(Dictionary<string, float> floats, Dictionary<string, string> strings, Dictionary<string, bool> bools, bool clear = true)
        {
            if (clear)
            {
                variables.Clear();
                variableTypes.Clear();
            }

            foreach (var value in floats)
            {
                SetValue(value.Key, value.Value);
            }
            foreach (var value in strings)
            {
                SetValue(value.Key, value.Value);
            }
            foreach (var value in bools)
            {
                SetValue(value.Key, value.Value);
            }

            Debug.Log($"bulk loaded {floats.Count} floats, {strings.Count} strings, {bools.Count} bools");
        }
        #endregion
    }
}
