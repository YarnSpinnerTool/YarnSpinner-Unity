using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Yarn.Unity;

namespace Yarn.Unity.Example
{
    public class MainMenuOptions : MonoBehaviour
    {
        private const string TextLanguageKey = "YarnSpinner_Demo_MainMenu_TextLanguage";
        private const string AudioLanguageKey = "YarnSpinner_Demo_MainMenu_AudioLanguage";

        public Dropdown textLanguagesDropdown;
        public Dropdown audioLanguagesDropdown;
        public TMP_Dropdown textLanguagesTMPDropdown;
        public TMP_Dropdown audioLanguagesTMPDropdown;

        [SerializeField] Yarn.Unity.YarnLinesAsCanvasText[] _yarnLinesCanvasTexts = default;

        int textLanguageSelected = -1;
        int audioLanguageSelected = -1;

        [SerializeField] YarnProject yarnProject;

        private static string TextLanguage {
            get {
                return PlayerPrefs.GetString(TextLanguageKey, null);
            }
            set {
                PlayerPrefs.SetString(TextLanguageKey, value);
            }
        }

        private static string AudioLanguage {
            get {
                return PlayerPrefs.GetString(AudioLanguageKey, null);
            }
            set {
                PlayerPrefs.SetString(AudioLanguageKey, value);
            }
        }

        private void Awake()
        {
            LoadTextLanguagesIntoDropdowns();
            LoadAudioLanguagesIntoDropdowns();
        }

        public void OnValueChangedTextLanguage(int value)
        {
            textLanguageSelected = value;
            ApplyChangedValueToPreferences(value, textLanguagesTMPDropdown, textLanguagesDropdown, PreferencesSetting.TextLanguage);

            foreach (var yarnLinesCanvasText in _yarnLinesCanvasTexts)
            {
                yarnLinesCanvasText?.OnTextLanguagePreferenceChanged();
            }
        }

        public void OnValueChangedAudioLanguage(int value)
        {
            audioLanguageSelected = value;
            ApplyChangedValueToPreferences(value, audioLanguagesTMPDropdown, audioLanguagesDropdown, PreferencesSetting.AudioLanguage);
        }

        public void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void LoadTextLanguagesIntoDropdowns()
        {
            if (textLanguagesDropdown || textLanguagesTMPDropdown)
            {
                var textLanguageList = new List<string>();

                foreach (var localization in yarnProject.localizations) {
                    var culture = Cultures.GetCulture(localization.LocaleCode);
                    textLanguageList.Add(culture.DisplayName);
                }

                PopulateLanguagesListToDropdown(textLanguageList, textLanguagesTMPDropdown, textLanguagesDropdown, ref textLanguageSelected, PreferencesSetting.TextLanguage);
            }
        }

        private void LoadAudioLanguagesIntoDropdowns()
        {
            
            if (audioLanguagesDropdown || audioLanguagesTMPDropdown)
            {
                var audioLanguageList = new List<string>();

                foreach (var localization in yarnProject.localizations) {
                    if (localization.ContainsLocalizedAssets == false) {
                        continue;
                    }

                    var culture = Cultures.GetCulture(localization.LocaleCode);
                    audioLanguageList.Add(culture.DisplayName);
                }

                PopulateLanguagesListToDropdown(audioLanguageList, textLanguagesTMPDropdown, textLanguagesDropdown, ref textLanguageSelected, PreferencesSetting.TextLanguage);
            }
        }

        private void PopulateLanguagesListToDropdown(List<string> languageList, TMP_Dropdown tmpDropdown, Dropdown dropdown, ref int selectedLanguageIndex, PreferencesSetting setting)
        {
            
            switch (setting)
            {
                case PreferencesSetting.TextLanguage:
                    selectedLanguageIndex = languageList.IndexOf(TextLanguage);
                    break;
                case PreferencesSetting.AudioLanguage:
                    selectedLanguageIndex = languageList.IndexOf(AudioLanguage);
                    break;
            }

            var displayNames = new List<string>();
            foreach (var culture in languageList) {
                displayNames.Add(Cultures.GetCulture(culture).NativeName);
            }

            if (dropdown)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(displayNames);
#if UNITY_2019_1_OR_NEWER
            dropdown.SetValueWithoutNotify(selectedLanguageIndex);
#else
                dropdown.value = selectedLanguageIndex;
#endif
            }

            if (tmpDropdown)
            {
                tmpDropdown.ClearOptions();
                tmpDropdown.AddOptions(displayNames);
#if UNITY_2019_1_OR_NEWER
            tmpDropdown.SetValueWithoutNotify(selectedLanguageIndex);
#else
                tmpDropdown.value = selectedLanguageIndex;
#endif
            }
        }

        private void ApplyChangedValueToPreferences(int value, TMP_Dropdown tmpDropdown, Dropdown dropdown, PreferencesSetting setting)
        {
            
            string language = default;

            if (dropdown)
            {
                language = Cultures.GetCultures().First(element => element.NativeName == dropdown.options[value].text).Name;
            }
            if (tmpDropdown)
            {
                language = Cultures.GetCultures().First(element => element.NativeName == tmpDropdown.options[value].text).Name;
            }

            switch (setting)
            {
                case PreferencesSetting.TextLanguage:
                    TextLanguage = language;
                    break;
                case PreferencesSetting.AudioLanguage:
                    AudioLanguage = language;
                    break;
            }
            
        }

        private enum PreferencesSetting
        {
            TextLanguage,
            AudioLanguage
        }
    }
}
