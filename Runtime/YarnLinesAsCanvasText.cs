using System.Collections.Generic;
using UnityEngine;
#if USE_TMP
using TMPro;
#endif

namespace Yarn.Unity
{
    /// <summary>
    /// Shows Yarn lines on Canvas Text components.
    /// </summary>
    public class YarnLinesAsCanvasText : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("yarnScript")]
        [UnityEngine.Serialization.FormerlySerializedAs("yarnProgram")]
        public YarnProject yarnProject;

        [System.Serializable]
        public class StringObjectDictionary : SerializedDictionary<string, Object> { }

        [SerializeField] public StringObjectDictionary stringsToViews = new StringObjectDictionary();

#if USE_TMP
        [SerializeField] bool _useTextMeshPro = default;
#endif

        void Start()
        {
            UpdateTextOnUiElements();
        }

        /// <summary>
        /// Reload the string table and update the UI elements. Useful if
        /// the languages preferences were changed.
        /// </summary>
        public void OnTextLanguagePreferenceChanged()
        {
            UpdateTextOnUiElements();
        }

        /// <summary>
        /// Update all UI components to the yarn lines loaded from
        /// yarnScript.
        /// </summary>
        private void UpdateTextOnUiElements()
        {

            foreach (var line in stringsToViews)
            {

                throw new System.NotImplementedException("Not implemented yet");

                // var localizedString =
                // yarnProject.GetLocalizedString(line.Key);

                // var view = line.Value;

                // When reimplementing, handle TMP as an optional dependency with #if USE_TMP (it's become optional due to 2023.2.0a14+ versions merging it with ugui)
                // if (_useTextMeshPro && view is TextMeshProUGUI tmpText)
                //     {tmpText.text = localizedString;} else if (view is
                //     UnityEngine.UI.Text text) {text.text =
                //     localizedString;
                // }
            }
        }
    }
}
