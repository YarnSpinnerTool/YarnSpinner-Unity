using System.Collections.Generic;

using UnityEngine;
#if ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Yarn.Unity
{
    public class AudioLineProvider : LineProviderBehaviour
    {
        private string CurrentAudioLanguageCode 
        { 
            get 
            { 
                return string.IsNullOrWhiteSpace(audioLanguageCodeOverride) ? Preferences.AudioLanguage : audioLanguageCodeOverride;
            }
        }

        /// <summary>Specifies the language code to use for audio content
        /// for this <see cref="AudioLineProvider"/>, overriding project
        /// settings.</summary>
        /// <remarks>
        /// If defined, this Line Provider will ignore the current setting
        /// in Preferences.AudioLanguage and use the audio language code
        /// override instead (e.g. "en" is the code for "English")
        /// </remarks>
        [Tooltip("(optional) if defined, this Line Provider will use this language code instead of Preferences.AudioLanguage... example: 'en' is the code for English")]
        public string audioLanguageCodeOverride;

        public override void Start () {
            base.Start();

            if ( !string.IsNullOrWhiteSpace(audioLanguageCodeOverride) ) {
                Debug.LogWarning($"LineProvider is ignoring global Preferences.AudioLanguage and using audioLanguageCodeOverride: {audioLanguageCodeOverride}");
            }
        }

        public override LocalizedLine GetLocalizedLine(Line line)
        {
            if (string.IsNullOrWhiteSpace(CurrentAudioLanguageCode)) {
                throw new System.InvalidOperationException($"Can't get audio for line {line.ID}: {nameof(CurrentAudioLanguageCode)} is not set");                
            }

            Localization audioLocalization = YarnProject.GetLocalization(CurrentAudioLanguageCode);

            Localization textLocalization;

            // If the audio language is different to the text language,
            // pull the text data from a different localization
            if (CurrentAudioLanguageCode != CurrentTextLanguageCode) {
                textLocalization = YarnProject.GetLocalization(CurrentTextLanguageCode);
            } else {
                textLocalization = audioLocalization;
            }

            var text = textLocalization.GetLocalizedString(line.ID);
            var audioClip = audioLocalization.GetLocalizedObject<AudioClip>(line.ID);
            
            return new AudioLocalizedLine() {
                TextID = line.ID,
                RawText = text,
                Substitutions = line.Substitutions,
                AudioClip = audioClip,                
            };
        }

        public override void PrepareForLines(IEnumerable<string> lineIDs)
        {
            // no-op; audio references are always available (they were
            // loaded with the scene)
        }

        public override bool LinesAvailable => true;
    }

    public class AudioLocalizedLine : LocalizedLine {
        /// <summary>
        /// DialogueLine's voice over clip
        /// </summary>
        public AudioClip AudioClip;
    }

}
