/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Yarn.Markup;

namespace Yarn.Unity
{
    /// <summary>
    /// An attribute marker processor receives a marker found in a Yarn line,
    /// and optionally rewrites the marker and its children into a new form.
    /// </summary>
    /// <seealso cref="LineProviderBehaviour"/>
    public abstract class ReplacementMarkupHandler : MonoBehaviour, IAttributeMarkerProcessor
    {
        /// <summary>
        /// An empty collection of diagnostics.
        /// </summary>
        public static readonly List<LineParser.MarkupDiagnostic> NoDiagnostics = new List<LineParser.MarkupDiagnostic>();

        /// <inheritdoc/>
        public abstract List<LineParser.MarkupDiagnostic> ProcessReplacementMarker(MarkupAttribute marker, StringBuilder childBuilder, List<MarkupAttribute> childAttributes, string localeCode);
    }
}
