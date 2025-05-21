namespace Yarn.Unity
{
    using TMPro;
    using System.Collections.Generic;
    using System.Threading;
    using System;

    #nullable enable

    public interface IAsyncTypewriter
    {
        public YarnTask Typewrite(int typewriterEffectSpeed, Markup.MarkupParseResult line, TMP_Text textfield, IEnumerable<IActionMarkupHandler> temporalProcessors, CancellationToken cancellationToken);
    }

    public class BasicTypewriter: IAsyncTypewriter
    {
        public async YarnTask Typewrite(int typewriterEffectSpeed, Markup.MarkupParseResult line, TMP_Text textfield, IEnumerable<IActionMarkupHandler> temporalProcessors, CancellationToken cancellationToken)
        {
            textfield.maxVisibleCharacters = 0;
            textfield.text = line.Text;

            // letting every temporal processor know that fading is done and display is about to begin
            foreach (var processor in temporalProcessors)
            {
                processor.OnLineDisplayBegin(line, textfield);
            }

            int milliSecondsPerLetter = 0;
            if (typewriterEffectSpeed > 0)
            {
                milliSecondsPerLetter = (int)(1000f / typewriterEffectSpeed);
            }

            // going through each character of the line and letting the processors know about it
            for (int i = 0; i < line.Text.Length; i++)
            {
                // telling every processor that it is time to process the current character
                foreach (var processor in temporalProcessors)
                {
                    await processor.OnCharacterWillAppear(i, line, cancellationToken).SuppressCancellationThrow();
                }

                textfield.maxVisibleCharacters += 1;
                if (milliSecondsPerLetter > 0)
                {
                    await YarnTask.Delay(TimeSpan.FromMilliseconds(milliSecondsPerLetter), cancellationToken).SuppressCancellationThrow();
                }
            }

            // letting each temporal processor know the line has finished displaying
            foreach (var processor in temporalProcessors)
            {
                processor.OnLineDisplayComplete();
            }

            textfield.maxVisibleCharacters = line.Text.Length;
        }
    }
}