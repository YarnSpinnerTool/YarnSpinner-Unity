/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Yarn.Markup;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// An attribute marker processor that inserts TextMeshPro style tags where
    /// Yarn Spinner <c>[style]</c> tags appear in a line.
    /// </summary>
    public sealed class StyleMarkerProcessor : ReplacementMarkupHandler
    {
        [SerializeField]
        public LineProviderBehaviour? lineProvider;

        /// <inheritdoc/>
        public override ReplacementMarkerResult ProcessReplacementMarker(MarkupAttribute marker, StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode)
        {
            // ok so we check if we have a property called style
            // if not give up
            if (!marker.TryGetProperty("style", out string? property))
            {
                var error = new List<LineParser.MarkupDiagnostic>
                {
                    new LineParser.MarkupDiagnostic("Unable to identify a name for the style.")
                };
                return new ReplacementMarkerResult(error, 0);
            }

            var originalLength = childBuilder.Length;

            childBuilder.Insert(0, $"<style=\"{property}\">");
            childBuilder.Append("</style>");

            // at this point we have no errors
            // but it is entirely possible that style has added visible characters
            // we have no way of knowing this
            // so if this is the case any attributes will now be off
            // unfortunately that is a downside to using the style replacement system
            // most of the time this won't be a problem
            return new ReplacementMarkerResult(childBuilder.Length - originalLength);
        }

        // Start is called before the first frame update
        void Start()
        {
            if (lineProvider == null)
            {
                var runner = DialogueRunner.FindRunner(this);
                if (runner == null)
                {
                    Debug.LogWarning("Was unable to find a dialogue runner, unable to register the style markup.");
                    return;
                }
                lineProvider = (LineProviderBehaviour)runner.LineProvider;
            }
            lineProvider.RegisterMarkerProcessor("style", this);

            // in an ideal world instead of making you write out [style = h1] you could just do [h1]
            // but TMP doesn't allow access to the list of styles, and as such also can't get their names
            // so we have no way of know what the names of any style to register them
            // alas
        }
    }
}
