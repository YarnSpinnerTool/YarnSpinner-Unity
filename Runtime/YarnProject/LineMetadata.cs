/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// Represents the bundles of metadata across all lines and a mechanism to retrieve a specific metadata bundle for a line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Line metadata is the contents of hashtags at the end of a line and any synthesised metadata added by the compiler or dialogue runner.
    /// <code>
    /// Character: Why hello there #happy
    /// </code>
    /// This shows off a a line with one entry in the metadata <c>happy</c>.
    /// </para>
    /// <para>
    /// Yarn Spinner may also synthesis metadata and add it to this entry as if you had manually done so.
    /// This is done for a variety of reasons and any metadata you do not care about can safely be ignored if you don't need that metadata.
    /// The most common synthesised metadata added to line is the <c>lastline</c> metadata.
    /// For more details on adding metadata see <see cref="https://docs.yarnspinner.dev/write-yarn-scripts/advanced-scripting/tags-metadata"/>.
    /// </para>
    /// </remarks>
    [Serializable]
    public class LineMetadata
    {
        [Serializable]
        class StringDictionary : SerializableDictionary<string, string> { }

        [SerializeField]
        private StringDictionary _lineMetadata = new StringDictionary();

        internal LineMetadata(IEnumerable<LineMetadataTableEntry> lineMetadataTableEntries)
        {
            AddMetadata(lineMetadataTableEntries);
        }

        /// <summary>
        /// Adds any metadata if they are defined for each line. The metadata is
        /// internally stored as a single string with each piece of metadata
        /// separated by a single whitespace.
        /// </summary>
        /// <param name="lineMetadataTableEntries">IEnumerable with metadata
        /// entries.</param>
        internal void AddMetadata(IEnumerable<LineMetadataTableEntry> lineMetadataTableEntries)
        {
            foreach (var entry in lineMetadataTableEntries)
            {
                if (entry.Metadata.Length == 0)
                {
                    continue;
                }

                _lineMetadata.Add(entry.ID, String.Join(" ", entry.Metadata));
            }
        }

        public LineMetadata() { }

        /// <summary>
        /// adds a new metadata entry to the requested line.
        /// This will override any metadata already on the line.
        /// </summary>
        /// <param name="lineID">the id of the line, if the line has implicit tags this value may change between runs</param>
        /// <param name="metadata">the metadata to add onto this line</param>
        public void AddMetadata(string lineID, IEnumerable<string> metadata)
        {
            _lineMetadata.Add(lineID, string.Join(" ", metadata));
        }

        /// <summary>
        /// Gets the line IDs that contain metadata.
        /// </summary>
        /// <returns>The line IDs.</returns>
        public IEnumerable<string> GetLineIDs()
        {
            // The object returned doesn't allow modifications and is kept in
            // sync with `_lineMetadata`.
            return _lineMetadata.Keys;
        }

        /// <summary>
        /// Returns metadata for a given line ID, if any is defined.
        /// </summary>
        /// <param name="lineID">The line ID.</param>
        /// <returns>An array of each piece of metadata if defined, otherwise
        /// returns null.</returns>
        public string[]? GetMetadata(string lineID)
        {
            if (_lineMetadata.TryGetValue(lineID, out var result))
            {
                return result.Split(' ');
            }

            return null;
        }

        /// <summary>
        /// Shadow lines are specialcased because while their contents is the same as another line they may have significantly different metadata.
        /// </summary>
        /// <remarks>
        /// The most common situation where a shadow line has different metadata than it's origin line is when it is the last line before options.
        /// In this case you will still want to know that this line is going to be followed by options and it is added by the compiler for you.
        /// You can also add your own custom line metadata to shadow lines as hashtags.
        /// </remarks>
        /// <param name="lineID">the id of the shadow line</param>
        /// <returns>the metadata for the shadow line</returns>
        public string? GetShadowLineSource(string lineID)
        {
            if (_lineMetadata.TryGetValue(lineID, out var metadataString) == false)
            {
                // The line has no metadata, so it is not a shadow line.
                return null;
            }

            var metadata = metadataString.Split(' ');

            foreach (var metadataEntry in metadata)
            {
                if (metadataEntry.StartsWith("shadow:"))
                {
                    // This is a shadow line. Return the line ID that it's
                    // shadowing.
                    return "line:" + metadataEntry.Substring("shadow:".Length);
                }
            }

            // The line had metadata, but it wasn't a shadow line.
            return null;
        }
    }
}
