using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yarn's project wide settings that will automatically be included in a
/// build and not altered after that.
/// </summary>
[System.Serializable]
public class ProjectSettings : ScriptableObject {
    /// <summary>
    /// Project wide available text languages
    /// </summary>
    [SerializeField]
    private List<string> _textProjectLanguages = new List<string>();

    /// <summary>
    /// Project wide available text languages
    /// </summary>
    public static List<string> TextProjectLanguages => Instance._textProjectLanguages;

    /// <summary>
    /// Project wide default text language. Returns null if no default
    /// languages exists.
    /// </summary>
    public static string TextProjectLanguageDefault => TextProjectLanguages.Count > 0 ? TextProjectLanguages[0] : null;

    /// <summary>
    /// Project wide available audio voice over languages
    /// </summary>
    [SerializeField]
    private List<string> _audioProjectLanguages = new List<string>();

    /// <summary>
    /// Project wide available audio voice over languages
    /// </summary>
    public static List<string> AudioProjectLanguages => Instance._audioProjectLanguages;
    
    /// <summary>
    /// Project wide default audio language. Returns null if no default
    /// languages exists.
    /// </summary>
    public static string AudioProjectLanguageDefault => AudioProjectLanguages.Count > 0 ? AudioProjectLanguages[0] : null;

    /// <summary>
    /// Path to Yarn's project settings
    /// </summary>
    private static string _settingsPath;

    /// <summary>
    /// Instance of this class (Singleton design pattern)
    /// </summary>
    private static ProjectSettings _instance;

    /// <summary>
    /// False when VoiceOver AudioClip should be directly referenced and
    /// loaded. True (default) if the Addressable system should be used.
    /// This property is always false when the Addressable Assets package
    /// is not present.
    /// </summary>
#if ADDRESSABLES    
    public static bool AddressableVoiceOverAudioClips
    {
        get => Instance._addressableVoiceOverAudioClips; 
        set => Instance._addressableVoiceOverAudioClips = value; 
    }

    [SerializeField]
    private bool _addressableVoiceOverAudioClips = true;
#else
    // Make this property available when the addressables package is not
    // present, but make it always return false. (This means that code can
    // check for whether addressables should be used without having to wrap
    // it all in an #ifdef ADDRESSABLES block.)
    public static bool AddressableVoiceOverAudioClips => false;
#endif

    /// <summary>
    /// Makes sure that there's always an instance of this class alive upon
    /// access.
    /// </summary>
    private static ProjectSettings Instance {
        get {
            if (!_instance) {
                // Calls Awake() implicitly
                _instance = CreateInstance<ProjectSettings>();
            }
            return _instance;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("", "IDE0051", Justification = "Called from Unity/upon creaton")]
    private void Awake() {
        if (_instance != null && this != _instance) {
            DestroyImmediate(_instance);
        }
        _instance = this;

#if UNITY_EDITOR
        _settingsPath = Application.dataPath + "/../ProjectSettings/YarnProjectSettings.json";
        YarnSettingsHelper.ReadPreferencesFromDisk(this, _settingsPath, Initialize);
#else
        _settingsPath = "YarnProjectSettings";
        var jsonString = Resources.Load<TextAsset>(_settingsPath);
        var test = jsonString.text.ToString();
        if (!string.IsNullOrEmpty(test)) {
            YarnSettingsHelper.ReadJsonFromString(this, test, Initialize);
        }
#endif
    }

    private void Initialize() {
        _textProjectLanguages = new List<string>();
        _audioProjectLanguages = new List<string>();
    }

    private void OnDestroy() {
        SortAudioLanguagesList();
        WriteProjectSettingsToDisk();
    }

    /// <summary>
    /// Sort the audio languages list to match the text languages list
    /// </summary>
    private void SortAudioLanguagesList() {
        var audioLanguagesSorted = new List<string>();
        foreach (var textLanguage in _textProjectLanguages) {
            if (_audioProjectLanguages.Contains(textLanguage)) {
                audioLanguagesSorted.Add(textLanguage);
            }
        }
        _audioProjectLanguages = audioLanguagesSorted;
    }

    /// <summary>
    /// Write current Yarn project settings from memory to disk.
    /// </summary>
    public static void WriteProjectSettingsToDisk() {
        YarnSettingsHelper.WritePreferencesToDisk(Instance, _settingsPath);
    }

    /// <summary>
    /// Adds a new language to the <see cref="TextProjectLanguages"/>.
    /// </summary>
    /// <remarks>
    /// Calling this method has the same effect as opening the Yarn Spinner
    /// project settings in Unity, and adding a language there.
    /// </remarks>
    /// <param name="localeCode">The locale code for the languge to
    /// add.</param>
    /// <throws cref="ArgumentException">Thrown when <paramref
    /// name="localeCode"/> is not a valid locale code (that is, it's not
    /// known to the <see cref="Yarn.Unity.Cultures"/> class.)</throws>
    public static void AddNewTextLanguage(string localeCode) {

        if (Yarn.Unity.Cultures.HasCulture(localeCode)) {
            Instance._textProjectLanguages.Add(localeCode);
        } else {
            throw new System.ArgumentException($"{localeCode} is not a valid locale code.", localeCode);
        }
    }
}
