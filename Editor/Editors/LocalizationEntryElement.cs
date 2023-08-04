using UnityEditor;
using UnityEngine;

using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Yarn.Unity.Editor
{
    public class LocalizationEntryElement : VisualElement, INotifyValueChanged<ProjectImportData.LocalizationEntry> {
        private readonly Foldout foldout;
        private readonly ObjectField assetFolderField;
        private readonly ObjectField stringsFileField;
        private readonly Button deleteButton;
        private readonly VisualElement stringsFileNotUsedLabel;
        private readonly PopupField<string> languagePopup;
        public event System.Action onDelete;
        ProjectImportData.LocalizationEntry data;

        public bool IsModified { get; private set; }

        private string _projectBaseLanguage;
        public string ProjectBaseLanguage {
            get => _projectBaseLanguage;
            set { 
                _projectBaseLanguage = value;
                SetValueWithoutNotify(this.data); 
            }
        }

        public LocalizationEntryElement(VisualTreeAsset asset, ProjectImportData.LocalizationEntry data, string baseLanguage) {
            asset.CloneTree(this);

            foldout = this.Q<Foldout>("foldout");
            assetFolderField = this.Q<ObjectField>("assetFolder");
            stringsFileField = this.Q<ObjectField>("stringsFile");
            deleteButton = this.Q<Button>("deleteButton");
            stringsFileNotUsedLabel = this.Q("stringsFileNotUsed");

            assetFolderField.objectType = typeof(DefaultAsset);
            stringsFileField.objectType = typeof(TextAsset);

            IsModified = false;

            // Dropdowns don't exist in Unity 2019/20(?), so we need to create
            // one at runtime and swap out a placeholder.
            var existingPopup = this.Q("languagePlaceholder");
            languagePopup = LanguagePopup.Create("Language");
            LanguagePopup.ReplaceElement(existingPopup, languagePopup);

            _projectBaseLanguage = baseLanguage;

            languagePopup.RegisterValueChangedCallback((evt) =>
            {
                IsModified = true;
                var newEntry = this.value;
                newEntry.languageID = evt.newValue;
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

            deleteButton.clicked += () => onDelete();

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
            culture = Cultures.GetCulture(data.languageID);
            
            languagePopup.SetValueWithoutNotify(data.languageID);
            assetFolderField.SetValueWithoutNotify(data.assetsFolder);
            stringsFileField.SetValueWithoutNotify(data.stringsFile);

            bool isBaseLanguage = data.languageID == ProjectBaseLanguage;

            if (isBaseLanguage) {
                stringsFileField.style.display = DisplayStyle.None;
                stringsFileNotUsedLabel.style.display = DisplayStyle.Flex;
            } else {
                stringsFileField.style.display = DisplayStyle.Flex;
                stringsFileNotUsedLabel.style.display = DisplayStyle.None;
            }

            deleteButton.SetEnabled(isBaseLanguage == false);

            foldout.text = $"{culture.DisplayName} ({culture.Name})";
        }

        internal void ClearModified()
        {
            IsModified = false;
        }
    }
}
