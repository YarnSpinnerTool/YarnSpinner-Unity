/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// Represents a collection of marker names and colours.
    /// </summary>
    /// <remarks>
    /// This is intended to be used with the <see cref="LinePresenter"/>, and
    /// also be a sample of using the markup system.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewPalette", menuName = "Yarn Spinner/Markup Palette", order = 102)]
    public sealed class MarkupPalette : ScriptableObject
    {
        /// <summary>
        /// Contains information describing the formatting style of text within
        /// a named marker.
        /// </summary>
        [System.Serializable]
        public struct BasicMarker
        {
            /// <summary>
            /// The name of the marker which can be used in text to indicate
            /// specific formatting.
            /// </summary>
            public string Marker;

            /// <summary>
            /// Indicates whethere or not the text associated with this marker should have a custom colour.
            /// </summary>
            public bool CustomColor;

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

        [System.Serializable]
        public struct CustomMarker
        {
            public string Marker;
            public string Start;
            public string End;
            public int MarkerOffset;
            public int TotalVisibleCharacterCount;
        }

        /// <summary>
        /// A list containing all the color markers defined in this palette.
        /// </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("ColourMarkers")]
        public List<BasicMarker> BasicMarkers = new List<BasicMarker>();
        public List<CustomMarker> CustomMarkers = new List<CustomMarker>();

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
            foreach (var item in BasicMarkers)
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

        public bool PaletteForMarker(string markerName, out CustomMarker palette)
        {
            // we first check if we have a marker of that name in the basic markers
            foreach (var item in BasicMarkers)
            {
                if (item.Marker == markerName)
                {
                    System.Text.StringBuilder front = new();
                    System.Text.StringBuilder back = new();

                    // do we have a custom colour set?
                    if (item.CustomColor)
                    {
                        front.AppendFormat("<color=#{0}>", ColorUtility.ToHtmlStringRGBA(item.Color));
                        back.Append("</color>");
                    }

                    // do we need to bold it?
                    if (item.Boldened)
                    {
                        front.Append("<b>");
                        back.Append("</b>");
                    }
                    // do we need to italicise it?
                    if (item.Italicised)
                    {
                        front.Append("<i>");
                        back.Append("</i>");
                    }
                    // do we need to underline it?
                    if (item.Underlined)
                    {
                        front.Append("<u>");
                        back.Append("</u>");
                    }
                    // do we need to strikethrough it?
                    if (item.Strikedthrough)
                    {
                        front.Append("<s>");
                        back.Append("</s>");
                    }

                    palette = new CustomMarker()
                    {
                        Marker = item.Marker,
                        Start = front.ToString(),
                        End = back.ToString(),
                        MarkerOffset = 0,
                        TotalVisibleCharacterCount = 0,
                    };
                    return true;
                }
            }

            // we now check if we have one in the format markers
            foreach (var item in CustomMarkers)
            {
                if (item.Marker == markerName)
                {
                    palette = item;
                    return true;
                }
            }

            // we don't have anything for this marker
            // so we return false and a default marker
            palette = new();
            return false;
        }
    }
}
