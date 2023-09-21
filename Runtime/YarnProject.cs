using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;

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

        [SerializeField]
        [HideInInspector]
        public LineMetadata lineMetadata;

        [SerializeField]
        [HideInInspector]
        public LocalizationType localizationType;

        /// <summary>
        /// The cached result of deserializing <see
        /// cref="compiledYarnProgram"/>.
        /// </summary>
        private Program cachedProgram = null;

        /// <summary>
        /// The names of assemblies that <see cref="ActionManager"/> should look
        /// for commands and functions in when this project is loaded into a
        /// <see cref="DialogueRunner"/>.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        public List<string> searchAssembliesForActions = new List<string>();

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
        
        /// <summary>
        /// The cached result of reading the default values from the <see
        /// cref="Program"/>.
        /// </summary>
        private Dictionary<string, System.IConvertible> initialValues;
        /// <summary>
        /// The default values of all declared or inferred variables in the
        /// <see cref="Program"/>.
        /// Organised by their name as written in the yarn files.
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

        // ok assumption is that this can be lazy loaded and then kept around
        // as not every node has headers you care about but some will and be read A LOT
        // so we will fill a dict on request and just keep it around
        // is somewhat unnecessary as people can get this out themselves if they want
        // but I think peeps will wanna use headers like a dictionary
        // so we will do the transformation for you
        private Dictionary<string, Dictionary<string, List<string>>>nodeHeaders = new Dictionary<string, Dictionary<string, List<string>>>();
        
        /// <summary>
        /// Gets the headers for the requested node.
        /// </summary>
        /// <remarks>
        /// The first time this is called, the values are extracted from
        /// <see cref="Program"/> and cached inside <see cref="nodeHeaders"/>.
        /// Future calls will then return the cached values.
        /// </remarks>
        public Dictionary<string, List<string>> GetHeaders(string nodeName)
        {
            // if the headers have already been extracted just return that
            Dictionary<string, List<string>> existingValues;
            if (this.nodeHeaders.TryGetValue(nodeName, out existingValues))
            {
                return existingValues;
            }

            // headers haven't been extracted so we look inside the program
            Node rawNode;
            if (!Program.Nodes.TryGetValue(nodeName, out rawNode))
            {
                return new Dictionary<string, List<string>>();
            }

            var rawHeaders = rawNode.Headers;

            // this should NEVER happen
            // because there will always be at least the title, right?
            if (rawHeaders == null || rawHeaders.Count == 0)
            {
                return new Dictionary<string, List<string>>();
            }

            // ok so this is an array of (string, string) tuples
            // with potentially duplicated keys inside the array
            // we'll convert it all into a dict of string arrays
            Dictionary<string, List<string>> headers = new Dictionary<string, List<string>>();
            foreach (var pair in rawHeaders)
            {
                List<string> values;

                if (headers.TryGetValue(pair.Key, out values))
                {
                    values.Add(pair.Value);
                }
                else
                {
                    values = new List<string>();
                    values.Add(pair.Value);
                }
                headers[pair.Key] = values;
            }

            // this.nodeHeaders[nodeName] = headers;

            return headers;
        }

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
        /// Returns a list of all line and option IDs within the requested nodes
        /// </summary>
        /// <remarks>
        /// This is intended to be used either to precache multiple nodes worth of lines or for debugging
        /// </remarks>
        /// <param name="nodes">the names of all nodes whos line IDs you covet</param>
        /// <returns>The ids of all lines and options in the requested <paramref name="nodes"/> </returns>
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
        [System.Obsolete("Use the Program property instead, which caches its return value.")]
        public Program GetProgram()
        {
            return Program.Parser.ParseFrom(compiledYarnProgram);
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
            // We have to invalidate the cache on Awake.
            // Note that this cannot be done through the importer 
            // (e.g., with a setter method that sets compiledYarnProgram and invalidates cachedProgram)
            // because the YarnProject the importer accesses is NOT the same object as the 
            // one currently loaded in the editor. (You can tell by comparing their HashCodes)
            // If there are other sources that can change the value of compiledYarnProgram aside from
            // the importer in runtime, maybe we can add such a method, but until then, this is sufficient.
            cachedProgram = null;
        }
    }

    public enum LocalizationType {
        YarnInternal,
        Unity,
    }
}
