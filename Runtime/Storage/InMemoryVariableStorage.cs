/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

#nullable enable

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
    /// </remarks>
    [HelpURL("https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/variable-storage")]
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
        /// A <see cref="TMPro.TMP_Text"/> that can show the current list
        /// of all variables in-game. Optional.
        /// </summary>
        [SerializeField, Tooltip("(optional) output list of variables and values to Text UI in-game")]
        internal TMP_Text? debugTextView = null;

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
        /// Throws a <see cref="System.ArgumentException"/> if <paramref
        /// name="variableName"/> is not a valid Yarn Spinner variable name.
        /// </summary>
        /// <param name="variableName">The variable name to test.</param>
        /// <exception cref="System.ArgumentException">Thrown when <paramref
        /// name="variableName"/> is not a valid variable name.</exception> 
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

            NotifyVariableChanged(variableName, stringValue);
        }

        public override void SetValue(string variableName, float floatValue)
        {
            ValidateVariableName(variableName);

            variables[variableName] = floatValue;
            variableTypes[variableName] = typeof(float);

            NotifyVariableChanged(variableName, floatValue);
        }

        public override void SetValue(string variableName, bool boolValue)
        {
            ValidateVariableName(variableName);

            variables[variableName] = boolValue;
            variableTypes[variableName] = typeof(bool);

            NotifyVariableChanged(variableName, boolValue);
        }

        private static bool TryGetAsType<T>(Dictionary<string, object> dictionary, string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T result)
        {

            if (dictionary.TryGetValue(key, out var objectResult) == true
                && typeof(T).IsAssignableFrom(objectResult.GetType()))
            {
                result = (T)objectResult;
                return true;
            }

            result = default!;
            return false;
        }

        /// <inheritdoc/>
        public override bool TryGetValue<T>(string variableName, [NotNullWhen(true)] out T result)
        {
            // Ensure that the variable name is valid.
            ValidateVariableName(variableName);

            switch (GetVariableKind(variableName))
            {
                case VariableKind.Stored:
                    // This is a stored value. First, attempt to fetch it from
                    // the variable storage.

                    // Try to get the value from the dictionary, and check to
                    // see that it's the 
                    if (TryGetAsType(variables, variableName, out result))
                    {
                        // We successfully fetched it from storage.
                        return true;
                    }
                    else
                    {
                        if (this.Program is null)
                        {
                            throw new System.InvalidOperationException($"Can't get initial value for variable {variableName}, because {nameof(Program)} is not set");
                        }
                        return this.Program.TryGetInitialValue<T>(variableName, out result);
                    }
                case VariableKind.Smart:
                    // The variable is a smart variable. Find the node that
                    // implements it, and use that to get the variable's current
                    // value.

                    // Update the VM's settings, since ours might have changed
                    // since we created the VM.

                    if (this.SmartVariableEvaluator is null)
                    {
                        throw new System.InvalidOperationException($"Can't get value for smart variable {variableName}, because {nameof(SmartVariableEvaluator)} is not set");
                    }

                    return this.SmartVariableEvaluator.TryGetSmartVariable(variableName, out result);
                case VariableKind.Unknown:
                default:
                    // The variable is not known.
                    result = default!;
                    return false;
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
        /// returns a boolean value representing if the particular variable is
        /// inside the variable storage
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
        public override (Dictionary<string, float>, Dictionary<string, string>, Dictionary<string, bool>) GetAllVariables()
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
