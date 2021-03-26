using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
#endif

namespace Yarn.Unity
{
    public class AudioLineProvider : LineProviderBehaviour
    {

        /// <summary>Specifies the language code to use for audio content
        /// for this <see cref="AudioLineProvider"/>.
        [Language]
        public string audioLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

#if USE_ADDRESSABLES
        // Lines are available if there are no outstanding load operations
        public override bool LinesAvailable => pendingLoadOperations.Count == 0;

        public System.Action<AsyncOperationHandle<AudioClip>> AssetLoadCompleteAction;

        public Dictionary<AsyncOperationHandle<AudioClip>, string> pendingLoadOperations = new Dictionary<AsyncOperationHandle<AudioClip>, string>();

        public Dictionary<string, AsyncOperationHandle<AudioClip>> completedLoadOperations = new Dictionary<string, AsyncOperationHandle<AudioClip>>();

#else
        // Lines are always available because they loaded with the scene
        public override bool LinesAvailable => true;
#endif

        public override LocalizedLine GetLocalizedLine(Line line)
        {
            Localization audioLocalization = YarnProject.GetLocalization(audioLanguage);

            Localization textLocalization;

            // If the audio language is different to the text language,
            // pull the text data from a different localization
            if (audioLanguage != textLanguageCode)
            {
                textLocalization = YarnProject.GetLocalization(textLanguageCode);
            }
            else
            {
                textLocalization = audioLocalization;
            }

            var text = textLocalization.GetLocalizedString(line.ID);

            AudioClip audioClip = null;

            if (audioLocalization.ContainsLocalizedAssets)
            {
                if (audioLocalization.UsesAddressableAssets)
                {
#if USE_ADDRESSABLES
                    var success = completedLoadOperations.TryGetValue(line.ID, out var loadOperation);
                    if (success == false)
                    {
                        // Addressables are available, but we didn't find
                        // the clip in the cache.
                        Debug.LogWarning($"Audio clip for line {line.ID} was requested, but it hadn't finished loading yet.");
                    }
                    else
                    {
                        audioClip = loadOperation.Result;
                    }
#else
                    Debug.LogError($"The Yarn project {YarnProject.name} uses addressable assets, but the Addressable Assets package wasn't found. Either add the package, or if it's already added, add the USE_ADDRESSABLES compiler define.");
#endif
                }
                else
                {
                    // We aren't using addressable assets, so fetch the
                    // asset directly from the localization object.
                    audioClip = audioLocalization.GetLocalizedObject<AudioClip>(line.ID);
                }
            }

            return new AudioLocalizedLine()
            {
                TextID = line.ID,
                RawText = text,
                Substitutions = line.Substitutions,
                AudioClip = audioClip,
            };
        }

        public override void PrepareForLines(IEnumerable<string> lineIDs)
        {
            var audioAddressableLocalization = YarnProject.GetLocalization(audioLanguage);

            if (audioAddressableLocalization.UsesAddressableAssets == false)
            {
                // Nothing further to do here - runtime loading isn't
                // needed.
                return;
            }

#if USE_ADDRESSABLES
            // Otherwise, we need to fetch the assets for these line IDs
            // from the Addressables system.

            if (AssetLoadCompleteAction == null)
            {
                // Cache the completion handler as a one-time operation
                AssetLoadCompleteAction = AssetLoadComplete;
            }

            var linesToLoad = new HashSet<string>(lineIDs);

            // Unload all clips that are not needed
            foreach (var completedLoadID in new List<string>(completedLoadOperations.Keys))
            {
                if (linesToLoad.Contains(completedLoadID) == false)
                {
                    // We no longer need this line. Release it and remove
                    // it from the list of completed operations.
                    Addressables.Release(completedLoadOperations[completedLoadID]);
                    completedLoadOperations.Remove(completedLoadID);
                }
            }

            // Release all pending operations.
            foreach (var element in pendingLoadOperations)
            {
                Addressables.Release(element.Key);
            }

            pendingLoadOperations.Clear();


            // Spin up a request to load each line ID
            foreach (var lineID in lineIDs)
            {
                var assetAddress = Localization.GetAddressForLine(lineID, audioAddressableLocalization.LocaleCode);

                AsyncOperationHandle<AudioClip> task;

                task = Addressables.LoadAssetAsync<AudioClip>(assetAddress);
                task.Completed += AssetLoadCompleteAction;
                pendingLoadOperations.Add(task, lineID);

#if YARNSPINNER_DEBUG
                Debug.Log($"Requesting line {lineID}.");
#endif
            }
#else
            Debug.LogError($"The Yarn project {YarnProject.name} uses addressable assets, but the Addressable Assets package wasn't found. Either add the package, or if it's already added, add the USE_ADDRESSABLES compiler define.");
#endif
        }


#if USE_ADDRESSABLES

        private void AssetLoadComplete(AsyncOperationHandle<AudioClip> operation)
        {
            if (pendingLoadOperations.TryGetValue(operation, out var stringID) == false)
            {
                Debug.LogWarning($"An audio clip load for \"{operation.DebugName}\" completed, but the {nameof(AudioLineProvider)} wasn't expecting it to. Load operation result: {operation.Result}");
                return;
            }

#if YARNSPINNER_DEBUG
            Debug.Log($"Async load for line \"{stringID}\" completed with {operation.Status}");
#endif

            pendingLoadOperations.Remove(operation);

            switch (operation.Status)
            {
                case AsyncOperationStatus.Succeeded:
                    completedLoadOperations.Add(stringID, operation);
                    break;
                case AsyncOperationStatus.Failed:
                    Debug.LogError($"Failed to load asset for line {stringID} in localization \"{YarnProject.GetLocalization(audioLanguage).LocaleCode}\"");
                    break;
                default:
                    // We shouldn't be here?
                    throw new System.InvalidOperationException($"Load operation for {stringID} completed, but its status is {operation.Status}");
            }
        }
#endif

    }

    public class AudioLocalizedLine : LocalizedLine
    {
        /// <summary>
        /// DialogueLine's voice over clip
        /// </summary>
        public AudioClip AudioClip;
    }

}
