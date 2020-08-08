using System.Collections.Generic;

using UnityEngine;
#if ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Yarn.Unity
{
    public class AudioLineProvider : LineProviderBehaviour
    {
        private static string CurrentAudioLanguageCode => Preferences.AudioLanguage;

        public override LocalizedLine GetLocalizedLine(Line line)
        {
            if (string.IsNullOrWhiteSpace(CurrentAudioLanguageCode)) {
                throw new System.InvalidOperationException($"Can't get audio for line {line.ID}: {nameof(CurrentAudioLanguageCode)} is not set");                
            }

            Localization audioLocalization = localizationDatabase.GetLocalization(CurrentAudioLanguageCode);

            Localization textLocalization;

            // If the audio language is different to the text language,
            // pull the text data from a different localization
            if (CurrentAudioLanguageCode != CurrentTextLanguageCode) {
                textLocalization = localizationDatabase.GetLocalization(CurrentTextLanguageCode);
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

}
