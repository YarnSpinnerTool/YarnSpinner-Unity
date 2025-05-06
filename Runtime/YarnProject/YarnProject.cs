/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    [HelpURL("https://docs.yarnspinner.dev/using-yarnspinner-with-unity/importing-yarn-files/yarn-projects")]
    public sealed class YarnProject : ScriptableObject
    {
        [HideInInspector]
        public byte[]? compiledYarnProgram;

        [HideInInspector]
        [NotNull]
#pragma warning disable CS8618
        public Localization baseLocalization;
#pragma warning restore CS8618

        [SerializeField]
        [HideInInspector]
        public SerializableDictionary<string, Localization> localizations = new SerializableDictionary<string, Localization>();

        public LineMetadata? lineMetadata;

        [HideInInspector]
        public LocalizationType localizationType;

        /// <summary>
        /// The cached result of deserializing <see
        /// cref="compiledYarnProgram"/>.
        /// </summary>
        private Program? cachedProgram = null;

        /// <summary>
        /// The names of all nodes contained within the <see cref="Program"/>.
        /// </summary>
        public string[] NodeNames
        {
            get
            {
                return Program.Nodes.Keys.ToArray();
            }
        }

        [System.Serializable]
        public struct ShadowTableEntry
        {
            public string sourceLineID;
            public string[] shadowMetadata;
        }

        [System.Serializable]
        public class ShadowTableDictionary : SerializableDictionary<string, ShadowTableEntry> { }

        /// <summary>
        /// The cached result of reading the default values from the <see
        /// cref="Program"/>.
        /// </summary>
        private Dictionary<string, System.IConvertible>? initialValues;

        /// <summary>
        /// The default values of all declared or inferred variables in the <see
        /// cref="Program"/>. Organised by their name as written in the yarn
        /// files.
        /// </summary>
        public Dictionary<string, System.IConvertible> InitialValues
        {
            get
            {
                if (initialValues != null)
                {
                    return initialValues;
                }

                initialValues = new Dictionary<string, System.IConvertible>();

                foreach (var pair in Program.InitialValues)
                {
                    var value = pair.Value;
                    switch (value.ValueCase)
                    {
                        case Yarn.Operand.ValueOneofCase.StringValue:
                            {
                                initialValues[pair.Key] = value.StringValue;
                                break;
                            }
                        case Yarn.Operand.ValueOneofCase.BoolValue:
                            {
                                initialValues[pair.Key] = value.BoolValue;
                                break;
                            }
                        case Yarn.Operand.ValueOneofCase.FloatValue:
                            {
                                initialValues[pair.Key] = value.FloatValue;
                                break;
                            }
                        default:
                            {
                                Debug.LogWarning($"{pair.Key} is of an invalid type: {value.ValueCase}");
                                break;
                            }
                    }
                }
                return initialValues;
            }
        }

        // ok assumption is that this can be lazy loaded and then kept around as
        // not every node has headers you care about but some will and be read A
        // LOT so we will fill a dict on request and just keep it around is
        // somewhat unnecessary as people can get this out themselves if they
        // want but I think peeps will wanna use headers like a dictionary so we
        // will do the transformation for you
        private Dictionary<string, Dictionary<string, List<string>>> nodeHeaders = new Dictionary<string, Dictionary<string, List<string>>>();

        /// <summary>
        /// Gets the headers for the requested node.
        /// </summary>
        /// <remarks>
        /// The first time this is called, the values are extracted from <see
        /// cref="Program"/> and cached inside <see cref="nodeHeaders"/>. Future
        /// calls will then return the cached values.
        /// </remarks>
        [System.Obsolete("Use " + nameof(Dialogue) + "." + nameof(Dialogue.GetHeaders), true)]
        public Dictionary<string, List<string>> GetHeaders(string nodeName)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Gets a Localization given a locale code.
        /// </summary>
        /// <param name="localeCode">The locale code to find a <see
        /// cref="Localization"/> for.</param>
        /// <returns>The Localization if one is found for the locale <paramref
        /// name="localeCode"/>; <see cref="baseLocalization"/>
        /// otherwise.</returns>
        public Localization GetLocalization(string localeCode)
        {
            // If localeCode is null, we use the base localization.
            if (localeCode == null)
            {
                return baseLocalization;
            }

            if (localizations.TryGetValue(localeCode, out var result))
            {
                return result;
            }

            // We didn't find a localization. Fall back to the Base
            // localization.
            return baseLocalization;
        }

        /// <summary>
        /// Returns a list of all line and option IDs within the requested nodes
        /// </summary>
        /// <remarks>
        /// This is intended to be used either to precache multiple nodes worth
        /// of lines or for debugging
        /// </remarks>
        /// <param name="nodes">the names of all nodes whos line IDs you
        /// covet</param>
        /// <returns>The ids of all lines and options in the requested <paramref
        /// name="nodes"/> </returns>
        public IEnumerable<string> GetLineIDsForNodes(IEnumerable<string> nodes)
        {
            var ids = new List<string>();

            foreach (var node in nodes)
            {
                var lines = Program.LineIDsForNode(node);
                if (lines != null)
                {
                    ids.AddRange(lines);
                }
            }

            return ids;
        }

        /// <summary>
        /// Gets the Yarn Program stored in this project.
        /// </summary>
        /// <remarks>
        /// The first time this is called, the program stored in <see
        /// cref="compiledYarnProgram"/> is deserialized and cached. Future
        /// calls to this method will return the cached value.
        /// </remarks>
        public Program Program
        {
            get
            {
                if (cachedProgram == null)
                {
                    cachedProgram = Program.Parser.ParseFrom(compiledYarnProgram);
                }
                return cachedProgram;
            }
        }
        private void Awake()
        {
            // We have to invalidate the cache on Awake. Note that this cannot
            // be done through the importer (e.g., with a setter method that
            // sets compiledYarnProgram and invalidates cachedProgram) because
            // the YarnProject the importer accesses is NOT the same object as
            // the one currently loaded in the editor. (You can tell by
            // comparing their HashCodes) If there are other sources that can
            // change the value of compiledYarnProgram aside from the importer
            // in runtime, maybe we can add such a method, but until then, this
            // is sufficient.
            cachedProgram = null;
        }
    }

    public enum LocalizationType
    {
        YarnInternal,
        Unity,
    }
}
