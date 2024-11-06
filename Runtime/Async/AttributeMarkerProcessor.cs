using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Markup;
using System.Text;

namespace Yarn.Unity
{
    public abstract class AttributeMarkerProcessor : MonoBehaviour, IAttributeMarkerProcessor
    {
        public static List<LineParser.MarkupDiagnostic> NoDiagnostics = new List<LineParser.MarkupDiagnostic>();
        public abstract List<LineParser.MarkupDiagnostic> ProcessReplacementMarker(MarkupAttribute marker, StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode);
    }
}