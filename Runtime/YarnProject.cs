using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Yarn.Unity
{

    [HelpURL("https://yarnspinner.dev/docs/unity/components/yarn-programs/")]
    public class YarnProject : ScriptableObject
    {

        [SerializeField]
        [HideInInspector]
        public byte[] compiledYarnProgram;

        [SerializeField]
        [HideInInspector]
        public Localization baseLocalization;

        [SerializeField]
        [HideInInspector]
        public List<Localization> localizations = new List<Localization>();

        [System.Serializable]
        class StringDictionary : SerializedDictionary<string, string> { }

        /// <summary>
        /// The metadata contained in each line (if defined).
        /// </summary>
        [SerializeField]
        private StringDictionary _lineMetadata = new StringDictionary();

        /// <summary>
        /// The names of assemblies that <see cref="ActionManager"/> should look
        /// for commands and functions in when this project is loaded into a
        /// <see cref="DialogueRunner"/>.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        public List<string> searchAssembliesForActions = new List<string>();

        public Localization GetLocalization(string localeCode)
        {

            // If localeCode is null, we use the base localization.
            if (localeCode == null)
            {
                return baseLocalization;
            }

            foreach (var loc in localizations)
            {
                if (loc.LocaleCode == localeCode)
                {
                    return loc;
                }
            }

            // We didn't find a localization. Fall back to the Base
            // localization.
            return baseLocalization;
        }

        /// <summary>
        /// Deserializes a compiled Yarn program from the stored bytes in
        /// this object.
        /// </summary>
        public Program GetProgram()
        {
            return Program.Parser.ParseFrom(compiledYarnProgram);
        }

        /// <summary>
        /// Goes through each line from a string table and add any metadata if
        /// they are defined for the line. The metadata is internally stored as
        /// a single string with each piece of metadata separated by a single
        /// whitespace.
        /// </summary>
        /// <param name="stringTableEntries">IEnumerable with entries from a string table.</param>
        internal void AddLineMetadata(IEnumerable<StringTableEntry> stringTableEntries)
        {
            foreach (var entry in stringTableEntries)
            {
                if (entry.Metadata.Length == 0)
                {
                    continue;
                }

                _lineMetadata.Add(entry.ID, System.String.Join(" ", entry.Metadata));
            }
        }

        /// <summary>
        /// Returns metadata for a given line ID, if any is defined.
        /// </summary>
        /// <param name="lineID">The line ID.</param>
        /// <returns>An array of each piece of metadata if defined, otherwise returns null.</returns>
        public string[] GetLineMetadata(string lineID)
        {
            if (_lineMetadata.TryGetValue(lineID, out var result))
            {
                return result.Split(' ');
            }

            return null;
        }
    }
}
