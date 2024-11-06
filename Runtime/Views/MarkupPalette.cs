/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// Represents a collection of marker names and colours.
    /// </summary>
    /// <remarks>
    /// This is intended to be used with the LineView, and also be a sample of using the markup system.
    /// </remarks>
    
    [CreateAssetMenu(fileName = "NewPalette", menuName = "Yarn Spinner/Markup Palette", order = 102)]
    public class MarkupPalette : ScriptableObject
    {
        [System.Serializable]
        public struct ColorMarker
        {
            public string Marker;
            public Color Color;
            public bool Boldened;
            public bool Italicised;
            public bool Underlined;
            public bool Strikedthrough;
        }

        /// <summary>
        /// The collection of colour markers inside this
        /// </summary>
        public List<ColorMarker> ColourMarkers = new List<ColorMarker>();

        /// <summary>
        /// Determines the colour for a particular marker inside this palette.
        /// </summary>
        /// <param name="Marker">The marker of which you are covetous of it's colour.</param>
        /// <param name="colour">The colour of the marker, or black if it doesn't exist.</param>
        /// <returns>True if the marker exists within this palette.</returns>
        public bool ColorForMarker(string Marker, out Color colour)
        {
            foreach (var item in ColourMarkers)
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

        public bool PaletteForMarker(string Marker, out ColorMarker palette)
        {
            foreach (var item in ColourMarkers)
            {
                if (item.Marker == Marker)
                {
                    palette = item;
                    return true;
                }
            }

            palette = new ColorMarker();
            return false;
        }
    }
}

// ok so there are TWO things I want to do now
// first is beef this up so that it supports multiple different relative
// colour
// bold or do we just do font-weight
// italics
// underline, strikethrough

// and then make another one called StyleMarkupProcessor
// this just converts [style = h1] into <style="h1"> for the more advanced stuff

