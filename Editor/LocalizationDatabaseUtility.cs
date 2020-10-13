using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yarn.Unity;

#if ADDRESSABLES
using UnityEditor.AddressableAssets;
#endif


internal static class LocalizationDatabaseUtility {
    /// <summary>
    /// Creates a new localization asset with the given language, and
    /// adds it to the localization database.
    /// </summary>
    /// <param name="language">The locale code for the language to add.
    /// </param>    
    internal static string CreateLocalizationWithLanguage(SerializedObject serializedObject, string theLanguage) {

        var target = serializedObject.targetObject;
        var localizationsProperty = serializedObject.FindProperty("_localizations");

        // Figure out where on disk this asset is
        var path = AssetDatabase.GetAssetPath(target);
        var directory = System.IO.Path.GetDirectoryName(path);

        // Figure out a new, unique path for the localization we're
        // creating
        var destinationPath = System.IO.Path.Combine(directory,
            $"{serializedObject.targetObject.name}-{theLanguage}.asset");
        destinationPath = AssetDatabase.GenerateUniqueAssetPath(destinationPath);

        // Create the asset and set it up
        var localizationAsset = ScriptableObject.CreateInstance<Localization>();
        localizationAsset.LocaleCode = theLanguage;

        // Save it to disk
        AssetDatabase.CreateAsset(localizationAsset, destinationPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(destinationPath);

        // Now that it exists, add it to the LocalizationDatabase's
        // _localizations field
        localizationsProperty.InsertArrayElementAtIndex(localizationsProperty.arraySize);
        var newProp = localizationsProperty.GetArrayElementAtIndex(localizationsProperty.arraySize - 1);
        newProp.objectReferenceValue = localizationAsset;

        // And we're done!
        serializedObject.ApplyModifiedProperties();

        // Finally, update this localization database so that this new
        // localization has content.
        UpdateContents(serializedObject.targetObject as LocalizationDatabase);

        // Return the path of the file we created.
        return destinationPath;
    }

    public static void UpdateContents(LocalizationDatabase database)
    {
        // First, get all scripts whose importers are configured to use
        // this database - we need to add them to our TrackedPrograms list
        
        foreach (var updatedGUID in database.RecentlyUpdatedGUIDs) {
            var path = AssetDatabase.GUIDToAssetPath(updatedGUID);

            if (string.IsNullOrEmpty(path)) {
                // The corresponding asset can't be found! No-op.
                continue;
            }

            var importer = AssetImporter.GetAtPath(path);
            if (!(importer is YarnImporter yarnImporter)) {
                Debug.LogWarning($"Yarn Spinner internal error: localization database was told to load asset {path}, but this does not have a {nameof(YarnImporter)}. Ignoring.");
                continue;
            }

            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            if (textAsset == null) {
                Debug.LogWarning($"Yarn Spinner internal error: failed to get a {nameof(TextAsset)} at {path}. Ignoring.");
                continue;
            }

            if (yarnImporter.localizationDatabase == database)
            {
                // We need to add or update content based on this asset.
                database.AddTrackedProgram(textAsset);
            }
            else
            {
                // This asset used to refer to this database, but now no
                // longer does. Remove the reference.
                database.RemoveTrackedProgram(textAsset);
            }

        }

        database.RecentlyUpdatedGUIDs.Clear();

        var allTrackedImporters = database.TrackedScripts
            .Select(p => AssetDatabase.GetAssetPath(p))
            .Select(path => AssetImporter.GetAtPath(path) as YarnImporter);

        var allLocalizationAssets = allTrackedImporters
            .Where(p => p != null)
            .SelectMany(p => p.AllLocalizations)
            .Select(localization => new
            {
                languageID = localization.languageName,
                text = localization.text
            });

        // Erase the contents of all localizations, because we're about to
        // replace them
        foreach (var localization in database.Localizations)
        {
            if (localization == null)
            {
                // Ignore any null entries
                continue;
            }
            localization.Clear();
        }

        foreach (var localizedTextAsset in allLocalizationAssets)
        {
            string languageName = localizedTextAsset.languageID;

            Localization localization;

            try
            {
                localization = database.GetLocalization(languageName);
            }
            catch (KeyNotFoundException)
            {
                Debug.LogWarning($"{localizedTextAsset.text.name} is marked for language {languageName}, but this {nameof(LocalizationDatabase)} isn't set up for that language. TODO: offer a quick way to create one here.");
                continue;
            }

            TextAsset textAsset = localizedTextAsset.text;

            if (textAsset == null)
            {
                // A null reference. Early out here.
                continue;
            }

            var records = StringTableEntry.ParseFromCSV(textAsset.text);

            foreach (var record in records)
            {
                AddLineEntryToLocalization(localization, record);
            }

            EditorUtility.SetDirty(localization);
        }

        AssetDatabase.SaveAssets();
    }

    private static void AddLineEntryToLocalization(Localization localization, StringTableEntry entry)
    {
        // Add the entry for this line's text

        try
        {
            localization.AddLocalizedString(entry.ID, entry.Text);
        }
        catch (ArgumentException)
        {
            // An ArgumentException will be thrown by the internal
            // dictionary of the Localization if there's a duplicate
            // key
            Debug.LogError($"Can't add line {entry.ID} (\"{entry.Text}\") because this localization already has an entry for this line. Are you trying to add two copies of the same file?");
            return;
        }


        if (localization.AssetSourceFolder == null)
        {
            // No asset source folder specified, so don't go looking
            // for assets to add
            return;
        }

        // Remove "line:" from the ID before looking for assets
        var id = entry.ID.Replace("line:", "");

        // Look inside assetSourceFolder for any asset that includes
        // this line ID
        var assetGUIDs = AssetDatabase.FindAssets(id, new[] { AssetDatabase.GetAssetPath(localization.AssetSourceFolder) });

        if (assetGUIDs.Length == 0)
        {
            // We didn't find any asset with this name, so early out
            // here
            return;
        }

        if (assetGUIDs.Length > 1)
        {
            string assetPaths = string.Join(", ", assetGUIDs.Select(g => AssetDatabase.GUIDToAssetPath(g)));
            Debug.LogWarning($"Multiple assets found for line {id} in language {localization.LocaleCode}: {assetPaths}");
        }

        // Select the GUID for the first asset that we've found for
        // this line
        var assetGUID = assetGUIDs[0];

        if (ProjectSettings.AddressableVoiceOverAudioClips)
        {
#if ADDRESSABLES
            // Add the asset reference
            AddAddressableAssetReferenceToLocalization(localization, entry, assetGUID);
#else
            // Something's gone wrong if we're in this situation
            throw new System.InvalidOperationException($"Internal error: {nameof(ProjectSettings.AddressableVoiceOverAudioClips)} returned true, but ADDRESSABLES was not defined. Please file a bug.");
#endif
        }
        else
        {
            // Add the asset reference directly
            AddAssetReferenceToLocalization(localization, entry, assetGUID);
        }
    }

#if ADDRESSABLES
    private static void AddAddressableAssetReferenceToLocalization(Localization localization, StringTableEntry entry, string assetGUID)
    {
        if (AddressableAssetSettingsDefaultObject.SettingsExists == false)
        {
            // Do nothing - the user hasn't set up their addressable
            // assets settings object, so we have no place to record
            // any new addresses.
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.Settings;

        // Get or create the asset reference for this asset.
        var assetReference = settings.CreateAssetReference(assetGUID);

        // Get the entry in the addressable assets settings - we want
        // to update its address to be one that a LineProvider will ask
        // for
        var addressableAssetEntry = settings.FindAssetEntry(assetGUID);

        addressableAssetEntry?.SetAddress(entry.ID + "-" + localization.LocaleCode);

        localization.AddLocalizedObjectAddress(entry.ID, assetReference);
    }
#endif

    private static void AddAssetReferenceToLocalization(Localization localization, StringTableEntry entry, string assetGUID)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(assetGUID));

        localization.AddLocalizedObject(entry.ID, asset);
    }
}
