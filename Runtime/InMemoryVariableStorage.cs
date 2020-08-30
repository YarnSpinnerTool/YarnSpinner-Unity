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
        private Dictionary<string, object> variables = new Dictionary<string, object> ();

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
            public string name;

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
        public DefaultVariable[] defaultVariables;

        [Header("Optional debugging tools")]
        
        /// A UI.Text that can show the current list of all variables. Optional.
        [SerializeField] 
        internal UnityEngine.UI.Text debugTextView = null;

        public override void SetValue(string variableName, string stringValue)
        {
            variables[variableName] = stringValue;
        }

        public override void SetValue(string variableName, float floatValue)
        {
            variables[variableName] = floatValue;
        }

        public override void SetValue(string variableName, bool boolValue)
        {
            variables[variableName] = boolValue;
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
            variables.Clear ();
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
