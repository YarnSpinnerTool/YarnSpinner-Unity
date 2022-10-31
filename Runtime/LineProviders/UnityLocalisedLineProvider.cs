using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

#if YARN_ENABLE_EXPERIMENTAL_FEATURES

#if USE_UNITY_LOCALIZATION
using UnityEngine.Localization.Tables;
using UnityEngine.Localization;
#endif

namespace Yarn.Unity
{
    public class UnityLocalisedLineProvider : LineProviderBehaviour
    {
        public override void PrepareForLines(IEnumerable<string> lineIDs) { } // I believe the localisation system supports loading elements piecemeal which we should take advantage of
        public override bool LinesAvailable => true; // likewise later we should check that it has actually loaded the string table

#if USE_UNITY_LOCALIZATION
        // the string table asset that has all of our (hopefully) localised strings inside
        [SerializeField] private LocalizedStringTable stringsTable;
        [SerializeField] private LocalizedAssetTable assetTable;

        // the runtime table we actually get our strings out of
        // this changes at runtime depending on the language
        private StringTable currentStringsTable;
        private AssetTable currentAssetTable;


        public override LocalizedLine GetLocalizedLine(Yarn.Line line)
        {
            var text = line.ID;
            if (currentStringsTable != null)
            {
                text = currentStringsTable[line.ID]?.LocalizedValue ?? line.ID;
            }

            return new LocalizedLine()
            {
                TextID = line.ID,
                RawText = text,
                Substitutions = line.Substitutions,
                Metadata = YarnProject.lineMetadata.GetMetadata(line.ID), // should this also use the metadata of the localisation system?
            };
        }

        public override void Start()
        {
            // doing an initial load of the strings
            if (stringsTable != null) {
                currentStringsTable = stringsTable.GetTable();
            }

            if (assetTable != null) {
                currentAssetTable = assetTable.GetTable();
            }

            // registering for any changes to the string table so we can update as needed
            stringsTable.TableChanged += OnStringTableChanged;
            assetTable.TableChanged += OnAssetTableChanged;
        }

        // if the strings change, either by actual modifications at runtime or
        // locale change we update our strings
        private void OnStringTableChanged(StringTable newTable)
        {
            currentStringsTable = newTable;
        }

        // if the assets change, either by actual modifications at runtime or
        // locale change we update our assets
        private void OnAssetTableChanged(AssetTable value)
        {
            currentAssetTable = value;
        }

#else
        public override void Start()
        {
            Debug.LogError($"{nameof(UnityLocalisedLineProvider)} requires that the Unity Localization package is installed in the project. To fix this, install Unity Localization.");
        }
        public override LocalizedLine GetLocalizedLine(Yarn.Line line)
        {
            Debug.LogError($"{nameof(UnityLocalisedLineProvider)}: Can't create a localised line for ID {line.ID} because the Unity Localization package is not installed in this project. To fix this, install Unity Localization.");
            
            return new LocalizedLine()
            {
                TextID = line.ID,
                RawText = $"{line.ID}: Unable to create a localised line, because the Unity Localization package is not installed in this project.",
                Substitutions = line.Substitutions,
            };
        }
#endif
    }
}

#endif
