using System.Collections.Generic;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine;
using System.Linq;
using Yarn.Compiler;
using System.IO;
using UnityEditorInternal;
using System.Collections;
using System.Reflection;

#if USE_ADDRESSABLES
using UnityEditor.AddressableAssets;
#endif

#if USE_UNITY_LOCALIZATION
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;
#endif

using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Yarn.Unity.Editor
{

    [CustomEditor(typeof(YarnProjectImporter))]
    public class YarnProjectImporterEditor : ScriptedImporterEditor
    {
        // A runtime-only field that stores the defaultLanguage of the
        // YarnProjectImporter. Used during Inspector GUI drawing.
        internal static SerializedProperty CurrentProjectDefaultLanguageProperty;

        const string ProjectUpgradeHelpURL = "https://docs.yarnspinner.dev/using-yarnspinner-with-unity/importing-yarn-files/yarn-projects#upgrading-yarn-projects";
        const string CreateNewIssueURL = "https://github.com/YarnSpinnerTool/YarnSpinner-Unity/issues/new?assignees=&labels=bug&projects=&template=bug_report.md&title=Project Import Error";

        private SerializedProperty compileErrorsProperty;
        private SerializedProperty serializedDeclarationsProperty;
        private SerializedProperty defaultLanguageProperty;
        private SerializedProperty sourceScriptsProperty;
        private SerializedProperty languagesToSourceAssetsProperty;
        private SerializedProperty useAddressableAssetsProperty;

        private ReorderableDeclarationsList serializedDeclarationsList;

        private SerializedProperty useUnityLocalisationSystemProperty;
        private SerializedProperty unityLocalisationTableCollectionProperty;

        public VisualTreeAsset editorUI;
        public VisualTreeAsset localizationUIAsset;
        public VisualTreeAsset sourceFileUIAsset;
        public StyleSheet yarnProjectStyleSheet;

        private string baseLanguage = null;
        private List<LocalizationEntryElement> localizationEntryFields = new List<LocalizationEntryElement>();
        private List<SourceFileEntryElement> sourceEntryFields = new List<SourceFileEntryElement>();
        
        private VisualElement localisationFieldsContainer;
        private VisualElement sourceFileEntriesContainer;

        private bool AnyModifications
        {
            get {
                return AnyLocalisationModifications
                || AnySourceFileModifications
                ;
            }
        }

        private bool AnyLocalisationModifications => LocalisationsAddedOrRemoved || localizationEntryFields.Any(f => f.IsModified) || BaseLanguageNameModified;
        
        private bool AnySourceFileModifications => SourceFilesAddedOrRemoved || sourceEntryFields.Any(f => f.IsModified);

        private bool LocalisationsAddedOrRemoved = false;
        private bool BaseLanguageNameModified = false;
        private bool SourceFilesAddedOrRemoved = false;

        public override void OnEnable()
        {
            base.OnEnable();

            useAddressableAssetsProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.useAddressableAssets));

#if USE_UNITY_LOCALIZATION
            useUnityLocalisationSystemProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.UseUnityLocalisationSystem));
            unityLocalisationTableCollectionProperty = serializedObject.FindProperty(nameof(YarnProjectImporter.unityLocalisationStringTableCollection));
#endif
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (AnyModifications)
            {
                if (EditorUtility.DisplayDialog("Unapplied Changes", "The currently selected Yarn Project has unapplied changes. Do you want to apply them or revert?", "Apply", "Revert"))
                {
#if UNITY_2022_2_OR_NEWER
                    this.SaveChanges();
#else
                    this.ApplyAndImport();
#endif
                }
            }
        }

        protected override void Apply() {
            base.Apply();

            var importer = (this.target as YarnProjectImporter);
            var data = importer.GetProject();
            var importerFolder = Path.GetDirectoryName(importer.assetPath);

            var removedLocalisations = data.Localisation.Keys.Except(localizationEntryFields.Select(f => f.value.languageID)).ToList();

            foreach (var removedLocalisation in removedLocalisations) {
                data.Localisation.Remove(removedLocalisation);
            }

            data.SourceFilePatterns = this.sourceEntryFields.Select(f => f.value);

            foreach (var locField in localizationEntryFields) {

                // Does this localisation field represent a localisation that
                // used to be the base localisation, but is now no longer? If it
                // is, even if it's unmodified, we need to make sure we add it
                // to the project data.
                bool wasPreviouslyBaseLocalisation = 
                    locField.value.languageID == data.BaseLanguage 
                    && BaseLanguageNameModified;

                // Skip any records that we don't need to touch
                if (locField.IsModified == false && !wasPreviouslyBaseLocalisation) {
                    continue;
                }

                var stringFile = locField.value.stringsFile;
                var assetFolder = locField.value.assetsFolder;

                var locInfo = new Project.LocalizationInfo();

                if (stringFile != null) {
                    string stringFilePath = AssetDatabase.GetAssetPath(stringFile);
#if UNITY_2021
                    locInfo.Strings = Path.GetRelativePath(importerFolder, stringFilePath);
#else
                    locInfo.Strings = YarnProjectImporter.UnityProjectRootVariable + "/" + stringFilePath;
#endif
                }
                if (assetFolder != null) {
                    string assetFolderPath = AssetDatabase.GetAssetPath(assetFolder);
#if UNITY_2021
                    locInfo.Assets = Path.GetRelativePath(importerFolder, assetFolderPath);
#else
                    locInfo.Assets = YarnProjectImporter.UnityProjectRootVariable + "/" + assetFolderPath;
#endif
                }

                data.Localisation[locField.value.languageID] = locInfo;
                locField.ClearModified();
            }

            data.BaseLanguage = this.baseLanguage;

            if (data.Localisation.TryGetValue(data.BaseLanguage, out var baseLanguageInfo)) {
                if (string.IsNullOrEmpty(baseLanguageInfo.Strings)
                    && string.IsNullOrEmpty(baseLanguageInfo.Assets)) {
                    // We have a localisation info entry, but it doesn't provide
                    // any useful information (the strings field is unused, and
                    // the assets field defaults to empty anyway). Trim it from
                    // the data.
                    data.Localisation.Remove(data.BaseLanguage);
                }
            }

            foreach (var sourceField in this.sourceEntryFields) {
                sourceField.ClearModified();
            }

            BaseLanguageNameModified = false;
            SourceFilesAddedOrRemoved = false;
            LocalisationsAddedOrRemoved = false;

            data.SaveToFile(importer.assetPath);

            if (localizationEntryFields.Any(f => f.value.languageID == this.baseLanguage) == false) {
                var newBaseLanguageField = CreateLocalisationEntryElement(new ProjectImportData.LocalizationEntry
                {
                    assetsFolder = null,
                    stringsFile = null,
                    languageID = baseLanguage,
                }, baseLanguage);
                localizationEntryFields.Add(newBaseLanguageField);
                localisationFieldsContainer.Add(newBaseLanguageField);
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            YarnProjectImporter yarnProjectImporter = target as YarnProjectImporter;
            var importData = yarnProjectImporter.ImportData;
            
            var ui = new VisualElement();

            ui.styleSheets.Add(yarnProjectStyleSheet);

            // if the import data is null it means import has crashed
            // we need to let the user know and perhaps ask them to file an issue
            if (importData == null)
            {
                ui.Add(CreateCriticalErrorUI());
                return ui;
            }

            // next we need to handle the two edge cases
            // either the importData is for the older format
            // or it's completely unknown (most likely a error)
            // in both cases we show the respective custom UI and return
            switch (importData.ImportStatus)
            {
                case ProjectImportData.ImportStatusCode.NeedsUpgradeFromV1:
                {
                    ui.Add(CreateUpgradeUI(yarnProjectImporter));
                    return ui;
                }
                case ProjectImportData.ImportStatusCode.Unknown:
                {
                    ui.Add(CreateUnknownErrorUI());
                    return ui;
                }
            }

            var importDataSO = new SerializedObject(importData);
            var diagnosticsProperty = importDataSO.FindProperty(nameof(ProjectImportData.diagnostics));

            var errorsContainer = new VisualElement();
            errorsContainer.name = "errors";

            var sourceFilesContainer = new VisualElement();

            var localisationControls = new VisualElement();

            var yarnInternalControls = new VisualElement();

            var unityControls = new VisualElement();

            var useAddressableAssetsField = new PropertyField(useAddressableAssetsProperty);

#if USE_UNITY_LOCALIZATION
            var useUnityLocalisationSystemField = new PropertyField(useUnityLocalisationSystemProperty);
            var unityLocalisationTableCollectionField = new PropertyField(unityLocalisationTableCollectionProperty);
#endif

            localisationFieldsContainer = new VisualElement();
            sourceFileEntriesContainer = new VisualElement();

            localisationControls.style.marginBottom = 8;

            if (importData.diagnostics.Any())
            {
                var header = new Label();
                header.text = "Errors";
                header.style.unityFontStyleAndWeight = FontStyle.Bold;

                errorsContainer.Add(header);

                foreach (var error in importData.diagnostics)
                {
                    var errorContainer = new VisualElement();
                    errorContainer.style.marginLeft = 15;

                    var objectField = new ObjectField();
                    objectField.value = error.yarnFile;
                    objectField.objectType = typeof(TextAsset);

                    var messagesField = new IMGUIContainer(() =>
                    {
                        foreach (var message in error.errorMessages)
                        {
                            EditorGUILayout.HelpBox(message, MessageType.Error);
                        }
                    });
                    messagesField.style.marginLeft = 30;

                    errorContainer.Add(objectField);
                    errorContainer.Add(messagesField);

                    errorsContainer.Add(errorContainer);
                }

                errorsContainer.style.marginBottom = 15;

                ui.Add(errorsContainer);
            }

            sourceFileEntriesContainer.style.marginLeft = 8;

            ui.Add(sourceFilesContainer);
            var sourceFilesHeader = new Label();
            sourceFilesHeader.text = "Source Files";
            sourceFilesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            sourceFilesContainer.Add(sourceFilesHeader);
            sourceFilesContainer.Add(sourceFileEntriesContainer);

            foreach (var path in importData.sourceFilePaths) {
                var locElement = CreateSourceFileEntryElement(path);
                sourceFileEntriesContainer.Add(locElement);
                sourceEntryFields.Add(locElement);
            }

            var addSourceFileButton = new Button();
            addSourceFileButton.text = "Add";
            sourceFilesContainer.Add(addSourceFileButton);
            addSourceFileButton.style.marginLeft = 8;
            addSourceFileButton.clicked += () =>
            {
                var loc = CreateSourceFileEntryElement("**/*.yarn");
                sourceEntryFields.Add(loc);
                sourceFileEntriesContainer.Add(loc);
                SourceFilesAddedOrRemoved = true;
            };

            var localisationHeader = new Label();

            localisationHeader.text = "Localisation";
            localisationHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            localisationHeader.style.marginTop = 8;

            localisationControls.Add(localisationHeader);

            var languagePopup = LanguagePopup.Create("Base Language");
            var generateStringsFileButton = new Button();
            var addStringTagsButton = new Button();
            var updateExistingStringsFilesButton = new Button();

            baseLanguage = importData.baseLanguageName;

            languagePopup.SetValueWithoutNotify(baseLanguage);
            languagePopup.RegisterValueChangedCallback(evt =>
            {
                baseLanguage = evt.newValue;
                foreach (var loc in localizationEntryFields) {
                    loc.ProjectBaseLanguage = baseLanguage;
                }
                BaseLanguageNameModified = true;
            });

            localisationControls.Add(languagePopup);

            yarnInternalControls.Add(useAddressableAssetsField);

            yarnInternalControls.Add(localisationFieldsContainer);

            foreach (var localisation in importData.localizations) {
                var locElement = CreateLocalisationEntryElement(localisation, baseLanguage);
                localisationFieldsContainer.Add(locElement);
                localizationEntryFields.Add(locElement);
            }

            var addLocalisationButton = new Button();
            addLocalisationButton.text = "Add Localisation";
            addLocalisationButton.clicked += () =>
            {
                var loc = CreateLocalisationEntryElement(new ProjectImportData.LocalizationEntry() {
                    languageID = importData.baseLanguageName,
                }, baseLanguage);
                localizationEntryFields.Add(loc);
                localisationFieldsContainer.Add(loc);
                LocalisationsAddedOrRemoved = true;

            };
            yarnInternalControls.Add(addLocalisationButton);

#if USE_ADDRESSABLES
            var updateAddressableAssetsButton = new Button();
            updateAddressableAssetsButton.text = "Update Asset Addresses";
            updateAddressableAssetsButton.clicked += () =>
            {
                YarnProjectUtility.UpdateAssetAddresses(yarnProjectImporter);
            };
            yarnInternalControls.Add(updateAddressableAssetsButton);

            bool ShouldShowAssetsButton() => yarnProjectImporter.ImportData.localizations.Any(l => l.assetsFolder != null) && AddressableAssetSettingsDefaultObject.SettingsExists && useAddressableAssetsProperty.boolValue;

            SetElementVisible(updateAddressableAssetsButton, ShouldShowAssetsButton());

            useAddressableAssetsField.RegisterCallback<ChangeEvent<bool>>(evt => 
            {
                SetElementVisible(updateAddressableAssetsButton, ShouldShowAssetsButton());
            });
#endif

#if USE_UNITY_LOCALIZATION

            localisationControls.Add(useUnityLocalisationSystemField);

            unityControls.Add(unityLocalisationTableCollectionField);

            SetElementVisible(unityControls, useUnityLocalisationSystemProperty.boolValue);
            SetElementVisible(yarnInternalControls, !useUnityLocalisationSystemProperty.boolValue);

            useUnityLocalisationSystemField.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                SetElementVisible(unityControls, useUnityLocalisationSystemProperty.boolValue);
                SetElementVisible(yarnInternalControls, !useUnityLocalisationSystemProperty.boolValue);
            });
#endif

            var cantGenerateUnityStringTableMessage = new IMGUIContainer(() =>
            {
                EditorGUILayout.HelpBox("All lines must have a line ID tag in order to create a string table. Click 'Add Line Tags to Scripts' to fix this problem.", MessageType.Warning);
            });

            addStringTagsButton.text = "Add Line Tags to Scripts";
            addStringTagsButton.clicked += () =>
            {
                YarnProjectUtility.AddLineTagsToFilesInYarnProject(yarnProjectImporter);
                UpdateTaggingButtonsEnabled();
            };

            generateStringsFileButton.text = "Export Strings and Metadata as CSV";

            generateStringsFileButton.clicked += () => ExportStringsData(yarnProjectImporter);

            updateExistingStringsFilesButton.text = "Update Existing Strings Files";
            updateExistingStringsFilesButton.clicked += () =>
            {
                YarnProjectUtility.UpdateLocalizationCSVs(yarnProjectImporter);
            };
            yarnInternalControls.Add(updateExistingStringsFilesButton);

            localisationControls.Add(yarnInternalControls);
            localisationControls.Add(unityControls);

            ui.Add(localisationControls);

            ui.Add(cantGenerateUnityStringTableMessage);

            ui.Add(addStringTagsButton);
            ui.Add(generateStringsFileButton);


            ui.Add(new IMGUIContainer(ApplyRevertGUI));

            UpdateTaggingButtonsEnabled();

            return ui;

            void UpdateTaggingButtonsEnabled()
            {
                addStringTagsButton.SetEnabled(yarnProjectImporter.CanGenerateStringsTable == false && importData.yarnFiles.Any());
                generateStringsFileButton.SetEnabled(yarnProjectImporter.CanGenerateStringsTable);

                bool isUnityLocalisationAvailable;
#if USE_UNITY_LOCALIZATION
                isUnityLocalisationAvailable = true;
#else
                isUnityLocalisationAvailable = false;
#endif

                cantGenerateUnityStringTableMessage.style.display = (isUnityLocalisationAvailable && yarnProjectImporter.HasErrors == false && yarnProjectImporter.CanGenerateStringsTable == false) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private LocalizationEntryElement CreateLocalisationEntryElement(ProjectImportData.LocalizationEntry localisation, string baseLanguage)
        {
            var locElement = new LocalizationEntryElement(localizationUIAsset, localisation, baseLanguage);
            locElement.onDelete += () =>
            {
                locElement.RemoveFromHierarchy();
                localizationEntryFields.Remove(locElement);
                LocalisationsAddedOrRemoved = true;
            };
            return locElement;
        }

        private SourceFileEntryElement CreateSourceFileEntryElement(string path) {
            var sourceElement = new SourceFileEntryElement(sourceFileUIAsset, path, this.target as YarnProjectImporter);
            sourceElement.onDelete += () =>
            {
                sourceElement.RemoveFromHierarchy();
                sourceEntryFields.Remove(sourceElement);
                SourceFilesAddedOrRemoved = true;
            };
            return sourceElement;
        }

        
        private void ExportStringsData(YarnProjectImporter yarnProjectImporter)
        {
            var currentPath = AssetDatabase.GetAssetPath(yarnProjectImporter);
            var currentFileName = Path.GetFileNameWithoutExtension(currentPath);
            var currentDirectory = Path.GetDirectoryName(currentPath);

            var destinationPath = EditorUtility.SaveFilePanel("Export Strings CSV", currentDirectory, $"{currentFileName}.csv", "csv");

            if (string.IsNullOrEmpty(destinationPath) == false)
            {
                // Generate the file on disk
                YarnProjectUtility.WriteStringsFile(destinationPath, yarnProjectImporter);

                // Also generate the metadata file.
                var destinationDirectory = Path.GetDirectoryName(destinationPath);
                var destinationFileName = Path.GetFileNameWithoutExtension(destinationPath);

                var metadataDestinationPath = Path.Combine(destinationDirectory, $"{destinationFileName}-metadata.csv");
                YarnProjectUtility.WriteMetadataFile(metadataDestinationPath, yarnProjectImporter);

                // destinationPath may have been inside our Assets
                // directory, so refresh the asset database
                AssetDatabase.Refresh();

            }
        }

        private static void SetElementVisible(VisualElement e, bool visible) {
            if (visible) {
                e.style.display = DisplayStyle.Flex;
            } else {
                e.style.display = DisplayStyle.None;

            }
        }

        public override bool HasModified()
        {
            return base.HasModified() || AnyModifications;
        }
        
        private VisualElement CreateUpgradeUI(YarnProjectImporter importer)
        {
            var ui = new VisualElement();

            var box = new VisualElement();
            box.AddToClassList("help-box");

            Label header = new Label("This project needs to be upgraded.");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            box.Add(header);
            box.Add(new Label("After upgrading, you will need to update your localisations, and ensure that all of the Yarn scripts for this project are in the same folder as the project."));
            box.Add(new Label("Your Yarn scripts will not be modified."));

            var learnMoreLink = new Label("Learn more...");
            learnMoreLink.RegisterCallback<MouseDownEvent>(evt =>
            {
                Application.OpenURL(ProjectUpgradeHelpURL);
            });
            learnMoreLink.AddToClassList("link");
            box.Add(learnMoreLink);

            var upgradeButton = new Button(() =>
            {
                YarnProjectUtility.UpgradeYarnProject(importer);

                // Reload the entire inspector - we will have changed the
                // project significantly
                ActiveEditorTracker.sharedTracker.ForceRebuild();
            });
            upgradeButton.text = "Upgrade Yarn Project";
            box.Add(upgradeButton);

            ui.Add(box);

            ui.Add(new IMGUIContainer(ApplyRevertGUI));

            return ui;
        }

        private VisualElement CreateErrorUI(string headerText, string[] labels, string linkLabel, string link)
        {
            var ui = new VisualElement();
            var box = new VisualElement();
            box.AddToClassList("help-box");

            Label header = new Label(headerText);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            box.Add(header);

            foreach (var label in labels)
            {
                box.Add(new Label(label));
            }

            var learnMoreLink = new Label(linkLabel);
            learnMoreLink.RegisterCallback<MouseDownEvent>(evt =>
            {
                Application.OpenURL(link);
            });
            learnMoreLink.AddToClassList("link");
            box.Add(learnMoreLink);

            ui.Add(box);

            ui.Add(new IMGUIContainer(ApplyRevertGUI));

            return ui;
        }

        private VisualElement CreateCriticalErrorUI()
        {
            string[] labels = { 
                "This is likely due to a bug on our end, and not in your project.",
                "Try recreating the project and see if this resolves the issue."
            };

            return CreateErrorUI("This project has failed to import due to an internal error.", labels, "If the issue persists, please open an issue.", CreateNewIssueURL);
        }

        private VisualElement CreateUnknownErrorUI()
        {
            string[] labels = { 
                "The type of this Yarn Project is unknown.",
                "Try recreating the project and see if this resolves the issue."
            };

            return CreateErrorUI("This project has failed to import correctly.", labels, "If the issue persists, please open an issue.", CreateNewIssueURL);
        }
    }
}
