/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

#nullable enable

/// <summary>
/// An attribute marker processor that uses a <see cref="MarkupPalette"/> to
/// apply TextMeshPro styling tags to a line.
/// </summary>
/// <remarks>This marker processor registers itself as a handler for markers
/// whose name is equal to the name of a style in the given palette. For
/// example, if the palette defines a style named "happy", this marker processor
/// will process tags in a Yarn line named <c>[happy]</c> by inserting the
/// appropriate TextMeshProp style tags defined for the "happy" style.</remarks>
public sealed class PaletteMarkerProcessor : Yarn.Unity.ReplacementMarkupHandler
{
    /// <summary>
    /// The <see cref="MarkupPalette"/> to use when applying styles.
    /// </summary>
    [Tooltip("The MarkupPalette to use when applying styles.")]
    public MarkupPalette? palette;

    /// <summary>
    /// The line provider to register this markup processor with.
    /// </summary>
    [Tooltip("The LineProviderBehaviour to register this markup processor with.")]
    public LineProviderBehaviour? lineProvider;

    /// <inheritdoc/>
    /// <summary>
    /// Processes a replacement marker by applying the style from the given
    /// palette.
    /// </summary>
    /// <param name="marker">The marker to process.</param>
    /// <param name="childBuilder">A StringBuilder to build the styled text in.</param>
    /// <param name="childAttributes">An optional list of child attributes to
    /// apply, but this is ignored for TextMeshPro styles.</param>
    /// <param name="localeCode">The locale code to use when formatting the style.</param>
    /// <returns>A list of markup diagnostics if there are any errors, otherwise an empty list.</returns>
    public override List<LineParser.MarkupDiagnostic> ProcessReplacementMarker(MarkupAttribute marker, StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode)
    {
        if (palette == null)
        {
            return new List<LineParser.MarkupDiagnostic>() {
                new LineParser.MarkupDiagnostic($"can't apply palette for marker {marker.Name}, because palette was not set")
            };
        }

        if (palette.PaletteForMarker(marker.Name, out var format))
        {
            childBuilder.Insert(0, format.Start);
            childBuilder.Append(format.End);

            // finally we need to know if we have to offset the markers
            // most of the time we won't have to do anything
            if (format.MarkerOffset != 0)
            {
                // we now need to move any children attributes down by however many characters were added to the front
                // this is only the case if visible glyphs were added
                // as in for example adding <b> to the front doesn't add any visible glyphs so won't need to offset anything
                // and because markers are all 0-offset relative to parents
                for (int i = 0; i < childAttributes.Count; i++)
                {
                    childAttributes[i] = childAttributes[i].Shift(format.MarkerOffset);
                }
            }

            return ReplacementMarkupHandler.NoDiagnostics;
        }

        var diagnostic = new LineParser.MarkupDiagnostic($"was unable to find a matching sprite for {marker.Name}");
        return new List<LineParser.MarkupDiagnostic>() { diagnostic };
    }

    /// <summary>
    /// Called by Unity when this script is enabled to register itself with <see
    /// cref="lineProvider"/>.
    /// </summary>
    private void Start()
    {
        if (palette == null)
        {
            return;
        }

        if (palette.BasicMarkers.Count == 0)
        {
            return;
        }

        if (lineProvider == null)
        {
            lineProvider = (LineProviderBehaviour)GameObject.FindAnyObjectByType<DialogueRunner>().LineProvider;
        }

        foreach (var marker in palette.BasicMarkers)
        {
            lineProvider.RegisterMarkerProcessor(marker.Marker, this);
        }
        foreach (var marker in palette.CustomMarkers)
        {
            lineProvider.RegisterMarkerProcessor(marker.Marker, this);
        }
    }
}
