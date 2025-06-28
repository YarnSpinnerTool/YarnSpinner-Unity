#nullable enable

namespace Yarn.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using TMPro;
    using Yarn.Markup;

    /// <summary>
    /// An implementation of <see cref="IAsyncTypewriter"/> that delivers its
    /// text as quickly as possible while still respecting any delays created by
    /// action markup handlers.
    /// </summary>
    public class FakeTypewriter : IAsyncTypewriter
    {
        /// <summary>
        /// The <see cref="TMPro.TMP_Text"/> to display line text in.
        /// </summary>
        public TMP_Text Text { get; set; }

        /// <inheritdoc/>
        public IEnumerable<IActionMarkupHandler> ActionMarkupHandlers { get; set; } = Array.Empty<IActionMarkupHandler>();

        /// <summary>
        /// Creates a new <see cref="FakeTypewriter"/> that uses the given <see
        /// cref="TMP_Text"/>.
        /// </summary>
        /// <param name="text">The TextMeshPro object to show the line
        /// in.</param>
        public FakeTypewriter(TMP_Text text)
        {
            this.Text = text;
        }

        /// <inheritdoc/>
        public void OnPrepareForLine(MarkupParseResult lineText)
        {
            this.Text.text = lineText.Text;
            this.Text.maxVisibleCharacters = 0;

            foreach (var handler in ActionMarkupHandlers)
            {
                handler.OnPrepareForLine(lineText, Text);
            }
        }

        /// <inheritdoc/>
        public void OnLineDisplayBegin(MarkupParseResult lineText) { }

        /// <inheritdoc/>
        public async YarnTask RunTypewriter(MarkupParseResult line, CancellationToken cancellationToken)
        {
            foreach (var handler in this.ActionMarkupHandlers)
            {
                handler.OnLineDisplayBegin(line, Text);
            }

            var visibleCharacterCount = Text.GetTextInfo(line.Text).characterCount;

            this.Text.maxVisibleCharacters = 0;

            for (int i = 0; i < this.Text.textInfo.characterCount; i++)
            {
                // Tell every markup handler that it is time to process the
                // current character
                foreach (var processor in ActionMarkupHandlers)
                {
                    await processor
                        .OnCharacterWillAppear(i, line, cancellationToken)
                        .SuppressCancellationThrow();
                }
                this.Text.maxVisibleCharacters += 1;
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
