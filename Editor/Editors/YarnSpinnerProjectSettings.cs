/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Editor
{
    using System.Collections.Generic;
    using System.IO;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
#endif

#nullable enable

    /// <summary>
    /// Basic data class of unity settings that impact Yarn Spinner.
    /// </summary>
    /// <remarks>
    /// Currently this only supports disabling the automatic reimport of Yarn Projects when locale assets change, but other settings will eventually end up here.
    /// </remarks>
    class YarnSpinnerProjectSettings
    {
        public static string YarnSpinnerProjectSettingsPath => Path.Combine("ProjectSettings", "Packages", "dev.yarnspinner", "YarnSpinnerProjectSettings.json");
        public static string YarnSpinnerGeneratedYSLSPath => Path.Combine("ProjectSettings", "Packages", "dev.yarnspinner", "generated.ysls.json");

        public bool autoRefreshLocalisedAssets = true;
        public bool automaticallyLinkAttributedYarnCommandsAndFunctions = true;
        public bool generateYSLSFile = false;
        public bool enableDirectLinkToVSCode = false;
        public (int major, int minor) Version
        {
            get
            {
                return (majorVersion, minorVersion);
            }
            set
            {
                majorVersion = value.major;
                minorVersion = value.minor;
            }
        }
        private int majorVersion = 0;
        private int minorVersion = 0;

        private const string automaticallyLinkAttributedYarnCommandsAndFunctionsKey = "automaticallyLinkAttributedYarnCommandsAndFunctions";
        private const string autoRefreshLocalisedAssetsKey = "autoRefreshLocalisedAssets";
        private const string generateYSLSFileKey = "generateYSLSFile";
        private const string enableDirectLinkToVSCodeKey = "enableDirectLinkToVSCode";
        private const string majorVersionKey = "majorVersion";
        private const string minorVersionKey = "minorVersion";

        internal static YarnSpinnerProjectSettings GetOrCreateSettings(string? path = null, Yarn.Unity.ILogger? iLogger = null)
        {
            var settingsPath = YarnSpinnerProjectSettingsPath;
            if (path != null)
            {
                settingsPath = Path.Combine(path, YarnSpinnerProjectSettingsPath);
            }
            var logger = ValidLogger(iLogger);

            YarnSpinnerProjectSettings settings = new YarnSpinnerProjectSettings();
            if (File.Exists(settingsPath))
            {
                try
                {
                    var settingsData = File.ReadAllText(settingsPath);
                    settings = FromJson(settingsData, logger);

                    return settings;
                }
                catch (System.Exception e)
                {
                    logger.WriteException(e, $"Failed to load Yarn Spinner project settings at {settingsPath}");
                }
            }

            settings.autoRefreshLocalisedAssets = true;
            settings.automaticallyLinkAttributedYarnCommandsAndFunctions = true;
            settings.generateYSLSFile = false;
            settings.majorVersion = 0;
            settings.minorVersion = 0;
            settings.WriteSettings(path, logger);

            return settings;
        }

        private static YarnSpinnerProjectSettings FromJson(string jsonString, Yarn.Unity.ILogger? iLogger = null)
        {
            var logger = ValidLogger(iLogger);

            YarnSpinnerProjectSettings settings = new YarnSpinnerProjectSettings();

            try
            {
                var jsonDict = Json.Deserialize(jsonString) as Dictionary<string, object>;

                if (jsonDict == null)
                {
                    logger.WriteLine($"Failed to parse Yarn Spinner project settings JSON");
                    return settings;
                }

                T GetValueOrDefault<T>(string key, T defaultValue)
                {
                    if (jsonDict.TryGetValue(key, out object result))
                    {
                        return (T)System.Convert.ChangeType(result, typeof(T));
                    }
                    else
                    {
                        return defaultValue;
                    }
                }

                bool automaticallyLinkAttributedYarnCommandsAndFunctions = GetValueOrDefault(automaticallyLinkAttributedYarnCommandsAndFunctionsKey, true);
                bool autoRefreshLocalisedAssets = GetValueOrDefault(autoRefreshLocalisedAssetsKey, true);
                bool generateYSLSFile = GetValueOrDefault(generateYSLSFileKey, false);
                bool enableDirectLinkToVSCode = GetValueOrDefault(enableDirectLinkToVSCodeKey, false);
                int major = GetValueOrDefault(majorVersionKey, 0);
                int minor = GetValueOrDefault(minorVersionKey, 0);

                settings.automaticallyLinkAttributedYarnCommandsAndFunctions = automaticallyLinkAttributedYarnCommandsAndFunctions;
                settings.autoRefreshLocalisedAssets = autoRefreshLocalisedAssets;
                settings.generateYSLSFile = generateYSLSFile;
                settings.enableDirectLinkToVSCode = enableDirectLinkToVSCode;
                settings.majorVersion = major;
                settings.minorVersion = minor;
            }
            catch (System.Exception ex)
            {
                logger.WriteException(ex);
            }

            return settings;
        }

        internal void WriteSettings(string? path = null, Yarn.Unity.ILogger? iLogger = null)
        {
            var logger = ValidLogger(iLogger);

            var settingsPath = YarnSpinnerProjectSettingsPath;
            if (path != null)
            {
                settingsPath = Path.Combine(path, settingsPath);
            }

            var dictForm = new System.Collections.Generic.Dictionary<string, object>();
            dictForm[automaticallyLinkAttributedYarnCommandsAndFunctionsKey] = this.automaticallyLinkAttributedYarnCommandsAndFunctions;
            dictForm[autoRefreshLocalisedAssetsKey] = this.autoRefreshLocalisedAssets;
            dictForm[generateYSLSFileKey] = this.generateYSLSFile;
            dictForm[enableDirectLinkToVSCodeKey] = this.enableDirectLinkToVSCode;
            dictForm[majorVersionKey] = this.majorVersion;
            dictForm[minorVersionKey] = this.minorVersion;

            var jsonValue = Json.Serialize(dictForm);

            var folder = Path.GetDirectoryName(settingsPath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            try
            {
                File.WriteAllText(settingsPath, jsonValue);
            }
            catch (System.Exception e)
            {
                logger.WriteException(e, $"Failed to save Yarn Spinner project settings to {settingsPath}");
            }
        }

        // if the provided logger is valid just return it
        // otherwise return the default logger
        private static Yarn.Unity.ILogger ValidLogger(Yarn.Unity.ILogger? iLogger)
        {
            var logger = iLogger;
            if (logger == null)
            {
#if UNITY_EDITOR
                logger = new UnityLogger();
#else
                logger = new NullLogger();
#endif
            }
            return logger;
        }
    }
}
