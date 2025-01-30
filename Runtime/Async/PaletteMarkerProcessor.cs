using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

/// <summary>
/// An attribute marker processor that uses a <see cref="MarkupPalette"/> to
/// apply TextMeshPro styling tags to a line.
/// </summary>
/// <remarks>This marker processor registers itself as a handler for markers
/// whose name is equal to the name of a style in the given palette. For
/// example, if the palette defines a style named "happy", this marker processor
/// will process tags in a Yarn line named <c>[happy]</c> by inserting the
/// appropriate TextMeshProp style tags defined for the "happy" style.</remarks>
public class PaletteMarkerProcessor : Yarn.Unity.ReplacementMarkupHandler
{
    /// <summary>
    /// The <see cref="MarkupPalette"/> to use when applying styles.
    /// </summary>
    [Tooltip("The MarkupPalette to use when applying styles.")]
    public MarkupPalette palette;

    /// <summary>
    /// The line provider to register this markup processor with.
    /// </summary>
    [Tooltip("The LineProviderBehaviour to register this markup processor with.")]
    public LineProviderBehaviour lineProvider;

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
        // get the colour for this marker
        if (!palette.FormatForMarker(marker.Name, out var style))
        {
            var error = new List<LineParser.MarkupDiagnostic>
            {
                new LineParser.MarkupDiagnostic($"Unable to identify a palette for {marker.Name}")
            };
            return error;
        }

        // do we have a custom colour set?
        if (style.CustomColor)
        {
            childBuilder.Insert(0, $"<color=#{ColorUtility.ToHtmlStringRGBA(style.Color)}>");
            childBuilder.Append("</color>");
        }

        // do we need to bold it?
        if (style.Boldened)
        {
            childBuilder.Insert(0, "<b>");
            childBuilder.Append("</b>");
        }
        // do we need to italicise it?
        if (style.Italicised)
        {
            childBuilder.Insert(0, "<i>");
            childBuilder.Append("</i>");
        }
        // do we need to underline it?
        if (style.Underlined)
        {
            childBuilder.Insert(0, "<u>");
            childBuilder.Append("</u>");
        }
        // do we need to strikethrough it?
        if (style.Strikedthrough)
        {
            childBuilder.Insert(0, "<s>");
            childBuilder.Append("</s>");
        }

        // we don't need to modify the children attributes because TMP knows
        // that the <color> tags aren't visible so we can just say we are done
        // now
        return ReplacementMarkupHandler.NoDiagnostics;
    }

    /// <summary>
    /// Called by Unity when this script is enabled to register itself with <see
    /// cref="lineProvider"/>.
    /// </summary>
    protected void Start()
    {
        if (lineProvider == null)
        {
            lineProvider = (LineProviderBehaviour)GameObject.FindAnyObjectByType<DialogueRunner>().LineProvider;
        }

        if (palette == null)
        {
            return;
        }

        foreach (var colour in palette.FormatMarkers)
        {
            lineProvider.RegisterMarkerProcessor(colour.Marker, this);
        }
    }
}
