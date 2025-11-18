#nullable enable

namespace Yarn.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;
#if USE_TMP
    using TMPro;
#else
    using TMP_Text = Yarn.Unity.TMPShim;
#endif

    /// <summary>
    /// An implementation of <see cref="IAsyncTypewriter"/> that delivers
    /// characters one at a time, and invokes any <see
    /// cref="IActionMarkupHandler"/>s along the way as needed.
    /// </summary>
    public class LetterTypewriter : IAsyncTypewriter
    {
        /// <summary>
        /// The <see cref="TMP_Text"/> to display the text in.
        /// </summary>
        public TMP_Text? Text { get; set; }

        /// <summary>
        /// A collection of <see cref="IActionMarkupHandler"/> objects that
        /// should be invoked as needed during the typewriter's delivery in <see
        /// cref="RunTypewriter"/>, depending upon the contents of a line.
        /// </summary>
        public List<IActionMarkupHandler> ActionMarkupHandlers { get; set; } = new();

        /// <summary>
        /// The number of characters per second to deliver.
        /// </summary>
        /// <remarks>If this value is zero, all characters are delivered at
        /// once, subject to any delays added by the markup handlers in <see
        /// cref="ActionMarkupHandlers"/>.</remarks>
        public float CharactersPerSecond { get; set; } = 0f;

        /// <inheritdoc/>
        public async YarnTask RunTypewriter(Markup.MarkupParseResult line, CancellationToken cancellationToken)
        {
            if (Text == null)
            {
                Debug.LogWarning($"Can't show text as typewriter, because {nameof(Text)} was not provided");
            }
            else
            {
                Text.maxVisibleCharacters = 0;
                Text.text = line.Text;

                // Let every markup handler know that display is about to begin
                foreach (var markupHandler in ActionMarkupHandlers)
                {
                    markupHandler.OnLineDisplayBegin(line, Text);
                }

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
            }

            // Let each markup handler know the line has finished displaying
            foreach (var markupHandler in ActionMarkupHandlers)
            {
                markupHandler.OnLineDisplayComplete();
            }
        }

        public void PrepareForContent(Markup.MarkupParseResult line)
        {
            if (Text == null)
            {
                return;
            }

            Text.maxVisibleCharacters = 0;
            Text.text = line.Text;

            foreach (var processor in ActionMarkupHandlers)
            {
                processor.OnPrepareForLine(line, Text);
            }
        }

        public void ContentWillDismiss()
        {
            // we tell all action processors that the line is finished and is about to go away
            foreach (var processor in ActionMarkupHandlers)
            {
                processor.OnLineWillDismiss();
            }
        }
    }
}
