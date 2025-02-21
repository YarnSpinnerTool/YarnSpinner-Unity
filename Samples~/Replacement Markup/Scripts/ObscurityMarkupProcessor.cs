/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Yarn.Markup;
using UnityEngine;

namespace Yarn.Unity.Samples
{
    public class ObscurityMarkupProcessor: ReplacementMarkupHandler
    {
        // matched replacement means all instances of the same character will replace with the same symbol
        public bool matchedReplacement = true;
        
        public LineProviderBehaviour lineProvider;
        
        void Start()
        {
            if (lineProvider == null)
            {
                lineProvider = GameObject.FindAnyObjectByType<LineProviderBehaviour>();
            }
            lineProvider.RegisterMarkerProcessor("obscurity", this);
        }

        private char[] replacementChars = { '?', '^', ';', '*', '&', '#', '!', '@', '<', '_' };
        public override List<LineParser.MarkupDiagnostic> ProcessReplacementMarker(MarkupAttribute marker, StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode)
        {
            // making sure we have an obscurity integer property
            if (!marker.TryGetProperty("obscurity", out int value))
            {
                var diagnostic = new LineParser.MarkupDiagnostic("Missing the obscurity property, we cannot continue without it.");
                return new List<LineParser.MarkupDiagnostic>() {diagnostic};
            }

            // we now change how much is obscured based on that property
            // where 0 is 100% obscured
            // 1 is 66% obscured
            // 2 is 25% obscured
            // all other values is not obscured at all
            switch (value)
            {
                case 0:
                {
                    // all of the line is hidden to the player
                    Obscure(childBuilder, 1);
                    break;
                }
                case 1:
                {
                    // roughly 2/3 is gibberish
                    Obscure(childBuilder, 0.67f);

                    break;
                }
                case 2:
                {
                    // roughly 25% is gibberish
                    Obscure(childBuilder, 0.25f);
                    break;
                }
            }

            return ReplacementMarkupHandler.NoDiagnostics;
        }

        void Obscure(StringBuilder builder, float obscurityPercentage)
        {
            // get all indexes of non-whitespace
            List<int> indices = new();
            for (int i = 0; i < builder.Length; i++)
            {
                if (!Char.IsWhiteSpace(builder[i]))
                {
                    indices.Add(i);
                }
            }

            // we now shuffle this list of indices
            int last = indices.Count - 1;
            for (int i = 0; i < last; i++)
            {
                var rand = UnityEngine.Random.Range(i, indices.Count);
                var temp = indices[i];
                indices[i] = indices[rand];
                indices[rand] = temp;
            }

            // now for every index that is less than the obscurity threshold we replace it
            int threshold = (int)(indices.Count * obscurityPercentage);
            for (int i = 0; i < threshold; i++)
            {
                var j = indices[i];
                if (matchedReplacement)
                {
                    builder[j] = replacementChars[builder[j] % replacementChars.Length];
                }
                else
                {
                    builder[j] = replacementChars[UnityEngine.Random.Range(0, replacementChars.Length)];
                }
            }
        }
    }
}