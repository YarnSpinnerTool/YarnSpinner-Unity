namespace Yarn.Unity.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;

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
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.LabelField("Automatically update localised assets with Yarn Projects");
                var localisedAssetUpdate = EditorGUILayout.Toggle(baseSettings.autoRefreshLocalisedAssets);

                EditorGUILayout.LabelField("do codegen");
                var linkingAttributedFuncs = EditorGUILayout.Toggle(baseSettings.automaticallyLinkAttributedYarnCommandsAndFunctions);

                if (changeCheck.changed)
                {
                    baseSettings.autoRefreshLocalisedAssets = localisedAssetUpdate;
                    baseSettings.automaticallyLinkAttributedYarnCommandsAndFunctions = linkingAttributedFuncs;
                    baseSettings.WriteSettings();
                }
            }
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateYarnSpinnerProjectSettingsProvider()
        {
            var provider = new YarnSpinnerProjectSettingsProvider("Project/Yarn Spinner", SettingsScope.Project);

            var keywords = new List<string>() { "yarn", "spinner", "localisation", "codegen", "attribute" };
            provider.keywords = keywords;
            return provider;
        }
    }
}
