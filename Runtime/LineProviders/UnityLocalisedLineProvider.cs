using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

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
        [SerializeField] private LocalizedStringTable strings;
        // the runtime table we actually get our strings out of
        // this changes at runtime depending on the language
        private StringTable table;

        public override LocalizedLine GetLocalizedLine(Yarn.Line line)
        {
            var text = line.ID;
            if (table != null)
            {
                text = table[line.ID]?.LocalizedValue ?? line.ID;
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
            var loading = strings.GetTable();
            table = loading;

            // registering for any changes to the string table so we can update as needed
            strings.TableChanged += OnStringTableChanged;
        }

        // if the strings change, either by actual modifications at runtime or locale change we update our strings
        private void OnStringTableChanged(StringTable newTable)
        {
            table = newTable;
        }
#else
        public override void Start()
        {
            Debug.LogError("Unable to use the localised line provider without also including the Unity Localization package.");
        }
#endif
    }
}
