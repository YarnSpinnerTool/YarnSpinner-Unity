#nullable enable

namespace Yarn.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using TMPro;
    using UnityEngine;
    using Yarn.Markup;

    /// <summary>
    /// An implementation of <see cref="IAsyncTypewriter"/> that delivers
    /// characters one at a time, and invokes any <see
    /// cref="IActionMarkupHandler"/>s along the way as needed.
    /// </summary>
    public class BasicTypewriter : IAsyncTypewriter
    {
        /// <summary>
        /// The <see cref="TMP_Text"/> to display the text in.
        /// </summary>
        public TMP_Text Text { get; set; }

        /// <inheritdoc/>
        public IEnumerable<IActionMarkupHandler> ActionMarkupHandlers { get; set; } = Array.Empty<IActionMarkupHandler>();

        /// <summary>
        /// The number of characters per second to deliver.
        /// </summary>
        /// <remarks>If this value is zero, all characters are delivered at
        /// once, subject to any delays added by the markup handlers in <see
        /// cref="ActionMarkupHandlers"/>.</remarks>
        public float CharactersPerSecond { get; set; } = 0f;

        public BasicTypewriter(TMP_Text text)
        {
            this.Text = text;
        }

        /// <inheritdoc/>
        public void OnPrepareForLine(MarkupParseResult lineText)
        {
            this.Text.text = lineText.Text;

            // the typewriter requires all characters to be hidden at the start so they can be shown one at a time
            Text.maxVisibleCharacters = 0;
            // letting every temporal processor know that fade up (if set) is about to begin
            foreach (var processor in ActionMarkupHandlers)
            {
                processor.OnPrepareForLine(lineText, Text);
            }
        }

        /// <inheritdoc/>
        public void OnLineDisplayBegin(MarkupParseResult lineText)
        {
            this.Text.maxVisibleCharacters = lineText.Text.Length;

            // Let every markup handler know that display is about to begin
            foreach (var markupHandler in ActionMarkupHandlers)
            {
                markupHandler.OnLineDisplayBegin(lineText, Text);
            }
        }

        /// <inheritdoc/>
        public async YarnTask RunTypewriter(Markup.MarkupParseResult line, CancellationToken cancellationToken)
        {
            Text.maxVisibleCharacters = 0;
            Text.text = line.Text;

            double secondsPerCharacter = 0;
            if (CharactersPerSecond > 0)
            {
                secondsPerCharacter = 1.0 / CharactersPerSecond;
            }

            // Get the count of visible characters from TextMesh to exclude markup characters
            var visibleCharacterCount = Text.GetTextInfo(line.Text).characterCount;

            // Start with a full time budget so that we immediately show the first character
            double accumulatedDelay = secondsPerCharacter;

            // Go through each character of the line and letting the
            // processors know about it
            for (int i = 0; i < visibleCharacterCount; i++)
            {
                // If we don't already have enough accumulated time budget
                // for a character, wait until we do (or until we're
                // cancelled)
                while (!cancellationToken.IsCancellationRequested
                    && (accumulatedDelay < secondsPerCharacter))
                {
                    var timeBeforeYield = Time.timeAsDouble;
                    await YarnTask.Yield();
                    var timeAfterYield = Time.timeAsDouble;
                    accumulatedDelay += timeAfterYield - timeBeforeYield;
                }

                // Tell every markup handler that it is time to process the
                // current character
                foreach (var processor in ActionMarkupHandlers)
                {
                    await processor
                        .OnCharacterWillAppear(i, line, cancellationToken)
                        .SuppressCancellationThrow();
                }

                Text.maxVisibleCharacters += 1;

                accumulatedDelay -= secondsPerCharacter;
            }

            // We've finished showing every character (or we were
            // cancelled); ensure that everything is now visible.
            Text.maxVisibleCharacters = visibleCharacterCount;

            // Let each markup handler know the line has finished displaying
            foreach (var markupHandler in ActionMarkupHandlers)
            {
                markupHandler.OnLineDisplayComplete();
            }
        }

        /// <inheritdoc/>
        public void OnLineWillDismiss()
        {
            // we tell all action processors that the line is finished and is about to go away
            foreach (var processor in ActionMarkupHandlers)
            {
                processor.OnLineWillDismiss();
            }
        }
    }
}
