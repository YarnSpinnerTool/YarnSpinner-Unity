/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    public partial class DialogueRunner
    {
        /// <summary>
        /// Loads all variables from the requested file in persistent storage
        /// into the Dialogue Runner's variable storage.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method loads the file <paramref name="saveFileName"/> from the
        /// persistent data storage and attempts to read it as JSON. This is
        /// then deserialised and loaded into the <see cref="VariableStorage"/>.
        /// </para>
        /// <para>
        /// The loaded information can be stored via the <see
        /// cref="SaveStateToPersistentStorage"/> method.
        /// </para>
        /// </remarks>
        /// <param name="saveFileName">the name the save file should have on
        /// disc, including any file extension</param>
        /// <returns><see langword="true"/> if the variables were successfully
        /// loaded from the player preferences; <see langword="false"/>
        /// otherwise.</returns>
        public bool LoadStateFromPersistentStorage(string saveFileName)
        {
            if (this.variableStorage == null)
            {
                Debug.LogWarning($"Can't load state from persistent storage: {nameof(variableStorage)} is not set");
                return false;
            }

            var path = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);

            try
            {
                var saveData = System.IO.File.ReadAllText(path);
                var dictionaries = DeserializeAllVariablesFromJSON(saveData);

                this.variableStorage.SetAllVariables(dictionaries.Item1, dictionaries.Item2, dictionaries.Item3);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load save state at {path}: {e.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves all variables from variable storage into the persistent
        /// storage.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method attempts to writes the contents of <see
        /// cref="VariableStorage"/> as a JSON file and saves it to the
        /// persistent data storage under the file name <paramref
        /// name="saveFileName"/>. The saved information can be loaded via the
        /// <see cref="LoadStateFromPersistentStorage"/> method.
        /// </para>
        /// <para>
        /// If <paramref name="saveFileName"/> already exists, it will be
        /// overwritten, not appended.
        /// </para>
        /// </remarks>
        /// <param name="saveFileName">the name the save file should have on
        /// disc, including any file extension</param>
        /// <returns><see langword="true"/> if the variables were successfully
        /// written into the player preferences; <see langword="false"/>
        /// otherwise.</returns>
        public bool SaveStateToPersistentStorage(string saveFileName)
        {
            var data = SerializeAllVariablesToJSON();
            var path = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);

            try
            {
                System.IO.File.WriteAllText(path, data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save state to {path}: {e.Message}");
                return false;
            }
        }

        // takes in a JSON string and converts it into a tuple of dictionaries
        // intended to let you just dump these straight into the variable storage
        // throws exceptions if unable to convert or if the conversion half works
        private (Dictionary<string, float>, Dictionary<string, string>, Dictionary<string, bool>) DeserializeAllVariablesFromJSON(string jsonData)
        {
            SaveData data = JsonUtility.FromJson<SaveData>(jsonData);

            if (data.floatKeys == null || data.floatValues == null)
            {
                throw new ArgumentException("Provided JSON string was not able to extract numeric variables");
            }
            if (data.stringKeys == null || data.stringValues == null)
            {
                throw new ArgumentException("Provided JSON string was not able to extract string variables");
            }
            if (data.boolKeys == null || data.boolValues == null)
            {
                throw new ArgumentException("Provided JSON string was not able to extract boolean variables");
            }

            if (data.floatKeys.Length != data.floatValues.Length)
            {
                throw new ArgumentException("Number of keys and values of numeric variables does not match");
            }
            if (data.stringKeys.Length != data.stringValues.Length)
            {
                throw new ArgumentException("Number of keys and values of string variables does not match");
            }
            if (data.boolKeys.Length != data.boolValues.Length)
            {
                throw new ArgumentException("Number of keys and values of boolean variables does not match");
            }

            var floats = new Dictionary<string, float>();
            for (int i = 0; i < data.floatValues.Length; i++)
            {
                floats.Add(data.floatKeys[i], data.floatValues[i]);
            }
            var strings = new Dictionary<string, string>();
            for (int i = 0; i < data.stringValues.Length; i++)
            {
                strings.Add(data.stringKeys[i], data.stringValues[i]);
            }
            var bools = new Dictionary<string, bool>();
            for (int i = 0; i < data.boolValues.Length; i++)
            {
                bools.Add(data.boolKeys[i], data.boolValues[i]);
            }

            return (floats, strings, bools);
        }
        private string SerializeAllVariablesToJSON()
        {
            if (this.variableStorage == null)
            {
                throw new InvalidOperationException("Can't save variables to JSON: {nameof(variableStorage)} is not set");
            }

            (var floats, var strings, var bools) = this.variableStorage.GetAllVariables();

            SaveData data = new SaveData();
            data.floatKeys = floats.Keys.ToArray();
            data.floatValues = floats.Values.ToArray();
            data.stringKeys = strings.Keys.ToArray();
            data.stringValues = strings.Values.ToArray();
            data.boolKeys = bools.Keys.ToArray();
            data.boolValues = bools.Values.ToArray();

            return JsonUtility.ToJson(data, true);
        }

        [System.Serializable]
        private struct SaveData
        {
            public string[]? floatKeys;
            public float[]? floatValues;
            public string[]? stringKeys;
            public string[]? stringValues;
            public string[]? boolKeys;
            public bool[]? boolValues;
        }

        /// <summary>
        /// Splits input into a number of non-empty sub-strings, separated by
        /// whitespace, and grouping double-quoted strings into a single
        /// sub-string.
        /// </summary>
        /// <param name="input">The string to split.</param>
        /// <returns>A collection of sub-strings.</returns>
        /// <remarks>
        /// This method behaves similarly to the <see cref="string.Split(char[],
        /// StringSplitOptions)"/> method with the <see
        /// cref="StringSplitOptions"/> parameter set to <see
        /// cref="StringSplitOptions.RemoveEmptyEntries"/>, with the following
        /// differences:
        ///
        /// <list type="bullet">
        /// <item>Text that appears inside a pair of double-quote characters
        /// will not be split.</item>
        ///
        /// <item>Text that appears after a double-quote character and before
        /// the end of the input will not be split (that is, an unterminated
        /// double-quoted string will be treated as though it had been
        /// terminated at the end of the input.)</item>
        ///
        /// <item>When inside a pair of double-quote characters, the string
        /// <c>\\</c> will be converted to <c>\</c>, and the string <c>\"</c>
        /// will be converted to <c>"</c>.</item>
        /// </list>
        /// </remarks>
        public static IEnumerable<string> SplitCommandText(string input)
        {
            var reader = new System.IO.StringReader(input.Normalize());

            int c;

            var results = new List<string>();
            var currentComponent = new System.Text.StringBuilder();

            while ((c = reader.Read()) != -1)
            {
                if (char.IsWhiteSpace((char)c))
                {
                    if (currentComponent.Length > 0)
                    {
                        // We've reached the end of a run of visible characters.
                        // Add this run to the result list and prepare for the
                        // next one.
                        results.Add(currentComponent.ToString());
                        currentComponent.Clear();
                    }
                    else
                    {
                        // We encountered a whitespace character, but didn't
                        // have any characters queued up. Skip this character.
                    }

                    continue;
                }
                else if (c == '\"')
                {
                    // We've entered a quoted string!
                    while (true)
                    {
                        c = reader.Read();
                        if (c == -1)
                        {
                            // Oops, we ended the input while parsing a quoted
                            // string! Dump our current word immediately and
                            // return.
                            results.Add(currentComponent.ToString());
                            return results;
                        }
                        else if (c == '\\')
                        {
                            // Possibly an escaped character!
                            var next = reader.Peek();
                            if (next == '\\' || next == '\"')
                            {
                                // It is! Skip the \ and use the character after
                                // it.
                                reader.Read();
                                currentComponent.Append((char)next);
                            }
                            else
                            {
                                // Oops, an invalid escape. Add the \ and
                                // whatever is after it.
                                currentComponent.Append((char)c);
                            }
                        }
                        else if (c == '\"')
                        {
                            // The end of a string!
                            break;
                        }
                        else
                        {
                            // Any other character. Add it to the buffer.
                            currentComponent.Append((char)c);
                        }
                    }

                    results.Add(currentComponent.ToString());
                    currentComponent.Clear();
                }
                else
                {
                    currentComponent.Append((char)c);
                }
            }

            if (currentComponent.Length > 0)
            {
                results.Add(currentComponent.ToString());
            }

            return results;
        }

        public static bool IsInPlaymode
        {
            get
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying)
                {
                    // We are not in playmode at all.
                    return false;
                }

                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    // We are in playmode, but we're about to change out of
                    // playmode.
                    return false;
                }
#endif
                return true;
            }
        }

        public static DialogueRunner? FindRunner(Component component)
        {
            var runner = component.GetComponentInParent<DialogueRunner>();
            if (runner == null)
            {
                runner = FindAnyObjectByType<DialogueRunner>();
            }
            return runner;
        }
    }
}
