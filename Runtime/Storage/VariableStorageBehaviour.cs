/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using BoolDictionary = System.Collections.Generic.Dictionary<string, bool>;
using FloatDictionary = System.Collections.Generic.Dictionary<string, float>;
using StringDictionary = System.Collections.Generic.Dictionary<string, string>;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> that a <see cref="DialogueRunner"/> uses
    /// to store and retrieve variables.
    /// </summary>
    /// <remarks>
    /// This abstract class inherits from <see cref="MonoBehaviour"/>, which
    /// means that subclasses of this class can be attached to <see
    /// cref="GameObject"/>s.
    /// </remarks>
    public abstract class VariableStorageBehaviour : MonoBehaviour, Yarn.IVariableStorage
    {
        public Program? Program { get; set; }

        public ISmartVariableEvaluator? SmartVariableEvaluator { get; set; }

        /// <inheritdoc/>
        public abstract bool TryGetValue<T>(string variableName, [NotNullWhen(true)] out T? result);

        /// <inheritdoc/>
        public abstract void SetValue(string variableName, string stringValue);

        /// <inheritdoc/>
        public abstract void SetValue(string variableName, float floatValue);

        /// <inheritdoc/>
        public abstract void SetValue(string variableName, bool boolValue);

        /// <summary>
        /// Notifies all currently registered change listeners that the variable
        /// named <paramref name="variableName"/> has changed value.
        /// </summary>
        /// <typeparam name="T">The type of the variable.</typeparam>
        /// <param name="variableName">The name of the variable that changed
        /// value.</param>
        /// <param name="newValue">The new value of the variable.</param>
        protected void NotifyVariableChanged<T>(string variableName, T newValue)
        {
            if (changeListeners.TryGetValue(variableName, out var delegates))
            {
                foreach (var listener in delegates)
                {
                    listener.DynamicInvoke(newValue);
                }
            }

            foreach (var listener in globalChangeListeners)
            {
                listener.DynamicInvoke(variableName, newValue);
            }
        }

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
        /// <param name="clear">Should the load also wipe the storage. Defaults
        /// to true so all existing variables will be cleared.
        /// </param>
        public abstract void SetAllVariables(FloatDictionary floats, StringDictionary strings, BoolDictionary bools, bool clear = true);

        /// <summary>
        /// Provides a unified interface for exporting all variables. Intended
        /// to be a point for custom saving, editors, etc.
        /// </summary>
        public abstract (FloatDictionary FloatVariables, StringDictionary StringVariables, BoolDictionary BoolVariables) GetAllVariables();

        public VariableKind GetVariableKind(string name)
        {
            if (this.Contains(name))
            {
                return VariableKind.Stored;
            }
            else if (this.Program != null)
            {
                return Program.GetVariableKind(name);
            }
            else
            {
                Debug.Log($"Unable to determine kind of variable {name}: it is not stored in this variable storage, and {nameof(Program)} is not set");
                return VariableKind.Unknown;
            }
        }

        private struct ChangeListenerDisposable : IDisposable
        {

            private Delegate listener;

            private readonly List<Delegate> listeners;

            public ChangeListenerDisposable(List<Delegate> listeners, Delegate listener)
            {
                this.listeners = listeners;
                this.listener = listener;
            }

            public readonly void Dispose()
            {
                listeners.Remove(listener);
            }
        }

        private Dictionary<string, List<Delegate>> changeListeners = new();
        private List<Delegate> globalChangeListeners = new();

        /// <summary>
        /// Registers a delegate that will be called when the variable <paramref
        /// name="variableName"/> is modified. 
        /// </summary>
        /// <typeparam name="T">The type of the variable.</typeparam>
        /// <param name="variableName">The name of the variable to watch for
        /// changes to. This variable must be of type <typeparamref name="T"/>,
        /// and it must not be a smart variable.</param>
        /// <param name="onChange">The delegate to run when the variable changes
        /// value.</param>
        /// <returns>An <see cref="IDisposable"/> that removes the registration
        /// when its <see cref="IDisposable.Dispose"/> method is
        /// called.</returns>
        /// <exception cref="InvalidOperationException">Called when <see
        /// cref="Program"/> is not set.</exception>
        /// <exception cref="ArgumentException">Called when <paramref
        /// name="variableName"/> is not the name of a valid variable, or if
        /// <typeparamref name="T"/> does not match the type of the
        /// variable.</exception>
        public IDisposable AddChangeListener<T>(string variableName, Action<T> onChange)
        {
            if (Program == null)
            {
                throw new InvalidOperationException($"Can't add a change listener for {variableName}: {nameof(Program)} is not set");
            }

            var kind = this.GetVariableKind(variableName);
            if (kind == VariableKind.Smart)
            {
                throw new ArgumentException($"Can't add a change listener for {variableName}: change listeners cannot be added for {VariableKind.Smart} variables.");
            }

            if (changeListeners.TryGetValue(variableName, out var list) == false)
            {
                list = new List<Delegate>();
                changeListeners[variableName] = list;
            }

            if (kind == VariableKind.Stored)
            {
                var type = this.Program.InitialValues[variableName].ValueCase;

                if (type == Operand.ValueOneofCase.BoolValue && typeof(T) != typeof(bool))
                {
                    throw new ArgumentException($"Can't add a {typeof(T)} change listener for {variableName}: must be a {typeof(bool)}");
                }
                if (type == Operand.ValueOneofCase.StringValue && typeof(T) != typeof(string))
                {
                    throw new ArgumentException($"Can't add a {typeof(T)} change listener for {variableName}: must be a {typeof(string)}");
                }
                if (type == Operand.ValueOneofCase.FloatValue && typeof(float).IsAssignableFrom(typeof(T)) == false)
                {
                    throw new ArgumentException($"Can't add a {typeof(T)} change listener for {variableName}: must be a number");
                }
            }

            list.Add(onChange);

            return new ChangeListenerDisposable(list, onChange);
        }


        /// <summary>
        /// Registers a delegate that will be called when any variable is modified. 
        /// </summary>
        /// <param name="onChange">The delegate to run when the variable changes
        /// value.</param>
        /// <returns>An <see cref="IDisposable"/> that removes the registration
        /// when its <see cref="IDisposable.Dispose"/> method is
        /// called.</returns>
        public IDisposable AddChangeListener(System.Action<string, object> onChange)
        {
            globalChangeListeners ??= new();

            globalChangeListeners.Add(onChange);

            return new ChangeListenerDisposable(globalChangeListeners, onChange);
        }
    }
}
