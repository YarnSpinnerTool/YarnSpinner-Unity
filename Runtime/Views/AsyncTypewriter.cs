namespace Yarn.Unity
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using TMPro;
    using UnityEngine;

#nullable enable

    public interface IAsyncTypewriter
    {
        public YarnTask RunTypewriter(Markup.MarkupParseResult line, CancellationToken cancellationToken);
    }

    public class BasicTypewriter : IAsyncTypewriter
    {
        public TMP_Text? Text { get; set; }

        public IEnumerable<IActionMarkupHandler> TemporalProcessors { get; set; } = Array.Empty<IActionMarkupHandler>();

        public float TypewriterEffectSpeed { get; set; } = 0f;

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

                // Let every temporal processor know that fading is done and
                // display is about to begin
                foreach (var processor in TemporalProcessors)
                {
                    processor.OnLineDisplayBegin(line, Text);
                }

                int milliSecondsPerLetter = 0;
                if (TypewriterEffectSpeed > 0)
                {
                    milliSecondsPerLetter = (int)(1000f / TypewriterEffectSpeed);
                }

                // Get the count of visible characters from TextMesh to exclude markup characters
                var visibleCharacterCount = Text.GetTextInfo(line.Text).characterCount;

                // Go through each character of the line and letting the
                // processors know about it
                for (int i = 0; i < visibleCharacterCount; i++)
                {
                    // Tell every processor that it is time to process the
                    // current character
                    foreach (var processor in TemporalProcessors)
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

            // Let each temporal processor know the line has finished displaying
            foreach (var processor in TemporalProcessors)
            {
                processor.OnLineDisplayComplete();
            }
        }
    }
}
