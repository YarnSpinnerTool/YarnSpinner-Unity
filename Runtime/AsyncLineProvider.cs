// #define USE_UNITASK

namespace Yarn.Unity
{
    using UnityEngine;

    using System.Collections;
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
    using Yarn.Unity;

#if USE_ADDRESSABLES
    using UnityEngine.AddressableAssets;
#endif
    using System.Collections.Generic;

#nullable enable

    public interface ILineProvider
    {
        public YarnProject? YarnProject { get; set; }
        public string LocaleCode { get; }
        public YarnLineTask GetLocalizedLineAsync(Line line, CancellationToken cancellationToken);
        public YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken);

        [System.Obsolete("Await the result of PrepareForLinesAsync instead of polling this property.")]
        public bool LinesAvailable { get; }
    }

#if USE_ADDRESSABLES
    public class AsyncLineProvider : MonoBehaviour, ILineProvider
    {
        public YarnProject? YarnProject { get; set; }

        public string LocaleCode
        {
            get => _textLocaleCode;
            set => _textLocaleCode = value;
        }

        [SerializeField,Language] private string _textLocaleCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        [SerializeField,Language] private string _assetLocaleCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        private Dictionary<string, Object> cachedAssets = new Dictionary<string, Object>();

        private YarnTask? prepareForLinesTask;

        public bool LinesAvailable => prepareForLinesTask?.IsCompletedSuccessfully ?? false;
        
        public async YarnLineTask GetLocalizedLineAsync(Line line, CancellationToken cancellationToken)
        {
            Localization loc = CurrentLocalization;
            string text = loc.GetLocalizedString(line.ID);

            cachedAssets.TryGetValue(line.ID, out Object? asset);

            if (asset == null)
            {
                // We didn't find an asset for this line. Try to get one now.

                if (loc.UsesAddressableAssets)
                {
                    string assetAddress = Localization.GetAddressForLine(line.ID, LocaleCode);

                    asset = await YarnAsync.WaitForAsyncOperation(
                        Addressables.LoadAssetAsync<AudioClip>(assetAddress),
                        cancellationToken
                    );

                    if (asset != null) { cachedAssets.Add(line.ID, asset); }
                }
                else
                {
                    asset = loc.GetLocalizedObject<Object>(line.ID);
                }
            }

            return new LocalizedLine
            {
                RawText = text,
                TextID = line.ID,
                Asset = asset,
            };
        }

        public async YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            if (CurrentLocalization.UsesAddressableAssets)
            {
                foreach (var entry in cachedAssets)
                {
                    Addressables.Release(entry.Value);
                }
                cachedAssets.Clear();
                var allTasks = new List<YarnTask>();
                foreach (var id in lineIDs)
                {
                    allTasks.Add(LoadAssetIntoCache(id, cancellationToken));
                }
                prepareForLinesTask = YarnTask.WhenAll(allTasks);
                await prepareForLinesTask;
            }
        }

        public async YarnTask LoadAssetIntoCache(string id, CancellationToken token)
        {
            var address = Localization.GetAddressForLine(id, LocaleCode);
            var load = Addressables.LoadAssetAsync<Object>(address);
            var asset = await YarnAsync.WaitForAsyncOperation(load, token);
            if (asset != null)
            {
                cachedAssets.Add(address, asset);
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
public class AsyncLineProvider : MonoBehaviour, ILineProvider
{
    public YarnProject? YarnProject { get; set; }

    public string LocaleCode
    {
        get => _localeCode;
        set => _localeCode = value;
    }

    [SerializeField] private string _localeCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

    public YarnLineTask GetLocalizedLineAsync(Line line)
    {
        var loc = CurrentLocalization;

        var text = loc.GetLocalizedString(line.ID);
        var asset = loc.GetLocalizedObject<Object>(line.ID);

        return UniTask.FromResult(new LocalizedLine
        {
            RawText = text,
            Asset = asset,
        });
    }

    public YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs)
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
}
#endif

}
