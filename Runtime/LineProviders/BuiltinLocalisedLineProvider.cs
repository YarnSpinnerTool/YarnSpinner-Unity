#nullable enable

using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    using UnityEngine;

    using System.Threading;
#if USE_UNITASK
    using Cysharp.Threading.Tasks;
    using YarnTask = Cysharp.Threading.Tasks.UniTask;
    using YarnIntTask = Cysharp.Threading.Tasks.UniTask<int>;
    using YarnLineTask = Cysharp.Threading.Tasks.UniTask<LocalizedLine>;
    using YarnObjectTask = Cysharp.Threading.Tasks.UniTask<UnityEngine.Object?>;
#elif UNITY_2023_1_OR_NEWER
    using YarnTask = UnityEngine.Awaitable;
    using YarnLineTask = UnityEngine.Awaitable<LocalizedLine>;
#else
    using YarnTask = System.Threading.Tasks.Task;
    using YarnLineTask = System.Threading.Tasks.Task<LocalizedLine>;
    using YarnObjectTask = System.Threading.Tasks.Task<UnityEngine.Object?>;
#endif

    using Yarn;
    using System.Collections.Generic;

#if USE_ADDRESSABLES
    using AddressablesHelper = Yarn.Unity.UnityAddressablesHelper;
#else
    using AddressablesHelper = Yarn.Unity.NullAddressablesHelper;
#endif

    public class BuiltinLocalisedLineProvider : LineProviderBehaviour, ILineProvider
    {
        public override string LocaleCode
        {
            get => _textLocaleCode;
            set => _textLocaleCode = value;
        }

        [SerializeField, Language] private string _textLocaleCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        [SerializeField, Language] private string _assetLocaleCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        public string AssetLocaleCode
        {
            get => _assetLocaleCode;
            set => _assetLocaleCode = value;
        }

        private YarnTask prepareForLinesTask = YarnAsync.Never(CancellationToken.None);

        public override bool LinesAvailable => prepareForLinesTask.IsCompletedSuccessfully();

        IAddressablesHelper addressablesHelper = new AddressablesHelper();

        private Markup.LineParser lineParser = new Markup.LineParser();
        private Markup.BuiltInMarkupReplacer builtInReplacer = new Markup.BuiltInMarkupReplacer();

        public override void RegisterMarkerProcessor(string attributeName, Markup.IAttributeMarkerProcessor markerProcessor)
        {
            lineParser.RegisterMarkerProcessor(attributeName, markerProcessor);
        }
        public override void DeregisterMarkerProcessor(string attributeName)
        {
            lineParser.DeregisterMarkerProcessor(attributeName);
        }

        void Awake()
        {
            lineParser.RegisterMarkerProcessor("select", builtInReplacer);
            lineParser.RegisterMarkerProcessor("plural", builtInReplacer);
            lineParser.RegisterMarkerProcessor("ordinal", builtInReplacer);
        }

        public override async YarnLineTask GetLocalizedLineAsync(Line line, CancellationToken cancellationToken)
        {
            Localization loc = CurrentLocalization;

            string sourceLineID = line.ID;

            string[] metadata = System.Array.Empty<string>();

            // Check to see if this line shadows another. If it does, we'll use
            // that line's text and asset.
            if (YarnProject != null)
            {
                metadata = YarnProject.lineMetadata.GetMetadata(line.ID) ?? System.Array.Empty<string>();

                var shadowLineSource = YarnProject.lineMetadata.GetShadowLineSource(line.ID);

                if (shadowLineSource != null)
                {
                    sourceLineID = shadowLineSource;
                }
            }

            string? text = loc.GetLocalizedString(sourceLineID);

            if (text == null)
            {
                // No line available.
                return LocalizedLine.InvalidLine;
            }

            var parseResult = lineParser.ParseString(Markup.LineParser.ExpandSubstitutions(text, line.Substitutions), this.LocaleCode);

            Object? asset;

            if (loc.UsesAddressableAssets)
            {
                // Fetch the asset from the addressables helper. (If the
                // Addressables package is not available, this object will log
                // an error and return null.)
                asset = await addressablesHelper.GetObject(sourceLineID, AssetLocaleCode, cancellationToken);
            }
            else
            {
                // This localisation doesn't use addressable assets. Fetch the
                // asset directly from the Localization object.
                asset = loc.GetLocalizedObject<Object>(sourceLineID);
            }

            return new LocalizedLine
            {
                Text = parseResult,
                RawText = text,
                TextID = line.ID,
                Asset = asset,
                Metadata = metadata,
            };
        }

        public async override YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            Localization loc = CurrentLocalization;

            if (loc.UsesAddressableAssets)
            {
                // The localization uses addressable assets. Ensure that these
                // assets are pre-loaded.
                prepareForLinesTask = addressablesHelper.PrepareForLinesAsync(lineIDs, this.AssetLocaleCode, cancellationToken);

                await prepareForLinesTask;
            }
            else
            {
                // The localization uses direct references. No need to pre-load
                // the assets - they were loaded with the scene.
                return;
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
}

namespace Yarn.Unity
{
#if USE_UNITASK
    using Cysharp.Threading.Tasks;
    using YarnTask = Cysharp.Threading.Tasks.UniTask;
    using YarnObjectTask = Cysharp.Threading.Tasks.UniTask<UnityEngine.Object?>;
#elif UNITY_2023_1_OR_NEWER
    using YarnTask = UnityEngine.Awaitable;
    using YarnObjectTask = UnityEngine.Awaitable<UnityEngine.Object?>;
#else
    using YarnTask = System.Threading.Tasks.Task;
    using YarnObjectTask = System.Threading.Tasks.Task<UnityEngine.Object?>;
#endif


#if USE_ADDRESSABLES
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEditor;

    internal class UnityAddressablesHelper : IAddressablesHelper
    {
        private Dictionary<string, AsyncOperationHandle<UnityEngine.Object>> assetHandles = new Dictionary<string, AsyncOperationHandle<UnityEngine.Object>>();

        public async YarnObjectTask GetObject(string assetID, string localeID, CancellationToken cancellationToken)
        {
            assetHandles.TryGetValue(assetID, out AsyncOperationHandle<Object> handle);

            if (handle.IsValid() == false)
            {
                // We don't have a loading handle for this asset. Start
                // loading it now.

                string assetAddress = Localization.GetAddressForLine(assetID, localeID);

                handle = Addressables.LoadAssetAsync<Object>(assetAddress);

                assetHandles.Add(localeID, handle);
            }

            if (handle.IsDone == false)
            {
                // The asset isn't already in memory. Wait for the handle to
                // finish loading.
                await YarnAsync.WaitForAsyncOperation(handle, cancellationToken);
            }

            // Get the asset itself.
            var asset = handle.Result;
            return asset;
        }

        private static async YarnTask FetchLine(string lineID, string localeID, Dictionary<string, AsyncOperationHandle<Object>> operationCache, CancellationToken cancellationToken)
        {

            // Find the location of the line's asset, and if a location exists,
            // start loading it. Cache the load operation so we can test for it
            // later. If a location is not available, do nothing.

            var address = Localization.GetAddressForLine(lineID, localeID);
            var location = await YarnAsync.WaitForAsyncOperation(Addressables.LoadResourceLocationsAsync(address), cancellationToken);

            if (location == null || location.Count == 0)
            {
                // No location available for this asset. Don't attempt to load it.
                return;
            }
            var assetLoadOperation = Addressables.LoadAssetAsync<UnityEngine.Object>(address);

            operationCache.Add(lineID, assetLoadOperation);

            await YarnAsync.WaitForAsyncOperation(assetLoadOperation, cancellationToken);
        }

        public async YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, string localeID, CancellationToken cancellationToken)
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
                allTasks.Add(FetchLine(id, localeID, assetHandles, cancellationToken));
            }

            // Wait for all of the lines to become ready.
            await YarnTask.WhenAll(allTasks);
        }
    }
#endif

    internal class NullAddressablesHelper : IAddressablesHelper
    {
        public YarnObjectTask GetObject(string assetID, string localeID, CancellationToken cancellationToken)
        {
            Debug.LogWarning($"Can't fetch assets for line {assetID}: the localisation object uses Addressable Assets, but the Addressables package isn't installed.");
            return YarnAsync.FromResult<Object?>(null);
        }

        public YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, string localeID, CancellationToken cancellationToken)
        {
            // No-op, because addressables support is not available.
            return YarnAsync.CompletedTask;
        }
    }

    internal interface IAddressablesHelper
    {
        public YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, string localeID, CancellationToken cancellationToken);
        public YarnObjectTask GetObject(string assetID, string localeID, CancellationToken cancellationToken);
    }
}
