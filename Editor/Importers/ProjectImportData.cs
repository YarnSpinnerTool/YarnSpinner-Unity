using System.Collections.Generic;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine;
using System.Linq;

namespace Yarn.Unity
{
    public class ProjectImportData : ScriptableObject {
        public List<Editor.YarnProjectImporter.SerializedDeclaration> serializedDeclarations = new List<Editor.YarnProjectImporter.SerializedDeclaration>();

        public bool HasCompileErrors => diagnostics.Count() > 0;

        public bool containsImplicitLineIDs = false;

        public List<TextAsset> yarnFiles = new List<TextAsset>();

        [System.Serializable]
        public struct LocalizationEntry {
            public string languageID;
            public DefaultAsset assetsFolder;
            public TextAsset stringsFile;
        }

        [System.Serializable]
        public struct DiagnosticEntry {
            public TextAsset yarnFile;
            public List<string> errorMessages;
        }

        public enum ImportStatusCode {
            Unknown = 0,
            Succeeded = 1,
            CompilationFailed = 2,
            NeedsUpgradeFromV1 = 3,
        }

        public ImportStatusCode ImportStatus = ImportStatusCode.Unknown;

        public List<DiagnosticEntry> diagnostics = new List<DiagnosticEntry>();

        public List<string> sourceFilePaths = new List<string>();

        public List<LocalizationEntry> localizations = new List<LocalizationEntry>();

        public string baseLanguageName;

        public LocalizationEntry BaseLocalizationEntry
        {
            get
            {
                try {
                    return localizations.First(l => l.languageID == baseLanguageName);
                } catch (System.Exception e) {
                    throw new System.InvalidOperationException("Project import data has no base localisation", e);
                }
            }
        }

        public bool TryGetLocalizationEntry(string languageID, out LocalizationEntry result) {
            foreach (var loc in this.localizations) {
                if (loc.languageID == languageID) {
                    result = loc;
                    return true;
                }
            }
            result = default;
            return false;
        }
    }
}
