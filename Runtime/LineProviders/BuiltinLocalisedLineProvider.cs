/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Unity.Attributes;

#nullable enable

namespace Yarn.Unity
{
    public sealed class BuiltinLocalisedLineProvider : LineProviderBehaviour, ILineProvider
    {
        public override string LocaleCode
        {
            get => _textLocaleCode;
            set => _textLocaleCode = value;
        }

        [SerializeField, Language] private string _textLocaleCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        [SerializeField, Language] private string _assetLocaleCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        [SerializeField] private bool _useFallback = false;

        [ShowIf(nameof(_useFallback))]
        [SerializeField, Language] private string _fallbackLocaleCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        public string AssetLocaleCode
        {
            get => _assetLocaleCode;
            set => _assetLocaleCode = value;
        }

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

        public override async YarnTask<LocalizedLine> GetLocalizedLineAsync(Line line, CancellationToken cancellationToken)
        {
            string sourceLineID = line.ID;

            string[] metadata = System.Array.Empty<string>();

            // Check to see if this line shadows another. If it does, we'll use
            // that line's text and asset.
            if (YarnProject != null)
            {
                metadata = YarnProject.lineMetadata?.GetMetadata(line.ID) ?? System.Array.Empty<string>();

                var shadowLineSource = YarnProject.lineMetadata?.GetShadowLineSource(line.ID);

                if (shadowLineSource != null)
                {
                    sourceLineID = shadowLineSource;
                }
            }

            string? text = GetLocalizedString(sourceLineID);
            Object? asset = await GetLocalizedAssetAsync(sourceLineID);

            if (text == null)
            {
                // No line available.
                Debug.LogWarning($"Localization {LocaleCode} does not contain an entry for line {line.ID}", this);
                return LocalizedLine.InvalidLine;
            }

            var parseResult = lineParser.ParseString(Markup.LineParser.ExpandSubstitutions(text, line.Substitutions), LocaleCode);

            return new LocalizedLine
            {
                Text = parseResult,
                RawText = text,
                TextID = line.ID,
                Asset = asset,
                Metadata = metadata,
            };
        }

        private Localization GetLocalization(string locale)
        {
            if (YarnProject == null)
            {
                throw new System.InvalidOperationException($"Can't get localized line: no Yarn Project set");
            }

            Localization loc = YarnProject.GetLocalization(locale);

            if (loc == null)
            {
                throw new System.InvalidOperationException($"Can't get localized line: Yarn Project has no localisation for {locale}");
            }

            return loc;
        }

        private string? GetLocalizedString(string sourceLineID)
        {

            var baseLoc = GetLocalization(_textLocaleCode);
            string? text = baseLoc.GetLocalizedString(sourceLineID);

            if (text != null)
            {
                return text;
            }

            if (_useFallback)
            {
                var fallbackLoc = GetLocalization(_fallbackLocaleCode);
                return fallbackLoc.GetLocalizedString(sourceLineID);
            }

            return null;
        }

        private async YarnTask<Object?> GetLocalizedAssetAsync(string sourceLineID)
        {
            var baseLoc = GetLocalization(_assetLocaleCode);
            Object? result = await baseLoc.GetLocalizedObjectAsync<Object>(sourceLineID);

            if (result != null)
            {
                return result;
            }

            if (_useFallback)
            {
                var fallbackLoc = GetLocalization(_fallbackLocaleCode);
                return await fallbackLoc.GetLocalizedObjectAsync<Object>(sourceLineID);
            }

            return null;
        }

        public async override YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            if (YarnProject == null)
            {
                // We don't have a Yarn Project, so there's no preparation we
                // can do. do.
                return;
            }

            var assetLocalization = YarnProject.GetLocalization(AssetLocaleCode);

            if (assetLocalization.UsesAddressableAssets)
            {
                // The localization uses addressable assets. Ensure that these
                // assets are pre-loaded.
                var tasks = new List<YarnTask<Object?>>();

                foreach (var id in lineIDs)
                {
                    var task = assetLocalization.GetLocalizedObjectAsync<Object>(id);
                    tasks.Add(task);
                }

                await YarnTask.WhenAll(tasks);
            }
            else
            {
                // The localization uses direct references. No need to pre-load
                // the assets - they were loaded with the scene.
                return;
            }
        }
    }
}
