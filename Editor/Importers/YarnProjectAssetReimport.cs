/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Editor
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// An asset post processor that forwards any asset database changes to all
    /// YarnProjectImporter for them to verify if they need to update their
    /// locale assets.
    /// </summary>
    class YarnProjectAssetReimport : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (!YarnSpinnerProjectSettings.GetOrCreateSettings().autoRefreshLocalisedAssets)
            {
                return;
            }
            var modifiedAssets = new List<string>();
            modifiedAssets.AddRange(importedAssets);
            modifiedAssets.AddRange(deletedAssets);
            modifiedAssets.AddRange(movedAssets);
            modifiedAssets.AddRange(movedFromAssetPaths);

            var yarnProjects = AssetDatabase.FindAssets($"t:{nameof(YarnProject)}");
            foreach (var guid in yarnProjects)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as YarnProjectImporter;
                if (importer != null)
                {
                    importer.CheckUpdatedAssetsRequireReimport(modifiedAssets);
                }
            }
        }
    }
}
