using System.Collections.Generic;
using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> that produces <see
    /// cref="LocalizedLine"/>s.
    /// </summary>
    /// <remarks>
    /// <see cref="DialogueRunner"/>s use a <see
    /// cref="LineProviderBehaviour"/> to get <see cref="LocalizedLine"/>s,
    /// which contain the localized information that <see
    /// cref="DialogueViewBase"/> classes use to present content to the
    /// player. 
    ///
    /// Subclasses of this abstract class may return subclasses of <see
    /// cref="LocalizedLine"/>. For example, <see
    /// cref="AudioLineProvider"/> returns an <see
    /// cref="AudioLocalizedLine"/>, which includes <see
    /// cref="AudioClip"/>; views that make use of audio can then access
    /// this additional data.
    /// </remarks>
    public abstract class LineProviderBehaviour : MonoBehaviour
    {
        /// <summary>Specifies the language code to use for text content
        /// for this <see cref="LineProviderBehaviour"/>.
        [Language]
        public string textLanguageCode = System.Globalization.CultureInfo.CurrentCulture.Name;

        /// <summary>
        /// Prepares and returns a <see cref="LocalizedLine"/> from the
        /// specified <see cref="Yarn.Line"/>.
        /// </summary>
        /// <remarks>
        /// This method should not be called if <see
        /// cref="LinesAvailable"/> returns <see langword="false"/>.
        /// </remarks>
        /// <param name="line">The <see cref="Yarn.Line"/> to produce the
        /// <see cref="LocalizedLine"/> from.</param>
        /// <returns>A localized line, ready to be presented to the
        /// player.</returns>
        public abstract LocalizedLine GetLocalizedLine(Yarn.Line line);

        /// <summary>
        /// The YarnProject that contains the localized data for lines.
        /// </summary>
        /// <remarks>This property is set at run-time by the object that
        /// will be requesting content (typically a <see
        /// cref="DialogueRunner"/>).
        public YarnProject YarnProject { get; set; }

        /// <summary>
        /// Signals to the line provider that lines with the provided line
        /// IDs may be presented shortly.        
        /// </summary>
        /// <remarks>
        /// Subclasses of <see cref="LineProviderBehaviour"/> can override
        /// this to prepare any neccessary resources needed to present
        /// these lines, like pre-loading voice-over audio. The default
        /// implementation does nothing.
        ///
        /// Not every line may run; this method serves as a way to give the
        /// line provider advance notice that a line _may_ run, not _will_
        /// run.
        ///
        /// When this method is run, the value returned by the <see
        /// cref="LinesAvailable"/> property should change to false until the
        /// necessary resources have loaded.
        /// </remarks>
        /// <param name="lineIDs">A collection of line IDs that the line
        /// provider should prepare for.</param>
        public virtual void PrepareForLines(IEnumerable<string> lineIDs)
        {
            // No-op by default.
        }

        /// <summary>
        /// Gets a value indicating whether this line provider is ready to
        /// provide <see cref="LocalizedLine"/> objects. The default
        /// implementation returns <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// Subclasses should return <see langword="false"/> when the
        /// required resources needed to deliver lines are not yet ready,
        /// and <see langword="true"/> when they are.
        /// </remarks>
        public virtual bool LinesAvailable => true;

        /// <summary>
        /// Called by Unity when the <see cref="LineProviderBehaviour"/>
        /// has first appeared in the scene.
        /// </summary>
        /// <remarks>
        /// This method is <see langword="public"/> <see
        /// langword="virtual"/> to allow subclasses to override it.
        /// </remarks>
        public virtual void Start()
        {
        }
    }

    /// <summary>
    /// Represents a line, ready to be presented to the user in the
    /// localisation they have specified.
    /// </summary>
    public class LocalizedLine
    {
        /// <summary>
        /// DialogueLine's ID
        /// </summary>
        public string TextID;

        /// <summary>
        /// DialogueLine's inline expression's substitution
        /// </summary>
        public string[] Substitutions;

        /// <summary>
        /// DialogueLine's text
        /// </summary>
        public string RawText;

        /// <summary>
        /// The line's delivery status.
        /// </summary>
        public LineStatus Status;

        /// <summary>
        /// The name of the character, if present.
        /// </summary>
        /// <remarks>
        /// This value will be <see langword="null"/> if the line does not
        /// have a character name.
        /// </remarks>
        public string CharacterName
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
        /// The underlying <see cref="Yarn.Markup.MarkupParseResult"/> for
        /// this line.
        /// </summary>
        public Markup.MarkupParseResult Text { get; set; }

        /// <summary>
        /// The underlying <see cref="Yarn.Markup.MarkupParseResult"/> for
        /// this line, with any `character` attribute removed.
        /// </summary>
        /// <remarks>
        /// If the line has no `character` attribute, this method returns
        /// the same value as <see cref="Text"/>.
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
    }

}
