/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

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
#region AudioLineProviderInternalObjects
        private interface IAssetLineProvider
        {
            LocalizedLine GetLocalizedLine(Yarn.Line line);
            void PrepareForLines(IEnumerable<string> lineIDs);
            bool LinesAvailable { get; }
            AudioLineProvider audioLineProvider { get; set; }
        }

        private class NullProvider: IAssetLineProvider
        {
            public LocalizedLine GetLocalizedLine(Yarn.Line line)
            {
                Debug.LogWarning($"asked for line {line.ID} but this provider has no project.");
                return null;
            }
            public void PrepareForLines(IEnumerable<string> lineIDs) { }
            public bool LinesAvailable { get { return false; } }
            public AudioLineProvider audioLineProvider { get; set; }
        }

        private class DirectReferenceAudioLineProvider: IAssetLineProvider
        {
            public AudioLineProvider audioLineProvider { get; set; }

            // Lines are always available because they loaded with the scene
            public bool LinesAvailable => true;

            public LocalizedLine GetLocalizedLine(Line line)
            {
                Localization audioLocalization = audioLineProvider.YarnProject.GetLocalization(audioLineProvider.audioLanguageCode);

                Localization textLocalization;

                // If the audio language is different to the text language,
                // pull the text data from a different localization
                if (audioLineProvider.audioLanguageCode != audioLineProvider.textLanguageCode)
                {
                    textLocalization = audioLineProvider.YarnProject.GetLocalization(audioLineProvider.textLanguageCode);
                }
                else
                {
                    textLocalization = audioLocalization;
                }

                var text = textLocalization.GetLocalizedString(line.ID);

                AudioClip audioClip = null;

                if (audioLocalization.ContainsLocalizedAssets)
                {
                    audioClip = audioLocalization.GetLocalizedObject<AudioClip>(line.ID);
                }

                return new LocalizedLine()
                {
                    TextID = line.ID,
                    RawText = text,
                    Substitutions = line.Substitutions,
                    Metadata = audioLineProvider.YarnProject.lineMetadata.GetMetadata(line.ID),
                    Asset = audioClip,
                };
            }

            public void PrepareForLines(IEnumerable<string> lineIDs)
            {
                return;
            }
        }

#if USE_ADDRESSABLES
        private class AddressablesAudioLineProvider: IAssetLineProvider
        {
            public AudioLineProvider audioLineProvider { get; set; }

            // Lines are available if there are no outstanding load operations
            public bool LinesAvailable => pendingLoadOperations.Count == 0;

            private System.Action<AsyncOperationHandle<AudioClip>> AssetLoadCompleteAction;

            private Dictionary<AsyncOperationHandle<AudioClip>, string> pendingLoadOperations = new Dictionary<AsyncOperationHandle<AudioClip>, string>();

            private Dictionary<string, AsyncOperationHandle<AudioClip>> completedLoadOperations = new Dictionary<string, AsyncOperationHandle<AudioClip>>();

            public LocalizedLine GetLocalizedLine(Line line)
            {
                Localization audioLocalization = audioLineProvider.YarnProject.GetLocalization(audioLineProvider.audioLanguageCode);

                Localization textLocalization;

                // If the audio language is different to the text language,
                // pull the text data from a different localization
                if (audioLineProvider.audioLanguageCode != audioLineProvider.textLanguageCode)
                {
                    textLocalization = audioLineProvider.YarnProject.GetLocalization(audioLineProvider.textLanguageCode);
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
                    }
                    else
                    {
                        // We aren't using addressable assets, so fetch the
                        // asset directly from the localization object.
                        audioClip = audioLocalization.GetLocalizedObject<AudioClip>(line.ID);
                    }
                }

                return new LocalizedLine()
                {
                    TextID = line.ID,
                    RawText = text,
                    Substitutions = line.Substitutions,
                    Metadata = audioLineProvider.YarnProject.lineMetadata.GetMetadata(line.ID),
                    Asset = audioClip,
                };
            }

            public void PrepareForLines(IEnumerable<string> lineIDs)
            {
                var audioAddressableLocalization = audioLineProvider.YarnProject.GetLocalization(audioLineProvider.audioLanguageCode);

                if (audioAddressableLocalization.UsesAddressableAssets == false)
                {
                    Debug.LogError($"The Yarn project {audioLineProvider.YarnProject.name} isn't using addressable assets, but the {nameof(AudioLineProvider)} is attempting to do so. Double check your project settings.");
                    return;
                }

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
            }

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

                switch (operation.Status)
                {
                    case AsyncOperationStatus.Succeeded:
                        completedLoadOperations.Add(stringID, operation);
                        break;
                    case AsyncOperationStatus.Failed:
                        Debug.LogError($"Failed to load asset for line {stringID} in localization \"{audioLineProvider.YarnProject.GetLocalization(audioLineProvider.audioLanguageCode).LocaleCode}\"");
                        break;
                    default:
                        // We shouldn't be here?
                        throw new System.InvalidOperationException($"Load operation for {stringID} completed, but its status is {operation.Status}");
                }
                pendingLoadOperations.Remove(operation);
                // Debug.Log("operation complete");
            }
        }
#endif
#endregion
        
        /// <summary>Specifies the language code to use for text content
        /// for this <see cref="AudioLineProvider"/>.
        [Language]
        public string textLanguageCode = System.Globalization.CultureInfo.CurrentCulture.Name;

        /// <summary>Specifies the language code to use for audio content
        /// for this <see cref="AudioLineProvider"/>.
        [Language]
        public string audioLanguageCode = System.Globalization.CultureInfo.CurrentCulture.Name;

        public override string LocaleCode => textLanguageCode;

        private IAssetLineProvider _provider;
        private IAssetLineProvider provider
        {
            get
            {
                IAssetLineProvider configureProvider()
                {
                    IAssetLineProvider p;
                    if (this.YarnProject != null)
                    {
                        if (YarnProject.GetLocalization(audioLanguageCode).UsesAddressableAssets)
                        {
#if USE_ADDRESSABLES
                            p = new AddressablesAudioLineProvider();
#else
                            Debug.LogError($"The Yarn project {YarnProject.name} is configured to use Addressable assets, but the package is not installed. Double check your package settings. Falling back to providing non-Addressable audio loading");
                            p = new DirectReferenceAudioLineProvider();
#endif
                        }
                        else
                        {
                            p = new DirectReferenceAudioLineProvider();
                        }
                    }
                    else
                    {
                        Debug.LogError($"The {nameof(AudioLineProvider)} is attempting to configure itself but the project isn't defined.");
                        p = new NullProvider();
                    }
                    p.audioLineProvider = this;
                    return p;
                }

                // do we have an already set up provider?
                // first time this runs it will always be null so we need to set one up
                if (_provider == null)
                {
                    _provider = configureProvider();
                }
                // we have been configured but it is the null one
                // this only happens when a yarn project hasn't been provided so there isn't really any solution
                // except to try to configure it again and hope
                else if (_provider is NullProvider)
                {
                    // do we have a project yet?
                    // if so we reconfigure and try again
                    if (this.YarnProject != null)
                    {
                        _provider = configureProvider();
                    }
                }

                return _provider;
            }
        }

        public override bool LinesAvailable => provider.LinesAvailable;

        public override LocalizedLine GetLocalizedLine(Line line)
        {
            return provider.GetLocalizedLine(line);
        }

        public override void PrepareForLines(IEnumerable<string> lineIDs)
        {
            provider.PrepareForLines(lineIDs);
        }
    }
}
