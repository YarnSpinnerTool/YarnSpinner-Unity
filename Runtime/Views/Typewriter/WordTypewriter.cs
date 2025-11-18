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
    /// words one at a time, and invokes any <see
    /// cref="IActionMarkupHandler"/>s along the way as needed.
    /// </summary>
    public class WordTypewriter : IAsyncTypewriter
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
        /// The number of words per second to deliver.
        /// </summary>
        /// <remarks>If this value is zero, all words are delivered at
        /// once, subject to any delays added by the markup handlers in <see
        /// cref="ActionMarkupHandlers"/>.</remarks>
        public float WordsPerSecond { get; set; } = 0f;

        /// <inheritdoc/>
        public async YarnTask RunTypewriter(Markup.MarkupParseResult line, CancellationToken cancellationToken)
        {
            // ok so this will have to do the following:
            // work out where the pauses are meant to be
            // do this by finding all the breaks in the line
            // then at each point in the line we move char by char
            // when we hit a break point (which we know in advance)

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

                double secondsPerWord = 0;
                if (WordsPerSecond > 0)
                {
                    secondsPerWord = 1.0 / WordsPerSecond;
                }

                var wordBoundaries = new SortedSet<int>();
                var textInfo = Text.GetTextInfo(line.Text);
                for (int i = 0; i < textInfo.wordCount; i++)
                {
                    var word = textInfo.wordInfo[i];
                    wordBoundaries.Add(word.lastCharacterIndex + 1);
                }

                // Get the count of visible characters from TextMesh to exclude markup characters
                var visibleCharacterCount = textInfo.characterCount;

                // Start with a full time budget so that we immediately show the first character
                double accumulatedDelay = secondsPerWord;

                int current = wordBoundaries.Min;

                // Go through each character of the line and letting the
                // processors know about it
                for (int i = 0; i < visibleCharacterCount; i++)
                {
                    // if we are at the character that requires waiting we want to wait until we hit the allotted time
                    if (i == current)
                    {
                        // If we don't already have enough accumulated time budget for a word, wait until we do (or until we're cancelled)
                        while (!cancellationToken.IsCancellationRequested && (accumulatedDelay < secondsPerWord))
                        {
                            var timeBeforeYield = Time.timeAsDouble;
                            await YarnTask.Yield();
                            var timeAfterYield = Time.timeAsDouble;
                            accumulatedDelay += timeAfterYield - timeBeforeYield;
                        }
                        accumulatedDelay -= secondsPerWord;

                        wordBoundaries.Remove(current);
                        if (wordBoundaries.Count > 0)
                        {
                            current = wordBoundaries.Min;
                        }
                        else
                        {
                            current = int.MaxValue;
                        }
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
