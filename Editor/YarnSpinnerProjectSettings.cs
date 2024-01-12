namespace Yarn.Unity.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;

    /// <summary>
    /// Basic data class of unity settings that impact Yarn Spinner.
    /// </summary>
    /// <remarks>
    /// Currently this only supports disabling the automatic reimport of Yarn Projects when locale assets change, but other settings will eventually end up here.
    /// </remarks>
    class YarnSpinnerProjectSettings
    {
        public static string YarnSpinnerProjectSettingsPath => Path.Combine("ProjectSettings", "Packages", "dev.yarnspinner", "YarnSpinnerProjectSettings.json");

        [SerializeField] internal bool autoRefreshLocalisedAssets = true;

        internal static YarnSpinnerProjectSettings GetOrCreateSettings()
        {
            YarnSpinnerProjectSettings settings = new YarnSpinnerProjectSettings();
            if (File.Exists(YarnSpinnerProjectSettingsPath))
            {
                try
                {
                    var settingsData = File.ReadAllText(YarnSpinnerProjectSettingsPath);
                    EditorJsonUtility.FromJsonOverwrite(settingsData, settings);

                    return settings;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to load Yarn Spinner project settings at {YarnSpinnerProjectSettingsPath}: {e.Message}");
                }
            }

            settings.autoRefreshLocalisedAssets = true;
            settings.WriteSettings();

            return settings;
        }

        internal void WriteSettings()
        {
            var jsonValue = EditorJsonUtility.ToJson(this);

            var folder = Path.GetDirectoryName(YarnSpinnerProjectSettingsPath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            try
            {
                File.WriteAllText(YarnSpinnerProjectSettingsPath, jsonValue);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save Yarn Spinner project settings to {YarnSpinnerProjectSettingsPath}: {e.Message}");
            }
        }
    }

    class YarnSpinnerProjectSettingsProvider : SettingsProvider
    {
        private YarnSpinnerProjectSettings baseSettings;

        public YarnSpinnerProjectSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // This function is called when the user clicks on the MyCustom element in the Settings window.
            baseSettings = YarnSpinnerProjectSettings.GetOrCreateSettings();
        }

        public override void OnGUI(string searchContext)
        {
            // Use IMGUI to display UI:
            EditorGUILayout.LabelField("Automatically update localised assets with Yarn Projects");

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                var result = EditorGUILayout.Toggle(baseSettings.autoRefreshLocalisedAssets);

                if (changeCheck.changed)
                {
                    baseSettings.autoRefreshLocalisedAssets = result;
                    baseSettings.WriteSettings();
                }
            }
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateYarnSpinnerProjectSettingsProvider()
        {
            var provider = new YarnSpinnerProjectSettingsProvider("Project/Yarn Spinner", SettingsScope.Project);

            var keywords = new List<string>() { "yarn", "spinner", "localisation" };
            provider.keywords = keywords;
            return provider;
        }
    }
}
