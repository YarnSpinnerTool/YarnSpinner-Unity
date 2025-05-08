/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
#nullable enable

namespace Yarn.Unity
{

    /// <summary>
    /// Represents a line, ready to be presented to the user in the localisation
    /// they have specified.
    /// </summary>
    public class LocalizedLine
    {
        /// <summary>
        /// DialogueLine's ID
        /// </summary>
        public string TextID = "<unknown>";

        /// <summary>
        /// DialogueLine's inline expression's substitution
        /// </summary>
        public string[] Substitutions = System.Array.Empty<string>();

        /// <summary>
        /// DialogueLine's text
        /// </summary>
        public string? RawText;

        /// <summary>
        /// Any metadata associated with this line.
        /// </summary>
        public string[] Metadata = System.Array.Empty<string>();

        /// <summary>
        /// The name of the character, if present.
        /// </summary>
        /// <remarks>
        /// This value will be <see langword="null"/> if the line does not have
        /// a character name.
        /// </remarks>
        public string? CharacterName
        {
            get
            {
                if (Text.TryGetAttributeWithName("character", out var characterNameAttribute))
                {
                    if (characterNameAttribute.Properties.TryGetValue("name", out var value))
                    {
                        return value.StringValue;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// The asset associated with this line, if any.
        /// </summary>
        public Object? Asset;

        /// <summary>
        /// The underlying <see cref="Yarn.Markup.MarkupParseResult"/> for this
        /// line.
        /// </summary>
        public Markup.MarkupParseResult Text { get; set; }

        /// <summary>
        /// The underlying <see cref="Yarn.Markup.MarkupParseResult"/> for this
        /// line, with any `character` attribute removed.
        /// </summary>
        /// <remarks>
        /// If the line has no `character` attribute, this method returns the
        /// same value as <see cref="Text"/>.
        /// </remarks>
        public Markup.MarkupParseResult TextWithoutCharacterName
        {
            get
            {
                // If a 'character' attribute is present, remove its text
                if (Text.TryGetAttributeWithName("character", out var characterNameAttribute))
                {
                    return Text.DeleteRange(characterNameAttribute);
                }
                else
                {
                    return Text;
                }
            }
        }

        /// <summary>
        /// A <see cref="LocalizedLine"/> object that represents content not
        /// being found.
        /// </summary>
        public static readonly LocalizedLine InvalidLine = new LocalizedLine
        {
            Asset = null,
            Metadata = System.Array.Empty<string>(),
            RawText = "!! ERROR: Missing line!",
            Substitutions = System.Array.Empty<string>(),
            TextID = "<missing>",
            Text = new Markup.MarkupParseResult("!! ERROR: Missing line!", new System.Collections.Generic.List<Markup.MarkupAttribute>())
        };
    }

}
