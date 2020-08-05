using CsvHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
#if ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif 
using Yarn;

namespace Yarn.Unity
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> created from a yarn file that stores
    /// the compiled Yarn program, all lines of text with their associated IDs,
    /// translations and voice over <see cref="AudioClip"/>s.
    /// </summary>
    public class YarnProgram : ScriptableObject
    {
        /// <summary>
        /// The compiled Yarn program as byte code.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        public byte[] compiledProgram;

        /// <summary>
        /// The localization database that this script provides information
        /// to.
        /// </summary>
        public LocalizationDatabase localizationDatabase;

        /// <summary>
        /// The TextAsset containing the string table for this script's
        /// base localization.
        /// </summary>
        public TextAsset baseLocalisationStringTable => GetStringTableAsset(this.baseLocalizationId);

        /// <summary>
        /// The language ID (e.g. "en" or "de") of the base language (the
        /// language the Yarn file is written in).
        /// </summary>
        [SerializeField]
        public string baseLocalizationId;

        /// <summary>
        /// Maps a language ID to a TextAsset.
        /// </summary>
        [Serializable]
        public class YarnTranslation
        {
            public YarnTranslation(string LanguageName, TextAsset Text = null) {
                languageName = LanguageName;
                text = Text;
            }

            /// <summary>
            /// Name of the language of this <see cref="YarnTranslation"/> in RFC
            /// 4646.
            /// </summary>
            public string languageName;

            /// <summary>
            /// The csv string table containing the translated text.
            /// </summary>
            public TextAsset text;
        }

        /// <summary>
        /// Available localizations of this <see cref="YarnProgram"/>.
        /// </summary>
        [SerializeField]
        //[HideInInspector]
        public YarnTranslation[] localizations = new YarnTranslation[0];

        /// <summary>
        /// Deserializes a compiled Yarn program from the stored bytes in this
        /// object.
        /// </summary>
        /// <returns></returns>
        public Program GetProgram()
        {
            return Program.Parser.ParseFrom(compiledProgram);
        }

        /// <summary>
        /// Returns the <see cref="TextAsset"/> containing localized
        /// strings for the language indicated by <paramref
        /// name="languageCode"/>.
        /// </summary>
        /// <param name="languageCode">The language code to get the string
        /// table asset for. </param>
        /// <returns>The string table <see cref="TextAsset"/>.</returns>
        public TextAsset  GetStringTableAsset(string languageCode) {
            foreach (var localisation in this.localizations) {
                if (localisation.languageName == languageCode) {
                    return localisation.text;
                }
            }
            return null;
        }
    }
}
