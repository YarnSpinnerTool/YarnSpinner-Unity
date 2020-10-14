using System.Collections.Generic;
using UnityEngine;

#if ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Yarn.Unity
{
    public class AddressableAudioLineProvider : LineProviderBehaviour
    {   
#if !ADDRESSABLES
        const string NotCompatibleMessage = nameof(AddressableAudioLineProvider) + " won't work in this project, because the Addressable Assets package is not installed. Either import the package (see https://docs.unity3d.com/Packages/com.unity.addressables@1.16/), or use " + nameof(AudioLineProvider) + " instead.";
        public override LocalizedLine GetLocalizedLine(Line line) {
            throw new System.InvalidOperationException(NotCompatibleMessage);
        }
        public override void PrepareForLines(IEnumerable<string> lineIDs) {
            throw new System.InvalidOperationException(NotCompatibleMessage);
        }
        public override bool LinesAvailable => throw new System.InvalidOperationException(NotCompatibleMessage);
#else

        // Lines are available if there are no outstanding load operations
        public override bool LinesAvailable => activeLoadOperationsToLineIDs.Count == 0;

        public System.Action<AsyncOperationHandle<AudioClip>> AssetLoadCompleteAction;

        // Maps load operations that are currently outstanding to the line
        // ID that they were invoked for. We do it this way because the
        // completion handler returns the operation, and we want to cache
        // the loaded AudioClip once it comes back, and for that to be
        // useful we need the line ID.
        private Dictionary<AsyncOperationHandle<AudioClip>, string> activeLoadOperationsToLineIDs = new Dictionary<AsyncOperationHandle<AudioClip>, string>();

        // The cache of AudioClips that we've loaded. Used by
        // GetLocalizedLine, cleared when PrepareForLines is called.
        private Dictionary<string, AudioClip> cachedAudioClips = new Dictionary<string, AudioClip>();

        public override LocalizedLine GetLocalizedLine(Line line)
        {
            if (string.IsNullOrWhiteSpace(CurrentAudioLanguageCode)) {
                throw new System.InvalidOperationException($"Can't get audio for line {line.ID}: {nameof(CurrentAudioLanguageCode)} is not set");                
            }
            
            Localization localization = localizationDatabase.GetLocalization(CurrentAudioLanguageCode);

            var text = localization.GetLocalizedString(line.ID);

            cachedAudioClips.TryGetValue(line.ID, out var audioClip);

            return new AudioLocalizedLine
            {
                TextID = line.ID,
                RawText = text,
                Substitutions = line.Substitutions,
                AudioClip = audioClip,
            };
        }

        private static string CurrentAudioLanguageCode => Preferences.AudioLanguage;

        public override void PrepareForLines(IEnumerable<string> lineIDs)
        {
            if (string.IsNullOrWhiteSpace(CurrentAudioLanguageCode)) {
                throw new System.InvalidOperationException($"Can't get audio for lines {string.Join(", ", lineIDs)}: {nameof(CurrentAudioLanguageCode)} is not set");                
            }            

            if (AssetLoadCompleteAction == null) {
                // Cache the completion handler as a one-time operation
                AssetLoadCompleteAction = AssetLoadComplete;
            }

            // TODO: only remove items from cachedAudioClips that aren't in
            // lineIDs, don't clear the whole thing
            cachedAudioClips.Clear();

            var audioAddressableLocalization = localizationDatabase.GetLocalization(CurrentAudioLanguageCode);

            // Spin up a request to load each line ID
            foreach (var lineID in lineIDs)
            {
                var assetReference = audioAddressableLocalization.GetLocalizedObjectAddress(lineID);

                if (assetReference == null)
                {
                    // No localized object address was found for this line
                    // ID. A localized object won't be available.
                    continue;
                }

                var task = assetReference.LoadAssetAsync<AudioClip>();
                task.Completed += AssetLoadCompleteAction;
                activeLoadOperationsToLineIDs.Add(task, lineID);
            }
        }

        private void AssetLoadComplete(AsyncOperationHandle<AudioClip> operation)
        {
            if (activeLoadOperationsToLineIDs.TryGetValue(operation, out var stringID) == false) {
                Debug.LogWarning($"An audio clip load completed, but the {nameof(AddressableAudioLineProvider)} wasn't expecting it to. Load operation result: {operation.Result}");
                return;
            }

            activeLoadOperationsToLineIDs.Remove(operation);

            if (operation.Result != null) {
                cachedAudioClips.Add(stringID, operation.Result);
            } else {
                Debug.LogWarning($"Failed to load audio clip for {stringID}");
            }
        }
#endif
    }
}
