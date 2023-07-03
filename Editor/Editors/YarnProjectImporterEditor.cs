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

        const string ProjectUpgradeHelpURL = "https://docs.yarnspinner.dev";

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

            if (AnyModifications) {
                if (EditorUtility.DisplayDialog("Unapplied Changes", "The currently selected Yarn Project has unapplied changes. Do you want to apply them or revert?", "Apply", "Revert")) {
                    this.ApplyAndImport();
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

            if (importData == null || importData.ImportStatus == ProjectImportData.ImportStatusCode.NeedsUpgradeFromV1) {
                ui.Add(CreateUpgradeUI(yarnProjectImporter));
                return ui;
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

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ApplyRevertGUI();
#warning Deprecated code
            return;
            serializedObject.Update();

            YarnProjectImporter yarnProjectImporter = serializedObject.targetObject as YarnProjectImporter;

            EditorGUILayout.Space();

            if (sourceScriptsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This Yarn Project has no content. Add Yarn Scripts to it.", MessageType.Warning);
            }
            EditorGUILayout.PropertyField(sourceScriptsProperty, true);

            EditorGUILayout.Space();

            bool hasCompileError = compileErrorsProperty.arraySize > 0;

            if (hasCompileError)
            {
                foreach (SerializedProperty compileError in compileErrorsProperty) {
                    EditorGUILayout.HelpBox(compileError.stringValue, MessageType.Error);
                }
            }

            serializedDeclarationsList.DrawLayout();

            // The 'Convert Implicit Declarations' feature has been
            // temporarily removed in v2.0.0-beta5.

#if false
            // If any of the serialized declarations are implicit, add a
            // button that lets you generate explicit declarations for them
            var anyImplicitDeclarations = false;
            foreach (SerializedProperty declProp in serializedDeclarationsProperty) {
                anyImplicitDeclarations |= declProp.FindPropertyRelative("isImplicit").boolValue;
            }
            
            if (hasCompileError == false && anyImplicitDeclarations) {
                if (GUILayout.Button("Convert Implicit Declarations")) {
                    // add explicit variable declarations to the file
                    YarnProjectUtility.ConvertImplicitVariableDeclarationsToExplicit(yarnProjectImporter);

                    // Return here becuase this method call will cause the
                    // YarnProgram contents to change, which confuses the
                    // SerializedObject when we're in the middle of a GUI
                    // draw call. So, stop here, and let Unity re-draw the
                    // Inspector (which it will do on the next editor tick
                    // because the item we're inspecting got re-imported.)
                    return;
                }
            }
#endif

            EditorGUILayout.PropertyField(defaultLanguageProperty, new GUIContent("Base Language"));

            CurrentProjectDefaultLanguageProperty = defaultLanguageProperty;

#if USE_UNITY_LOCALIZATION
            EditorGUILayout.PropertyField(useUnityLocalisationSystemProperty, new GUIContent("Use Unity's Built-in Localisation System"));

            // if we are using the unity localisation system we need a field to add in the string table
            // and we also disable the in-built localisation system while we are at it for its unnecessary
            if (useUnityLocalisationSystemProperty.boolValue)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(unityLocalisationTableCollectionProperty, new GUIContent("String Table Collection"));
                EditorGUI.indentLevel -= 1;
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.PropertyField(languagesToSourceAssetsProperty, new GUIContent("Localisations"));
            }
#else
            EditorGUILayout.PropertyField(languagesToSourceAssetsProperty, new GUIContent("Localisations"));
#endif


            // Determines whether or not we draw the GUI for the internal Yarn
            // localisation system.
            bool showInternalLocalizationGUI;

#if USE_UNITY_LOCALIZATION
            // If Unity Localization is available, then we draw the internal
            // localization system's UI if we've chosen not to use Unity's.
            showInternalLocalizationGUI = !useUnityLocalisationSystemProperty.boolValue;
#else
            // If Unity Localization is not available, we always draw the
            // internal localization system UI.
            showInternalLocalizationGUI = true;
#endif

            CurrentProjectDefaultLanguageProperty = null;

            // Ask the project importer if it can generate a strings table.
            // This involves querying several assets, which means various
            // exceptions might get thrown, which we'll catch and log (if
            // we're in debug mode).
            bool canGenerateStringsTable;

            try
            {
                canGenerateStringsTable = yarnProjectImporter.CanGenerateStringsTable;
            }
            catch (System.Exception e)
            {
#if YARNSPINNER_DEBUG
                Debug.LogWarning($"Encountered in error when checking to see if Yarn Project Importer could generate a strings table: {e}", this);
#else
                // Ignore the 'variable e is unused' warning
                var _ = e;
#endif
                canGenerateStringsTable = false;
            }

            if (showInternalLocalizationGUI)
            {
                // The following controls only do something useful if all of the
                // lines in the project have tags, which means the project can
                // generate a string table.
                using (new EditorGUI.DisabledScope(canGenerateStringsTable == false))
                {
#if USE_ADDRESSABLES

                    // If the addressable assets package is available, show a
                    // checkbox for using it.
                    var hasAnySourceAssetFolders = yarnProjectImporter.ImportData.localizations.Any(l => l.assetsFolder != null);
                    if (hasAnySourceAssetFolders == false)
                    {
                        // Disable this checkbox if there are no assets
                        // available.
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.Toggle(useAddressableAssetsProperty.displayName, false);
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(useAddressableAssetsProperty);

                        // Show a warning if we've requested addressables but
                        // haven't set it up.
                        if (useAddressableAssetsProperty.boolValue && AddressableAssetSettingsDefaultObject.SettingsExists == false)
                        {
                            EditorGUILayout.HelpBox("Please set up Addressable Assets in this project.", MessageType.Warning);
                        }
                    }

                    // Add a button for updating asset addresses, if any asset
                    // source folders exist
                    if (useAddressableAssetsProperty.boolValue && AddressableAssetSettingsDefaultObject.SettingsExists)
                    {
                        using (new EditorGUI.DisabledScope(hasAnySourceAssetFolders == false))
                        {
                            if (GUILayout.Button($"Update Asset Addresses"))
                            {
                                YarnProjectUtility.UpdateAssetAddresses(yarnProjectImporter);
                            }
                        }
                    }
#endif
                }
            }

            EditorGUILayout.Space();

#if YARN_USE_LEGACY_ACTIONMANAGER

            EditorGUILayout.LabelField("Commands and Functions", EditorStyles.boldLabel);

            var searchAllAssembliesLabel = new GUIContent("Search All Assemblies", "Search all assembly definitions for commands and functions, as well as code that's not in a folder with an assembly definition");
            EditorGUILayout.PropertyField(searchAllAssembliesProperty, searchAllAssembliesLabel);

            if (searchAllAssembliesProperty.boolValue == false)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(assembliesToSearchProperty);
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.PropertyField(predeterminedFunctionsProperty, true);
#endif

            if (showInternalLocalizationGUI)
            {

                using (new EditorGUI.DisabledGroupScope(canGenerateStringsTable == false))
                {
                    if (GUILayout.Button("Export Strings and Metadata as CSV"))
                    {
                        var currentPath = AssetDatabase.GetAssetPath(serializedObject.targetObject);
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
                    if (yarnProjectImporter.ImportData.localizations.Count > 0)
                    {
                        if (GUILayout.Button("Update Existing Strings Files"))
                        {
                            YarnProjectUtility.UpdateLocalizationCSVs(yarnProjectImporter);
                        }
                    }
                }
            }

            // Does this project's source scripts list contain any actual
            // assets? (It can have a count of >0 and still have no assets
            // when, for example, you've just clicked the + button but
            // haven't dragged an asset in yet.)
            var hasAnyTextAssets = yarnProjectImporter.ImportData.yarnFiles.Where(s => s != null).Count() > 0;

            // Disable this button if 1. all lines already have tags or 2.
            // no actual source files exist
            using (new EditorGUI.DisabledScope(canGenerateStringsTable == true || hasAnyTextAssets == false))
            {
                if (GUILayout.Button("Add Line Tags to Scripts"))
                {
                    YarnProjectUtility.AddLineTagsToFilesInYarnProject(yarnProjectImporter);
                }
            }

            var hadChanges = serializedObject.ApplyModifiedProperties();

#if UNITY_2018
            // Unity 2018's ApplyRevertGUI is buggy, and doesn't
            // automatically detect changes to the importer's
            // serializedObject. This means that we'd need to track the
            // state of the importer, and don't have a way to present a
            // Revert button. 
            //
            // Rather than offer a broken experience, on Unity 2018 we
            // immediately reimport the changes. This is slow (we're
            // serializing and writing the asset to disk on every property
            // change!) but ensures that the writes are done.
            if (hadChanges)
            {
                // Manually perform the same tasks as the 'Apply' button
                // would
                ApplyAndImport();
            }
#endif

#if UNITY_2019_1_OR_NEWER
            // On Unity 2019 and newer, we can use an ApplyRevertGUI that
            // works identically to the built-in importer inspectors.
            ApplyRevertGUI();
#endif
        }

        public VisualElement CreateUpgradeUI(YarnProjectImporter importer) {
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
    }
}
