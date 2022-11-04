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
    }

    public enum LocalizationType {
        YarnInternal,
        Unity,
    }
}
