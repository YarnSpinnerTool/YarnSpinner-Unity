using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;
using System.Text;

public class PaletteMarkerProcessor : Yarn.Unity.AttributeMarkerProcessor
{
    public MarkupPalette palette;
    public LineProviderBehaviour lineProvider;

    public override List<LineParser.MarkupDiagnostic> ProcessReplacementMarker(MarkupAttribute marker, StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode)
    {
        // get the colour for this marker
        if (!palette.PaletteForMarker(marker.Name, out var style))
        {
            var error = new List<LineParser.MarkupDiagnostic>
            {
                new LineParser.MarkupDiagnostic($"Unable to identify a palette for {marker.Name}")
            };
            return error;
        }

        // we will always add the colour because we don't really know what the default is anyways
        childBuilder.Insert(0, $"<color=#{ColorUtility.ToHtmlStringRGBA(style.Color)}>");
        childBuilder.Append("</color>");

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

        // we don't need to modify the children attributes because TMP knows that the <color> tags aren't visible
        // so we can just say we are done now
        return AttributeMarkerProcessor.NoDiagnostics;
    }

    void Start()
    {
        if (lineProvider == null)
        {
            lineProvider = (LineProviderBehaviour)GameObject.FindObjectOfType<DialogueRunner>().LineProvider;
        }

        if (palette == null)
        {
            return;
        }

        foreach (var colour in palette.ColourMarkers)
        {
            lineProvider.RegisterMarkerProcessor(colour.Marker, this);
        }
    }
}
