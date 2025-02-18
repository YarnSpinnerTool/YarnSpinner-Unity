/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#if USE_UNITY_LOCALIZATION

using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;

#nullable enable

namespace Yarn.Unity.Editor
{
    /// <summary>
    /// An asset post processor that updates Unity Localization string tables
    /// when a Yarn Project that uses them is imported.
    /// </summary>
    /// <remarks>
    /// Due to a bug in Unity, <see cref="ScriptedImporter"/> objects aren't
    /// able to interact with ScriptableObject instances during import, and
    /// Unity string tables are scriptable objects. To work around this, we
    /// update the string table after import is complete, using a
    /// post-processor.
    /// </remarks>
    internal class YarnProjectUnityLocalizationUpdater : AssetPostprocessor
    {
        [RunAfterPackage("com.unity.localization")]
        public static void OnPostprocessAllAssets(string[] importedAssets,
                                                  string[] deletedAssets,
                                                  string[] movedAssets,
                                                  string[] movedFromAssetPaths,
                                                  bool didDomainReload)
        {
            // Get all importers for Yarn projects that were just imported
            var importedYarnProjectAssets = importedAssets
                .Select(path => AssetImporter.GetAtPath(path))
                .OfType<YarnProjectImporter>();

            foreach (var importer in importedYarnProjectAssets)
            {
                // If the importer uses Unity Localization, get it to update its
                // table
                if (importer.UseUnityLocalisationSystem)
                {
                    importer.AddStringsToUnityLocalization();
                }
            }
        }
    }
}
#endif
