#nullable enable

namespace Yarn.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;
    using Yarn.Markup;
#if USE_TMP
    using TMPro;
#else
    using TMP_Text = Yarn.Unity.TMPShim;
#endif

    /// <summary>
    /// An implementation of <see cref="IAsyncTypewriter"/> that delivers
    /// all content instantly, and invokes any <see
    /// cref="IActionMarkupHandler"/>s along the way as needed.
    /// </summary>
    public class InstantTypewriter : IAsyncTypewriter
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

        /// <inheritdoc/>
        public async YarnTask RunTypewriter(Markup.MarkupParseResult line, CancellationToken cancellationToken)
        {
            if (Text == null)
            {
                Debug.LogWarning($"Can't show text as typewriter, because {nameof(Text)} was not provided");
                return;
            }

            Text.maxVisibleCharacters = 0;
            Text.text = line.Text;

            // Let every markup handler know that display is about to begin
            foreach (var markupHandler in ActionMarkupHandlers)
            {
                markupHandler.OnLineDisplayBegin(line, Text);
            }

            var textInfo = Text.GetTextInfo(line.Text);
            // Get the count of visible characters from TextMesh to exclude markup characters
            var visibleCharacterCount = textInfo.characterCount;

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

        public void PrepareForContent(MarkupParseResult line)
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
