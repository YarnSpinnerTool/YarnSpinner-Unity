
namespace Yarn.Unity.Samples.Editor
{
    using UnityEditor;
    using UnityEditor.PackageManager;
    using UnityEditor.PackageManager.Requests;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor.Callbacks;
    using System.IO;
#if USE_UNITY_LOCALIZATION
    using UnityEngine.Localization;
    using UnityEditor.AddressableAssets;

#endif

#nullable enable

    public class DependenciesInstallerTool : EditorWindow
    {
        public static IEnumerable<string> SampleLocaleIdentifiers => new[] { "en", "es" };

        [InitializeOnLoadMethod]
        public static void AddOpenSceneHook()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += (scene, mode) => ShowWindowIfNeeded();
        }

        public static void ShowWindowIfNeeded()
        {
            var dependencyInstaller = FindAnyObjectByType<DependencyInstaller>(FindObjectsInactive.Include);

            if (dependencyInstaller == null)
            {
                return;
            }

            var requirements = dependencyInstaller.requirements;

            if (AreDependenciesReady(requirements) == false)
            {
                Install(dependencyInstaller);
            }

        }
        private class PackageSetupStep
        {
            public string Description { get; }
            public string PerformStepButtonLabel { get; }
            public System.Func<bool> NeedsSetup { get; }
            public System.Action RunSetup { get; }

            public PackageSetupStep(string description,
                                    string performStepButtonLabel,
                                    System.Func<bool> needsSetup,
                                    System.Action runSetup)
            {
                Description = description;
                NeedsSetup = needsSetup;
                RunSetup = runSetup;
                PerformStepButtonLabel = performStepButtonLabel;
            }
        }

        private static PackageSetupStep? GetSetupStep(DependencyInstaller.DependencyPackage package)
        {
            if (package.PackageName == "com.unity.localization")
            {
#if !USE_UNITY_LOCALIZATION
                return null;
#else
                return new PackageSetupStep(
                    "Unity Localization is installed, but your project doesn't have a Localization Settings asset, and/or it lacks Locale assets that this sample needs.",
                    "Create Localization Assets",
                    static () =>
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
                            // we now have a valid settings, but we don't know
                            // if it has english locale support
                            var localeID = new UnityEngine.Localization.LocaleIdentifier("en");
                            if (UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.GetLocale(localeID) == null)
                            {
                                return true;
                            }
                        }
                        return false;
                    },
                    static () =>
                    {
                        var localizationAssetsPath = "Assets/Localization";
                        // first we need to make a temporary folder to store all
                        // these assets
                        if (Directory.Exists(localizationAssetsPath) == false)
                        {
                            AssetDatabase.CreateFolder("Assets", "Localization");
                        }

                        var settings = UnityEditor.Localization.LocalizationEditorSettings.ActiveLocalizationSettings;
                        if (settings == null)
                        {
                            // Create localization settings
                            settings = CreateInstance<UnityEngine.Localization.Settings.LocalizationSettings>();
                            settings.name = "Test Localization Settings";
                            AssetDatabase.CreateAsset(settings, localizationAssetsPath + "/Localization Settings.asset");

                            // setting this new settings object to be th global
                            // settings for the project
                            AssetDatabase.SaveAssets();
                            UnityEditor.Localization.LocalizationEditorSettings.ActiveLocalizationSettings = settings;
                        }

                        foreach (var identifier in SampleLocaleIdentifiers)
                        {
                            // we now have a valid settings, but we don't know
                            // if it has english locale support
                            var localeID = new LocaleIdentifier(identifier);
                            if (UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.GetLocale(localeID) == null)
                            {
                                // we need to make the asset and add it to the
                                // settings and on disk
                                var locale = Locale.CreateLocale(localeID);
                                AssetDatabase.CreateAsset(locale, localizationAssetsPath + "/Locale " + identifier + ".asset");
                                AssetDatabase.SaveAssets();

                                UnityEditor.Localization.LocalizationEditorSettings.AddLocale(locale);
                            }
                        }

                        // Find all table collections, and make sure they (and
                        // their contents) are known to the addressable system
                        // (which might have just been installed, so no assets
                        // have any addresses)
                        var allTableCollectionGUIDs = AssetDatabase.FindAssets("t:LocalizationTableCollection");

                        foreach (var guid in allTableCollectionGUIDs)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            var localizationCollection = AssetDatabase.LoadAssetAtPath<UnityEditor.Localization.LocalizationTableCollection>(path);

                            // Make sure the table collection's assets are all
                            // addressable

                            localizationCollection.RefreshAddressables();

                            // If this is an asset table collection, make sure
                            // that every asset in all of its tables is
                            // addressable
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
                        
                    }
                );
#endif
            }
            else
            {
                return null;
            }
        }

        public static bool AreDependenciesAvailable(IEnumerable<DependencyInstaller.DependencyPackage> dependencies)
        {
            return dependencies.Count() == 0 || dependencies.All(d => CheckAssemblyLoaded(d.AssemblyName));
        }

        public static bool AreDependenciesReady(IEnumerable<DependencyInstaller.DependencyPackage> dependencies)
        {
            return AreDependenciesAvailable(dependencies) && AreDependenciesConfigured(dependencies);
        }

        public static bool AreDependenciesConfigured(IEnumerable<DependencyInstaller.DependencyPackage> dependencies)
        {
            return dependencies.Count() == 0 || dependencies.All(d => CheckPackageConfigured(d));
        }

        public static bool CheckPackageConfigured(DependencyInstaller.DependencyPackage package)
        {
            var setup = GetSetupStep(package);
            if (setup == null)
            {
                return true;
            }
            return !setup.NeedsSetup();
        }

        public static bool CheckAssemblyLoaded(string name)
        {
            return System.AppDomain.CurrentDomain
                .GetAssemblies()
                .Any(assembly => assembly.FullName.Contains(name));
        }

        public static void Install(DependencyInstaller dependencyInstaller)
        {
            DependenciesInstallerTool window = EditorWindow.GetWindow<DependenciesInstallerTool>();
            window.titleContent = new GUIContent("Install Sample Dependencies");
            window.DependencyInstaller = dependencyInstaller;
            window.ShowUtility();
        }

        private DependencyInstaller? _cachedDependencyInstallerInstance;
        const string CurrentDependencyInstallerObjectIDKey = nameof(DependencyInstallerEditor) + "." + nameof(DependencyInstaller);
        private DependencyInstaller? DependencyInstaller
        {
            set
            {
                _cachedDependencyInstallerInstance = value;
                var id = GlobalObjectId.GetGlobalObjectIdSlow(value);
                SessionState.SetString(CurrentDependencyInstallerObjectIDKey, id.ToString());
            }

            get
            {
                if (_cachedDependencyInstallerInstance == null)
                {
                    // Retrieve the global object ID
                    var idString = SessionState.GetString(CurrentDependencyInstallerObjectIDKey, string.Empty);

                    // Attempt to find the DependencyInstaller, possibly from a
                    // previously loaded domain
                    if (!string.IsNullOrEmpty(idString)
                        && GlobalObjectId.TryParse(idString, out var id)
                        && GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is DependencyInstaller installer)
                    {
                        _cachedDependencyInstallerInstance = installer;
                    }
                }
                return _cachedDependencyInstallerInstance;
            }
        }

        private Vector2 scrollViewPosition = Vector2.zero;

        protected void OnGUI()
        {
            if (this.DependencyInstaller != null)
            {
                scrollViewPosition = EditorGUILayout.BeginScrollView(scrollViewPosition);
                DrawDependencyInstallerGUI(isWindow: true, this.DependencyInstaller.requirements);
                EditorGUILayout.EndScrollView();
            }
        }

        internal static void DrawDependencyInstallerGUI(bool isWindow, IEnumerable<DependencyInstaller.DependencyPackage>? dependencies)
        {
            var wrap = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                richText = true
            };

            if (dependencies == null)
            {
                if (isWindow == true)
                {
                    // Close the window - we likely just did a domain reload
                    // after installation
                    var window = GetWindow<DependenciesInstallerTool>();
                    window.Close();
                }
                else
                {
                    // Show an error in the inspector
                    EditorGUILayout.HelpBox($"{nameof(DependencyInstaller)} has null {nameof(dependencies)}", MessageType.Error);
                }
                return;
            }

            if (dependencies.Count() == 0)
            {
                EditorGUILayout.LabelField(
                    "This sample has no dependencies.",
                    wrap
                );
                return;
            }



            if (AreDependenciesReady(dependencies))
            {
                string message;

                if (isWindow)
                {
                    message = "All dependencies for this sample are installed and enabled. You can close this window.";
                }
                else
                {
                    message = "All dependencies for this sample are installed and enabled. You can delete this object.";
                }
                EditorGUILayout.LabelField(message, wrap);
                return;
            }

            EditorGUILayout.LabelField("This sample requires some additional packages, and won't work correctly without them.", wrap);
            EditorGUILayout.Space();

            foreach (var dependency in dependencies)
            {
                if (!CheckAssemblyLoaded(dependency.AssemblyName))
                {
                    EditorGUILayout.LabelField($"This sample requires {dependency.Name}.", wrap);

                    using (new EditorGUI.DisabledGroupScope(PackageInstaller.IsInstallationInProgress))
                    {
                        if (GUILayout.Button($"Install {dependency.Name}"))
                        {
                            PackageInstaller.Add(dependency.PackageName);
                        }
                    }

                    EditorGUILayout.Space();
                }

                var setup = GetSetupStep(dependency);
                if (setup != null && setup.NeedsSetup())
                {
                    EditorGUILayout.LabelField(setup.Description, wrap);
                    if (GUILayout.Button(setup.PerformStepButtonLabel))
                    {
                        setup.RunSetup();
                    }
                }
            }


            if (PackageInstaller.IsInstallationInProgress)
            {
                EditorGUILayout.LabelField("Installation in progress...");
            }
        }
    }

    [CustomEditor(typeof(DependencyInstaller))]
    public class DependencyInstallerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            var dependencyInstaller = target as DependencyInstaller;
            if (dependencyInstaller == null) { return; }
            DependenciesInstallerTool.DrawDependencyInstallerGUI(isWindow: false, dependencyInstaller.requirements);
        }
    }

    public static class PackageInstaller
    {
        static AddRequest? CurrentRequest;

        public static bool IsInstallationInProgress { get; private set; }

        public static void Add(string identifier)
        {
            CurrentRequest = Client.Add(identifier);
            EditorApplication.update += Progress;
            IsInstallationInProgress = true;
        }

        static void Progress()
        {
            if (CurrentRequest == null || CurrentRequest.IsCompleted)
            {
                IsInstallationInProgress = false;
                if (CurrentRequest?.Status >= StatusCode.Failure)
                {
                    Debug.LogError($"Unable to install cinemachine: {CurrentRequest.Error.message}");
                }
                EditorApplication.update -= Progress;
                CurrentRequest = null;
            }
        }
    }
}