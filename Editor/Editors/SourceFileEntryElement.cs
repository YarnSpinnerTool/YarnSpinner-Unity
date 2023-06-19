
using UnityEditor;
using UnityEngine;

using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Yarn.Unity.Editor
{
    public class SourceFileEntryElement : VisualElement, INotifyValueChanged<string>
    {
        private readonly TextField sourceFileField;
        private readonly Button deleteButton;
        public event System.Action onDelete;

        public bool IsModified { get; private set; }

        public string path;

        public SourceFileEntryElement(VisualTreeAsset asset, string path, YarnProjectImporter importer)
        {
            asset.CloneTree(this);
            sourceFileField = this.Q<TextField>("sourceFile");
            deleteButton = this.Q<Button>("deleteButton");
            
            IsModified = false;

            sourceFileField.RegisterValueChangedCallback((evt) =>
            {
                IsModified = true;
                this.value = evt.newValue;
            });

            deleteButton.clicked += () => onDelete();

            SetValueWithoutNotify(path);
        }

        public string value
        {
            get => path;
            set
            {
                var previous = path;
                SetValueWithoutNotify(value);
                using (var evt = ChangeEvent<string>.GetPooled(previous, value))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
            }
        }

        public void SetValueWithoutNotify(string data)
        {
            this.path = data;
            sourceFileField.SetValueWithoutNotify(data);
        }
        
        public void ClearModified() {
            this.IsModified = false;
        }
    }
}
