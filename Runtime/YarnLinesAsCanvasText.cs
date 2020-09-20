using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Yarn.Unity {
    /// <summary>
    /// Shows Yarn lines on Canvas Text components.
    /// </summary>
    public class YarnLinesAsCanvasText : MonoBehaviour {
        [UnityEngine.Serialization.FormerlySerializedAs("yarnScript")]
        public YarnProgram yarnProgram;
        
        public TextAsset useLinesFromScript;

        public LocalizationDatabase localizationDatabase;

        [System.Serializable]
        public class StringObjectDictionary : SerializedDictionary<string, Object> {}

        [SerializeField] public StringObjectDictionary stringsToViews = new StringObjectDictionary();

        [SerializeField] bool _useTextMeshPro = default;

        void Start() {
            UpdateTextOnUiElements();
        }

        /// <summary>
        /// Reload the string table and update the UI elements. Useful if
        /// the languages preferences were changed.
        /// </summary>
        public void OnTextLanguagePreferenceChanged () {
            UpdateTextOnUiElements();
        }

        /// <summary>
        /// Update all UI components to the yarn lines loaded from
        /// yarnScript.
        /// </summary>
        private void UpdateTextOnUiElements() {
            var loc = localizationDatabase.GetLocalization(Preferences.TextLanguage);
            
            foreach (var line in stringsToViews) {

                var localizedString = loc.GetLocalizedString(line.Key);

                var view = line.Value;

                if (_useTextMeshPro && view is TextMeshProUGUI tmpText) {
                    tmpText.text = localizedString;
                } else if (view is UnityEngine.UI.Text text) {
                    text.text = localizedString;
                }
            }
        }
    }
}
