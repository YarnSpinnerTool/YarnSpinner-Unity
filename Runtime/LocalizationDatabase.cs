using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace Yarn.Unity
{

    [CreateAssetMenu(fileName = "LocalizationDatabase", menuName = "Yarn Spinner/Localization Database", order = 0), HelpURL("https://yarnspinner.dev/docs/unity/localisation/")]
    public class LocalizationDatabase : ScriptableObject
    {
        [SerializeField] List<Localization> _localizations = new List<Localization>();
        public IEnumerable<Localization> Localizations => _localizations;

        /// <summary>
        /// Creates a new localization with the provided locale code.
        /// </summary>
        /// <param name="textLanguage"></param>
        /// <returns></returns>
        internal Localization CreateLocalization(string textLanguage)
        {
            var newLocalization = ScriptableObject.CreateInstance<Localization>();
            newLocalization.LocaleCode = textLanguage;
            _localizations.Add(newLocalization);
            return newLocalization;
        }

        /// <summary>
        /// Returns the list of language codes present in this localization
        /// database.
        /// </summary>
        /// <returns>A collection of language codes.</returns>
        public IEnumerable<string> GetLocalizationLanguages()
        {
            var list = new List<string>();
            foreach (var localization in _localizations)
            {
                if (localization == null)
                {
                    continue;
                }
                list.Add(localization.LocaleCode);
            }
            return list;
        }

        public bool HasLocalization(string languageCode) {
            foreach (var localization in _localizations)
            {
                if (localization.LocaleCode == languageCode)
                {
                    return true;
                }
            }
            return false;
        }

        public Localization GetLocalization(string languageCode)
        {
            foreach (var localization in _localizations)
            {
                // Ignore any null entries that may happen to be in the
                // list
                if (localization == null) {
                    continue;
                }

                if (localization.LocaleCode == languageCode)
                {
                    return localization;
                }
            }
            throw new KeyNotFoundException($"No localization available for language {languageCode}");
        }

#if UNITY_EDITOR
        // The list of YarnPrograms that supply this LocalizationDatabase
        // with line content. LocalizationDatabaseEditor uses this to
        // update this database with content.
        [SerializeField] List<TextAsset> _trackedScripts = new List<TextAsset>();

        // A list of GUIDs for YarnScripts that have recently been
        // (re-)imported, and are (or previously were) configured to use
        // this LocalizationDatabase. An AssetPostProcessor will use this
        // to update its contents, and also update the list of
        // TrackedPrograms.
        [SerializeField] List<string> _recentlyUpdatedScriptGUIDs = new List<string>();

        public bool NeedsUpdate => _recentlyUpdatedScriptGUIDs.Count > 0;

        public IEnumerable<TextAsset> TrackedScripts => _trackedScripts;
        public List<string> RecentlyUpdatedGUIDs => _recentlyUpdatedScriptGUIDs;

        public void AddTrackedProgram(string guid)
        {
            if (_recentlyUpdatedScriptGUIDs.Contains(guid) == false)
            {
                _recentlyUpdatedScriptGUIDs.Add(guid);
            }
        }

        public void AddTrackedProgram(TextAsset script)
        {
            // No-op if we already have this in the list
            if (_trackedScripts.Contains(script)) {
                return;
            }
            _trackedScripts.Add(script);
        }

        public void RemoveTrackedProgram(TextAsset script)
        {
            _trackedScripts.Remove(script);
        }

        public void RemoveTrackedProgram(string guid)
        {
            _recentlyUpdatedScriptGUIDs.Remove(guid);
        }

        public void AddLocalization(Localization localization) {
            _localizations.Add(localization);
        }

        private void OnValidate() {
            var newTrackedProgramList = new List<TextAsset>();
            foreach (var entry in _trackedScripts) {
                if (entry == null) {
                    continue;
                }
                newTrackedProgramList.Add(entry);
            }
            _trackedScripts = newTrackedProgramList;
        }
#endif
    }
}
