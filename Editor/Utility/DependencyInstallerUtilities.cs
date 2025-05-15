using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if USE_UNITY_LOCALIZATION
using UnityEngine.Localization;
#endif

#nullable enable

namespace Yarn.Unity.Editor
{
    public abstract class PackageSetupStep
    {
        public abstract string PerformStepButtonLabel { get; }
        public abstract string Description { get; }
        public abstract bool NeedsSetup { get; }
        public abstract void RunSetup();
    }

    public class CustomPackageSetupStep : PackageSetupStep
    {
        public override string Description { get; }
        public override string PerformStepButtonLabel { get; }
        public override bool NeedsSetup => this.NeedsSetupAction();
        public override void RunSetup() => this.RunSetupAction();

        private System.Func<bool> NeedsSetupAction { get; }
        private System.Action RunSetupAction { get; }

        public CustomPackageSetupStep(string description,
                                string performStepButtonLabel,
                                System.Func<bool> needsSetup,
                                System.Action runSetup)
        {
            Description = description;
            NeedsSetupAction = needsSetup;
            RunSetupAction = runSetup;
            PerformStepButtonLabel = performStepButtonLabel;
        }
    }

#if USE_UNITY_LOCALIZATION
    public class UnityLocalizationSetupStep : PackageSetupStep
    {
        public static IEnumerable<string> SampleLocaleIdentifiers => new[] { "en", "es", "pt-BR", "de", "zh-Hans" };
        public static IDictionary<string, string> SampleLocaleFallbacks => new Dictionary<string, string> { { "es", "en" } };

        public override string Description =>
            "Unity Localization is installed, but your project doesn't have a " +
            "Localization Settings asset, and/or it lacks Locale assets that this sample needs.";

        public override string PerformStepButtonLabel => "Create Localization Assets";

        public override bool NeedsSetup
        {
            get
            {
                // Do we have settings?
                var settings = UnityEditor.Localization.LocalizationEditorSettings.ActiveLocalizationSettings;
                if (settings == null)
                {
                    return true;
                }
                // Do we have the appropriate locales?
                foreach (var identifier in SampleLocaleIdentifiers)
                {
                    // we now have a valid settings, but we don't know if it
                    // has english locale support
                    var localeID = new UnityEngine.Localization.LocaleIdentifier(identifier);
                    if (UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.GetLocale(localeID) == null)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public string DestinationPath { get; }

        public UnityLocalizationSetupStep(string destinationPath = "Assets/Localization")
        {
            this.DestinationPath = destinationPath;
        }

        public override void RunSetup()
        {
            // First, we need to make sure the folder we're working in exists
            if (Directory.Exists(this.DestinationPath) == false)
            {
                var pieces = this.DestinationPath.Split('/', System.StringSplitOptions.RemoveEmptyEntries);
                var parent = string.Join('/', pieces.Take(pieces.Length - 1));
                AssetDatabase.CreateFolder(parent, pieces.Last());
            }

            // Do we already have a LocalizationSettings asset? If not, create
            // one and set it up.
            var settings = UnityEditor.Localization.LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                // Create localization settings
                settings = ScriptableObject.CreateInstance<UnityEngine.Localization.Settings.LocalizationSettings>();
                settings.name = "Test Localization Settings";
                AssetDatabase.CreateAsset(settings, DestinationPath + "/Localization Settings.asset");

                // setting this new settings object to be the global settings
                // for the project
                UnityEditor.Localization.LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            foreach (var identifier in SampleLocaleIdentifiers)
            {
                // we now have a valid settings, but we don't know if it has
                // the locales we need
                var localeID = new LocaleIdentifier(identifier);
                if (UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.GetLocale(localeID) == null)
                {
                    // we need to make the asset and add it to the settings
                    // and on disk
                    var locale = Locale.CreateLocale(localeID);
                    AssetDatabase.CreateAsset(locale, DestinationPath + "/Locale " + identifier + ".asset");

                    UnityEditor.Localization.LocalizationEditorSettings.AddLocale(locale);
                }
            }

            // Finally, ensure that the locales have their fallbacks
            // configured correctly
            foreach (var (fromLocaleID, toLocaleID) in SampleLocaleFallbacks)
            {
                var fromLocale = UnityEditor.Localization.LocalizationEditorSettings.GetLocale(fromLocaleID);
                var toLocale = UnityEditor.Localization.LocalizationEditorSettings.GetLocale(toLocaleID);

                var fallbackMetadata = fromLocale.Metadata.GetMetadata<UnityEngine.Localization.Metadata.FallbackLocale>();
                if (fallbackMetadata == null)
                {
                    fallbackMetadata = new UnityEngine.Localization.Metadata.FallbackLocale();
                    fromLocale.Metadata.AddMetadata(fallbackMetadata);
                }
                fallbackMetadata.Locale = toLocale;
            }
            AssetDatabase.SaveAssets();


            // Find all table collections, and make sure they (and their
            // contents) are known to the addressable system (which might
            // have just been installed, so no assets have any addresses)
            var allTableCollectionGUIDs = AssetDatabase.FindAssets("t:LocalizationTableCollection");

            foreach (var guid in allTableCollectionGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var localizationCollection = AssetDatabase.LoadAssetAtPath<UnityEditor.Localization.LocalizationTableCollection>(path);

                // Make sure the table collection's assets are all
                // addressable

                localizationCollection.RefreshAddressables();

                // If this is an asset table collection, make sure that
                // every asset in all of its tables is addressable
                if (localizationCollection is UnityEditor.Localization.AssetTableCollection assetTableCollection)
                {
                    foreach (var table in assetTableCollection.AssetTables)
                    {
                        var allEntries = new Dictionary<long, UnityEngine.Localization.Tables.AssetTableEntry>(table);

                        foreach (var entry in allEntries)
                        {
                            var assetPath = AssetDatabase.GUIDToAssetPath(entry.Value.Guid);
                            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                            assetTableCollection.AddAssetToTable(table, entry.Key, asset);
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
#endif
}
