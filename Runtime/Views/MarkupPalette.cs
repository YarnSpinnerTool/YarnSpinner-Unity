using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// Represents a collection of marker names and colours.
    /// </summary>
    /// <remarks>
    /// This is intended to be used with the LineView, and also be a sample of
    /// using the markup system.
    /// </remarks>

    [CreateAssetMenu(fileName = "NewPalette", menuName = "Yarn Spinner/Markup Palette", order = 102)]
    public class MarkupPalette : ScriptableObject
    {
        /// <summary>
        /// Contains information describing the formatting style of text within
        /// a named marker.
        /// </summary>
        [System.Serializable]
        public struct FormatMarker
        {
            /// <summary>
            /// The name of the marker which can be used in text to indicate
            /// specific formatting.
            /// </summary>
            public string Marker;

            /// <summary>
            /// The color to use for text associated with this marker.
            /// </summary>
            public Color Color;

            /// <summary>
            /// Indicates whether the text associated with this marker should be
            /// bolded.
            /// </summary>
            public bool Boldened;

            /// <summary>
            /// Indicates whether the text associated with this marker should be
            /// italicized.
            /// </summary>
            public bool Italicised;

            /// <summary>
            /// Indicates whether the text associated with this marker should be
            /// underlined.
            /// </summary>
            public bool Underlined;

            /// <summary>
            /// Indicates whether the text associated with this marker should
            /// have a strikethrough effect.
            /// </summary>
            public bool Strikedthrough;
        }

        /// <summary>
        /// A list containing all the color markers defined in this palette.
        /// </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("ColourMarkers")]
        public List<FormatMarker> FormatMarkers = new List<FormatMarker>();

        /// <summary>
        /// Determines the colour for a particular marker inside this palette.
        /// </summary>
        /// <param name="Marker">The marker you want to get a colour
        /// for.</param>
        /// <param name="colour">The colour of the marker, or <see
        /// cref="Color.black"/> if it doesn't exist in the <see
        /// cref="MarkupPalette"/>.</param>
        /// <returns><see langword="true"/> if the marker exists within this
        /// palette; <see langword="false"/> otherwise.</returns>
        public bool ColorForMarker(string Marker, out Color colour)
        {
            foreach (var item in FormatMarkers)
            {
                if (item.Marker == Marker)
                {
                    colour = item.Color;
                    return true;
                }
            }
            colour = Color.black;
            return false;
        }

        /// <summary>
        /// Gets formatting information. for a particular marker inside this
        /// palette.
        /// </summary>
        /// <param name="markerName">The marker you want to get formatting
        /// information for.</param>
        /// <param name="palette">The <see cref="FormatMarker"/> for the given
        /// marker name, or a default format if a marker named <paramref
        /// name="markerName"/> was not found.</param>
        /// <returns><see langword="true"/> if the marker exists within this
        /// palette; <see langword="false"/> otherwise.</returns>
        public bool FormatForMarker(string markerName, out FormatMarker palette)
        {
            foreach (var item in FormatMarkers)
            {
                if (item.Marker == markerName)
                {
                    palette = item;
                    return true;
                }
            }

            palette = new FormatMarker()
            {
                Color = Color.black,
                Boldened = false,
                Italicised = false,
                Strikedthrough = false,
                Underlined = false,
                Marker = "undefined",
            };

            return false;
        }
    }
}

// ok so there are TWO things I want to do now first is beef this up so that it
// supports multiple different relative colour bold or do we just do font-weight
// italics underline, strikethrough

// and then make another one called StyleMarkupProcessor this just converts
// [style = h1] into <style="h1"> for the more advanced stuff

