namespace Yarn.Unity
{
    using UnityEngine;

    using System.Threading;
#if USE_UNITASK
    using Cysharp.Threading.Tasks;
    using YarnTask = Cysharp.Threading.Tasks.UniTask;
    using YarnIntTask = Cysharp.Threading.Tasks.UniTask<int>;
    using YarnLineTask = Cysharp.Threading.Tasks.UniTask<LocalizedLine>;
#else
    using YarnTask = System.Threading.Tasks.Task;
    using YarnLineTask = System.Threading.Tasks.Task<LocalizedLine>;
#endif

    using Yarn;

#if USE_ADDRESSABLES
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
#endif
    using System.Collections.Generic;

#nullable enable

#if USE_ADDRESSABLES
    public class BuiltinLocalisedLineProvider : LineProviderBehaviour, ILineProvider
    {
        public override string LocaleCode
        {
            get => _textLocaleCode;
            set => _textLocaleCode = value;
        }

        [SerializeField, Language] private string _textLocaleCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        [SerializeField, Language] private string _assetLocaleCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        private Dictionary<string, AsyncOperationHandle<Object>> assetHandles = new Dictionary<string, AsyncOperationHandle<Object>>();

        private YarnTask? prepareForLinesTask;

        public override bool LinesAvailable => prepareForLinesTask?.IsCompletedSuccessfully ?? false;

        public override async YarnLineTask GetLocalizedLineAsync(Line line, CancellationToken cancellationToken)
        {
            Localization loc = CurrentLocalization;
            string text = loc.GetLocalizedString(line.ID);

            if (!loc.UsesAddressableAssets) {
                // This localisation doesn't use addressable assets. Fetch the
                // asset directly.
                return new LocalizedLine()
                {
                    RawText = text,
                    TextID = line.ID,
                    Asset = loc.GetLocalizedObject<Object>(line.ID)
                };
            }

            assetHandles.TryGetValue(line.ID, out AsyncOperationHandle<Object> handle);

            if (handle.IsValid() == false)
            {
                // We don't have a loading handle for this asset. Start loading it now.
            
                string assetAddress = Localization.GetAddressForLine(line.ID, _assetLocaleCode);

                handle = Addressables.LoadAssetAsync<Object>(assetAddress);
                assetHandles.Add(line.ID, handle);
                
            }

            if (handle.IsDone == false) {
                // Wait for the handle to finish loading.
                await YarnAsync.WaitForAsyncOperation(handle, cancellationToken);
            }

            // Get the asset itself.
            var asset = handle.Result;

            return new LocalizedLine
            {
                RawText = text,
                TextID = line.ID,
                Asset = asset,
            };
        }

        public async override YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            if (CurrentLocalization.UsesAddressableAssets)
            {
                // Release all existing asset handles.
                foreach (var entry in assetHandles)
                {
                    Addressables.Release(entry.Value);
                }
                assetHandles.Clear();
                var allTasks = new List<YarnTask>();
                foreach (var id in lineIDs)
                {
                    // Requesting the new lines, and cache the load operation
                    // handles for them.
                    var address = Localization.GetAddressForLine(id, _assetLocaleCode);
                    var load = Addressables.LoadAssetAsync<Object>(address);
                    assetHandles.Add(id, load);
                    allTasks.Add(YarnAsync.WaitForAsyncOperation(load, cancellationToken));
                }

                // Wait for all of the lines to become ready.
                prepareForLinesTask = YarnTask.WhenAll(allTasks);
                await prepareForLinesTask;
            }
        }

        private Localization CurrentLocalization
        {
            get
            {
                if (YarnProject != null)
                {
                    return YarnProject.GetLocalization(LocaleCode);
                }
                else
                {
                    throw new System.InvalidOperationException($"Can't get a localised line because {nameof(YarnProject)} is not set!");
                }
            }
        }
    }
#else
    // This version of the class is used when the Addressables package is not
    // installed.
    public class BuiltinLocalisedLineProvider : LineProviderBehaviour, ILineProvider
    {
        public override string LocaleCode
        {
            get => _localeCode;
            set => _localeCode = value;
        }

        [SerializeField] private string _localeCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        public override YarnLineTask GetLocalizedLineAsync(Line line, CancellationToken cancellationToken)
        {
            var loc = CurrentLocalization;

            var text = loc.GetLocalizedString(line.ID);
            Object? asset;

            if (loc.UsesAddressableAssets)
            {
                // This localisation uses addressable assets, but the Addressables
                // package isn't available.
                Debug.LogWarning($"Can't fetch assets for line {line.ID}: the localisation object uses Addressable Assets, but the Addressables package isn't installed.");
                asset = null;
            } else {
                asset = loc.GetLocalizedObject<Object>(line.ID);
            }

            return YarnTask.FromResult(new LocalizedLine
            {
                RawText = text,
                Asset = asset,
            });
        }

        public override YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            return YarnTask.CompletedTask;
        }

        private Localization CurrentLocalization
        {
            get
            {
                if (YarnProject != null)
                {
                    return YarnProject.GetLocalization(LocaleCode);
                }
                else
                {
                    throw new System.InvalidOperationException($"Can't get a localised line because {nameof(YarnProject)} is not set!");
                }
            }
        }

        public override bool LinesAvailable => true;
    }
#endif

}
