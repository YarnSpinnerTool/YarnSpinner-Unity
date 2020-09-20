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
        public Dropdown textLanguagesDropdown;
        public Dropdown audioLanguagesDropdown;
        public TMP_Dropdown textLanguagesTMPDropdown;
        public TMP_Dropdown audioLanguagesTMPDropdown;

        [SerializeField] Yarn.Unity.YarnLinesAsCanvasText[] _yarnLinesCanvasTexts = default;

        int textLanguageSelected = -1;
        int audioLanguageSelected = -1;

        private void Awake()
        {
            LoadTextLanguagesIntoDropdowns();
            LoadAudioLanguagesIntoDropdowns();
        }

        private void OnEnable()
        {
            Preferences.LanguagePreferencesChanged += OnLanguagePreferencesChanged;
        }

        private void OnDisable()
        {
            Preferences.LanguagePreferencesChanged -= OnLanguagePreferencesChanged;
        }

        public void OnLanguagePreferencesChanged(object sender, System.EventArgs e)
        {
            Awake();
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

                if (ProjectSettings.TextProjectLanguages.Count > 0) {
                    foreach (var language in ProjectSettings.TextProjectLanguages) {
                    var culture = Cultures.GetCulture(language);
                    textLanguageList.Add(culture.NativeName);
                }
                } else {
                    // If no project settings have been defined, show all available cultures
                    foreach (var culture in Cultures.GetCultures()) {
                        textLanguageList.Add(culture.NativeName);
                    }
                }
                

                PopulateLanguagesListToDropdown(textLanguageList, textLanguagesTMPDropdown, textLanguagesDropdown, ref textLanguageSelected, PreferencesSetting.TextLanguage);
            }
        }

        private void LoadAudioLanguagesIntoDropdowns()
        {
            
            if (audioLanguagesDropdown || audioLanguagesTMPDropdown)
            {
                var audioLanguagesList = new List<string>();
                if (ProjectSettings.AudioProjectLanguages.Count == 0) {
                    // If no project settings have been defined, show all available cultures                    
                    foreach (var culture in Cultures.GetCultures()) {
                        audioLanguagesList.Add(culture.Name);
                    }                        
                } else {
                    foreach (var language in ProjectSettings.AudioProjectLanguages) {
                        audioLanguagesList.Add(language);
                    }
                }
                PopulateLanguagesListToDropdown(audioLanguagesList, audioLanguagesTMPDropdown, audioLanguagesDropdown, ref audioLanguageSelected, PreferencesSetting.AudioLanguage);
            }
        }

        private void PopulateLanguagesListToDropdown(List<string> languageList, TMP_Dropdown tmpDropdown, Dropdown dropdown, ref int selectedLanguageIndex, PreferencesSetting setting)
        {
            
            switch (setting)
            {
                case PreferencesSetting.TextLanguage:
                    selectedLanguageIndex = languageList.IndexOf(Preferences.TextLanguage);
                    break;
                case PreferencesSetting.AudioLanguage:
                    selectedLanguageIndex = languageList.IndexOf(Preferences.AudioLanguage);
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
                    Preferences.TextLanguage = language;
                    break;
                case PreferencesSetting.AudioLanguage:
                    Preferences.AudioLanguage = language;
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
