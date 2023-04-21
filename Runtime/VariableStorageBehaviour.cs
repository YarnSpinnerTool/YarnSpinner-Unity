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
