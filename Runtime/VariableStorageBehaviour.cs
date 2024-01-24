/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;

using FloatDictionary = System.Collections.Generic.Dictionary<string, float>;
using StringDictionary = System.Collections.Generic.Dictionary<string, string>;
using BoolDictionary = System.Collections.Generic.Dictionary<string, bool>;

namespace Yarn.Unity
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> that a <see cref="DialogueRunner"/>
    /// uses to store and retrieve variables.
    /// </summary>
    /// <remarks>
    /// This abstract class inherits from <see cref="MonoBehaviour"/>,
    /// which means that subclasses of this class can be attached to <see
    /// cref="GameObject"/>s.
    /// </remarks>
    public abstract class VariableStorageBehaviour : MonoBehaviour, Yarn.IVariableStorage
    {
        /// <inheritdoc/>
        public abstract bool TryGetValue<T>(string variableName, out T result);

        /// <inheritdoc/>
        public abstract void SetValue(string variableName, string stringValue);

        /// <inheritdoc/>
        public abstract void SetValue(string variableName, float floatValue);

        /// <inheritdoc/>
        public abstract void SetValue(string variableName, bool boolValue);

        /// <inheritdoc/>
        public abstract void Clear();

        /// <summary>
        /// Returns a boolean value representing if a particular variable is
        /// inside the variable storage.
        /// </summary>
        /// <param name="variableName">The name of the variable to check
        /// for.</param>
        /// <returns><see langword="true"/> if this variable storage contains a
        /// value for the variable named <paramref name="variableName"/>; <see
        /// langword="false"/> otherwise.</returns>
        public abstract bool Contains(string variableName);

        /// <summary>
        /// Provides a unified interface for loading many variables all at once.
        /// Will override anything already in the variable storage.
        /// </summary>
        /// <param name="clear">Should the load also wipe the storage.
        /// Defaults to true so all existing variables will be cleared.
        /// </param>
        public abstract void SetAllVariables(FloatDictionary floats, StringDictionary strings, BoolDictionary bools, bool clear = true);

        /// <summary>
        /// Provides a unified interface for exporting all variables.
        /// Intended to be a point for custom saving, editors, etc.
        /// </summary>
        public abstract (FloatDictionary FloatVariables, StringDictionary StringVariables, BoolDictionary BoolVariables) GetAllVariables();
    }
}
