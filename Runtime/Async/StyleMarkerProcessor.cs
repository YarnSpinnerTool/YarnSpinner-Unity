using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Yarn.Markup;

namespace Yarn.Unity
{
    public class StyleMarkerProcessor : AttributeMarkerProcessor
    {
        public LineProviderBehaviour lineProvider;
        public override List<LineParser.MarkupDiagnostic> ProcessReplacementMarker(MarkupAttribute marker, StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode)
        {
            // throw new System.NotImplementedException();
            // ok so we check if we have a property called style
            // if not give up
            if (!marker.TryGetProperty("style", out var property))
            {
                var error = new List<LineParser.MarkupDiagnostic>
                {
                    new LineParser.MarkupDiagnostic("Unable to identify a name for the style.")
                };
                return error;
            }

            childBuilder.Insert(0, $"<style=\"{property.StringValue}\">");
            childBuilder.Append("</style>");

            return AttributeMarkerProcessor.NoDiagnostics;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (lineProvider == null)
            {
                lineProvider = (LineProviderBehaviour)GameObject.FindObjectOfType<DialogueRunner>().LineProvider;
            }
            lineProvider.RegisterMarkerProcessor("style", this);
        }
    }
}
