/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Yarn.Unity.Editor
{
    public class LocalizationEntryElement : VisualElement, INotifyValueChanged<ProjectImportData.LocalizationEntry>
    {
        private readonly Foldout foldout;
        private readonly ObjectField assetFolderField;
        private readonly ObjectField stringsFileField;
        private readonly Button deleteButton;
        private readonly VisualElement stringsFileNotUsedLabel;
        private readonly LanguageField languagePopup;
        private readonly Toggle isExternalAssetToggle;
        private readonly ObjectField externalLocalisationAssetField;
        private readonly VisualElement externalReferenceFields;
        private readonly VisualElement internallyGeneratedAssetFields;
        public event System.Action? OnDelete;
        ProjectImportData.LocalizationEntry data;

        public bool IsModified { get; private set; }

        private string _projectBaseLanguage;
        public string ProjectBaseLanguage
        {
            get => _projectBaseLanguage;
            set
            {
                _projectBaseLanguage = value;
                SetValueWithoutNotify(this.data);
            }
        }

        public LocalizationEntryElement(VisualTreeAsset asset, ProjectImportData.LocalizationEntry data, string baseLanguage)
        {
            asset.CloneTree(this);

            foldout = this.Q<Foldout>("foldout");
            assetFolderField = this.Q<ObjectField>("assetFolder");
            stringsFileField = this.Q<ObjectField>("stringsFile");
            deleteButton = this.Q<Button>("deleteButton");
            stringsFileNotUsedLabel = this.Q("stringsFileNotUsed");
            isExternalAssetToggle = this.Q<Toggle>("isExternalAsset");
            externalLocalisationAssetField = this.Q<ObjectField>("externalLocalisationAsset");
            externalReferenceFields = this.Q<VisualElement>("externalReferenceFields");
            internallyGeneratedAssetFields = this.Q<VisualElement>("internallyGeneratedAssetFields");

            assetFolderField.objectType = typeof(DefaultAsset);
            stringsFileField.objectType = typeof(TextAsset);

            IsModified = false;

            // Dropdowns don't exist in Unity 2019/20(?), so we need to create
            // one at runtime and swap out a placeholder.
            var existingPopup = this.Q("languagePlaceholder");
            languagePopup = new LanguageField("Language");
            LanguagePopup.ReplaceElement(existingPopup, languagePopup);

            _projectBaseLanguage = baseLanguage;


            languagePopup.RegisterValueChangedCallback((evt) =>
            {
                IsModified = true;
                var newEntry = this.value;
                newEntry.languageID = evt.newValue;
                this.value = newEntry;
            });

            isExternalAssetToggle.RegisterValueChangedCallback((evt) =>
            {
                IsModified = true;
                var newEntry = this.value;
                newEntry.isExternal = evt.newValue;
                this.value = newEntry;
            });
            externalLocalisationAssetField.RegisterValueChangedCallback((evt) =>
            {
                IsModified = true;
                var newEntry = this.value;
                newEntry.externalLocalization = evt.newValue as Localization;
                this.value = newEntry;
            });


            stringsFileField.RegisterValueChangedCallback((evt) =>
            {
                IsModified = true;
                var newEntry = this.value;
                newEntry.stringsFile = evt.newValue as TextAsset;
                this.value = newEntry;
            });
            assetFolderField.RegisterValueChangedCallback((evt) =>
            {
                IsModified = true;
                var newEntry = this.value;
                newEntry.assetsFolder = evt.newValue as DefaultAsset;
                this.value = newEntry;
            });

            deleteButton.clicked += () => OnDelete?.Invoke();

            SetValueWithoutNotify(data);

        }

        public ProjectImportData.LocalizationEntry value
        {
            get => data;
            set
            {
                var previous = data;
                SetValueWithoutNotify(value);
                using (var evt = ChangeEvent<ProjectImportData.LocalizationEntry>.GetPooled(previous, value))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
            }
        }

        public void SetValueWithoutNotify(ProjectImportData.LocalizationEntry data)
        {
            this.data = data;
            Culture culture;
            var foundCulture = Cultures.TryGetCulture(data.languageID, out culture);

            string foldoutDisplayName = foundCulture ? $"{culture.DisplayName} ({culture.Name})" : $"{data.languageID}";

            languagePopup.SetValueWithoutNotify(data.languageID);
            assetFolderField.SetValueWithoutNotify(data.assetsFolder);
            stringsFileField.SetValueWithoutNotify(data.stringsFile);
            isExternalAssetToggle.SetValueWithoutNotify(data.isExternal);
            externalLocalisationAssetField.SetValueWithoutNotify(data.externalLocalization);

            bool isBaseLanguage = data.languageID == ProjectBaseLanguage;

            if (isBaseLanguage)
            {
                stringsFileField.style.display = DisplayStyle.None;
                stringsFileNotUsedLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                stringsFileField.style.display = DisplayStyle.Flex;
                stringsFileNotUsedLabel.style.display = DisplayStyle.None;
            }

            internallyGeneratedAssetFields.style.display = data.isExternal ? DisplayStyle.None : DisplayStyle.Flex;
            externalReferenceFields.style.display = data.isExternal ? DisplayStyle.Flex : DisplayStyle.None;

            deleteButton.SetEnabled(isBaseLanguage == false);

            foldout.text = foldoutDisplayName;
        }

        internal void ClearModified()
        {
            IsModified = false;
        }
    }
}
