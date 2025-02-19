/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Yarn.Markup;

#nullable enable

namespace Yarn.Unity.Samples
{
    public class SpriteReplacmentMarkerProcessor: ReplacementMarkupHandler
    {
        public LineProviderBehaviour? lineProvider;
        public Color buff;
        public Color debuff;
        public override List<LineParser.MarkupDiagnostic> ProcessReplacementMarker(MarkupAttribute marker, StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode)
        {
            bool addedSprite = true;

            var start = "<b>[<color=#{0}><sprite=\"effects\" name=\"{1}\">";
            var end = "</color>]</b>";

            switch (marker.Name.ToLower())
            {
                case "lightning":
                    childBuilder.Insert(0, string.Format(start, ColorUtility.ToHtmlStringRGB(debuff), "lightning"));
                    childBuilder.Append(end);
                    break;
                case "ice":
                    childBuilder.Insert(0, string.Format(start, ColorUtility.ToHtmlStringRGB(buff), "water"));
                    childBuilder.Append(end);
                    break;
                case "heart":
                    childBuilder.Insert(0, string.Format(start, ColorUtility.ToHtmlStringRGB(buff), "heart"));
                    childBuilder.Append(end);
                    break;
                case "fire":
                    childBuilder.Insert(0, string.Format(start, ColorUtility.ToHtmlStringRGB(debuff), "fire"));
                    childBuilder.Append(end);
                    break;
                default:
                    addedSprite = false;
                    break;
            }

            if (!addedSprite)
            {
                var diagnostic = new LineParser.MarkupDiagnostic($"was unable to find a matching sprite for {marker.Name}");
                return new List<LineParser.MarkupDiagnostic>() { diagnostic };
            }

            // we now need to move any children attributes down by two characters
            // because we added a [ at the front and sprite
            // so all indices are now off by two and it's up to us to fix that
            for (int i = 0; i < childAttributes.Count; i++)
            {
                childAttributes[i] = childAttributes[i].Shift(2);
            }

            return ReplacementMarkupHandler.NoDiagnostics;
        }

        void Start()
        {
            if (lineProvider == null)
            {
                lineProvider = (LineProviderBehaviour)GameObject.FindAnyObjectByType<DialogueRunner>().LineProvider;
            }
            lineProvider.RegisterMarkerProcessor("lightning", this);
            lineProvider.RegisterMarkerProcessor("ice", this);
            lineProvider.RegisterMarkerProcessor("heart", this);
            lineProvider.RegisterMarkerProcessor("fire", this);
        }
    }
}