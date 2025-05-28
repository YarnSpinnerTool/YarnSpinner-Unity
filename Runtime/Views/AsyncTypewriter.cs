namespace Yarn.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using TMPro;
    using UnityEngine;

#nullable enable

    /// <summary>
    /// An object that can handle delivery of a line's text over time.
    /// </summary>
    public interface IAsyncTypewriter
    {
        /// <summary>
        /// Displays the contents of a line over time.
        /// </summary>
        /// <remarks>
        /// <para>This method is called when a dialogue presenter wants to
        /// deliver a line's text. The typewriter should present the text to the
        /// user; it may take as long as it needs to do so. </para>
        ///
        /// <para>If <paramref name="cancellationToken"/>'s <see
        /// cref="CancellationToken.IsCancellationRequested"/> becomes true, the
        /// typewriter effect should end early and present the entire contents
        /// of <paramref name="line"/>.</para>
        /// <param name="line">The line to display.</param>
        /// <param name="cancellationToken">A token that indicates that the
        /// typewriter effect should be cancelled.</param>
        /// <returns>A task that completes when the typewriter effect has
        /// finished.</returns>
        public YarnTask RunTypewriter(Markup.MarkupParseResult line, CancellationToken cancellationToken);
    }

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
        public TMP_Text? Text { get; set; }

        /// <summary>
        /// A collection of <see cref="IActionMarkupHandler"/> objects that
        /// should be invoked as needed during the typewriter's delivery in <see
        /// cref="RunTypewriter"/>, depending upon the contents of a line.
        /// </summary>
        public IEnumerable<IActionMarkupHandler> ActionMarkupHandlers { get; set; } = Array.Empty<IActionMarkupHandler>();

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

                int milliSecondsPerLetter = 0;
                if (CharactersPerSecond > 0)
                {
                    milliSecondsPerLetter = (int)(1000f / CharactersPerSecond);
                }

                // Get the count of visible characters from TextMesh to exclude markup characters
                var visibleCharacterCount = Text.GetTextInfo(line.Text).characterCount;

                // Go through each character of the line and letting the
                // processors know about it
                for (int i = 0; i < visibleCharacterCount; i++)
                {
                    // Tell every markup handler that it is time to process the
                    // current character
                    foreach (var processor in ActionMarkupHandlers)
                    {
                        await processor
                            .OnCharacterWillAppear(i, line, cancellationToken)
                            .SuppressCancellationThrow();
                    }

                    Text.maxVisibleCharacters += 1;
                    if (milliSecondsPerLetter > 0)
                    {
                        await YarnTask.Delay(
                            TimeSpan.FromMilliseconds(milliSecondsPerLetter),
                            cancellationToken
                        ).SuppressCancellationThrow();
                    }
                }

                Text.maxVisibleCharacters = visibleCharacterCount;
            }

            // Let each markup handler know the line has finished displaying
            foreach (var markupHandler in ActionMarkupHandlers)
            {
                markupHandler.OnLineDisplayComplete();
            }
        }
    }
}
