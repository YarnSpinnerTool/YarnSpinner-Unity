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
    }
}
