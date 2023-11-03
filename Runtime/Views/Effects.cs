using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = TMPShim;
#endif

namespace Yarn.Unity
{
    /// <summary>
    /// Contains coroutine methods that apply visual effects. This class is used
    /// by <see cref="LineView"/> to handle animating the presentation of lines.
    /// </summary>
    public static class Effects
    {
        /// <summary>
        /// An object that can be used to signal to a coroutine that it should
        /// terminate early.
        /// </summary>
        /// <remarks>
        /// <para>
        /// While coroutines can be stopped by calling <see
        /// cref="MonoBehaviour.StopCoroutine"/> or <see
        /// cref="MonoBehaviour.StopAllCoroutines"/>, this has the side effect
        /// of also stopping any coroutine that was waiting for the now-stopped
        /// coroutine to finish.
        /// </para>
        /// <para>
        /// Instances of this class may be passed as a parameter to a coroutine
        /// that they can periodically poll to see if they should terminate
        /// earlier than planned.
        /// </para>
        /// <para>
        /// To use this class, create an instance of it, and pass it as a
        /// parameter to your coroutine. In the coroutine, call <see
        /// cref="Start"/> to mark that the coroutine is running. During the
        /// coroutine's execution, periodically check the <see
        /// cref="WasInterrupted"/> property to determine if the coroutine
        /// should exit. If it is <see langword="true"/>, the coroutine should
        /// exit (via the <c>yield break</c> statement.) At the normal exit of
        /// your coroutine, call the <see cref="Complete"/> method to mark that the
        /// coroutine is no longer running. To make a coroutine stop, call the
        /// <see cref="Interrupt"/> method.
        /// </para>
        /// <para>
        /// You can also use the <see cref="CanInterrupt"/> property to
        /// determine if the token is in a state in which it can stop (that is,
        /// a coroutine that's using it is currently running.)
        /// </para>
        /// </remarks>
        public class CoroutineInterruptToken {

            /// <summary>
            /// The state that the token is in.
            /// </summary>
            enum State {
                NotRunning,
                Running,
                Interrupted,
            }
            private State state = State.NotRunning;

            public bool CanInterrupt => state == State.Running;
            public bool WasInterrupted => state == State.Interrupted;
            public void Start() => state = State.Running;
            public void Interrupt()
            {
                if (CanInterrupt == false) {
                    throw new InvalidOperationException($"Cannot stop {nameof(CoroutineInterruptToken)}; state is {state} (and not {nameof(State.Running)}");
                }
                state = State.Interrupted;
            }

            public void Complete() => state = State.NotRunning;
        }

        /// <summary>
        /// A coroutine that fades a <see cref="CanvasGroup"/> object's opacity
        /// from <paramref name="from"/> to <paramref name="to"/> over the
        /// course of <see cref="fadeTime"/> seconds, and then invokes <paramref
        /// name="onComplete"/>.
        /// </summary>
        /// <param name="from">The opacity value to start fading from, ranging
        /// from 0 to 1.</param>
        /// <param name="to">The opacity value to end fading at, ranging from 0
        /// to 1.</param>
        /// <param name="stopToken">A <see cref="CoroutineInterruptToken"/> that
        /// can be used to interrupt the coroutine.</param>
        public static IEnumerator FadeAlpha(CanvasGroup canvasGroup, float from, float to, float fadeTime, CoroutineInterruptToken stopToken = null)
        {
            stopToken?.Start();

            canvasGroup.alpha = from;

            var timeElapsed = 0f;

            while (timeElapsed < fadeTime)
            {
                if (stopToken?.WasInterrupted ?? false) {
                    yield break;
                }

                var fraction = timeElapsed / fadeTime;
                timeElapsed += Time.deltaTime;

                float a = Mathf.Lerp(from, to, fraction);

                canvasGroup.alpha = a;
                yield return null;
            }

            canvasGroup.alpha = to;

            // If our destination alpha is zero, disable interactibility,
            // because the canvas group is now invisible.
            if (to == 0)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                // Otherwise, enable interactibility, because it's visible.
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            stopToken?.Complete();
        }

        /// <summary>
        /// A coroutine that gradually reveals the text in a <see
        /// cref="TextMeshProUGUI"/> object over time.
        /// </summary>
        /// <remarks>
        /// <para>This method works by adjusting the value of the <paramref name="text"/> parameter's <see cref="TextMeshProUGUI.maxVisibleCharacters"/> property. This means that word wrapping will not change half-way through the presentation of a word.</para>
        /// <para style="note">Depending on the value of <paramref name="lettersPerSecond"/>, <paramref name="onCharacterTyped"/> may be called multiple times per frame.</para>
        /// <para>Due to an internal implementation detail of TextMeshProUGUI, this method will always take at least one frame to execute, regardless of the length of the <paramref name="text"/> parameter's text.</para>
        /// </remarks>
        /// <param name="text">A TextMeshProUGUI object to reveal the text
        /// of.</param>
        /// <param name="lettersPerSecond">The number of letters that should be
        /// revealed per second.</param>
        /// <param name="onCharacterTyped">An <see cref="Action"/> that should be called for each character that was revealed.</param>
        /// <param name="stopToken">A <see cref="CoroutineInterruptToken"/> that
        /// can be used to interrupt the coroutine.</param>
        public static IEnumerator Typewriter(TextMeshProUGUI text, float lettersPerSecond, Action onCharacterTyped, CoroutineInterruptToken stopToken = null)
        {
            yield return PausableTypewriter(
                text,
                lettersPerSecond,
                onCharacterTyped,
                null,
                null,
                null,
                stopToken
            );
        }

        /// <summary>
        /// A basic wait coroutine that can be interrupted.
        /// </summary>
        /// <remarks>
        /// This is designed to be used as part of the <see cref="PausableTypewriter"/> but theoretically anything can use it.
        /// </remarks>
        /// <param name="duration">The length of the pause</param>
        /// <param name="stopToken">An interrupt token for this wait</param>
        private static IEnumerator InterruptableWait(float duration, CoroutineInterruptToken stopToken = null)
        {
            float accumulator = 0;
            while (accumulator < duration)
            {
                if (stopToken?.WasInterrupted ?? false)
                {
                    yield break;
                }

                yield return null;
                accumulator += Time.deltaTime;
            }
        }

        /// <summary>
        /// A coroutine that gradually reveals the text in a <see cref="TextMeshProUGUI"/> object over time.
        /// </summary>
        /// <remarks>
        /// Essentially identical to <see cref="Effects.Typewriter"/> but supports pausing the animation based on <paramref name="pausePositions"/> values.
        /// <para>This method works by adjusting the value of the <paramref name="text"/> parameter's <see cref="TextMeshProUGUI.maxVisibleCharacters"/> property. This means that word wrapping will not change half-way through the presentation of a word.</para>
        /// <para style="note">Depending on the value of <paramref name="lettersPerSecond"/>, <paramref name="onCharacterTyped"/> may be called multiple times per frame.</para>
        /// <para>Due to an internal implementation detail of TextMeshProUGUI, this method will always take at least one frame to execute, regardless of the length of the <paramref name="text"/> parameter's text.</para>
        /// </remarks>
        /// <param name="text">A TextMeshProUGUI object to reveal the text of</param>
        /// <param name="lettersPerSecond">The number of letters that should be revealed per second.</param>
        /// <param name="onCharacterTyped">An <see cref="Action"/> that should be called for each character that was revealed.</param>
        /// <param name="onPauseStarted">An <see cref="Action"/> that will be called when the typewriter effect is paused.</param>
        /// <param name="onPauseEnded">An <see cref="Action"/> that will be called when the typewriter effect is restarted.</param>
        /// <param name="pausePositions">A stack of character position and pause duration tuples used to pause the effect. Generally created by <see cref="LineView.GetPauseDurationsInsideLine"/></param>
        /// <param name="stopToken">A <see cref="CoroutineInterruptToken"/> that can be used to interrupt the coroutine.</param>
        public static IEnumerator PausableTypewriter(TextMeshProUGUI text, float lettersPerSecond, Action onCharacterTyped, Action onPauseStarted, Action onPauseEnded, Stack<(int position, float duration)> pausePositions, CoroutineInterruptToken stopToken = null)
        {
            stopToken?.Start();

            // Start with everything invisible
            text.maxVisibleCharacters = 0;

            // Wait a single frame to let the text component process its
            // content, otherwise text.textInfo.characterCount won't be
            // accurate
            yield return null;

            // How many visible characters are present in the text?
            var characterCount = text.textInfo.characterCount;

            // Early out if letter speed is zero, text length is zero
            if (lettersPerSecond <= 0 || characterCount == 0)
            {
                // Show everything and return
                text.maxVisibleCharacters = characterCount;
                stopToken?.Complete();
                yield break;
            }

            // Convert 'letters per second' into its inverse
            float secondsPerLetter = 1.0f / lettersPerSecond;

            // If lettersPerSecond is larger than the average framerate, we
            // need to show more than one letter per frame, so simply
            // adding 1 letter every secondsPerLetter won't be good enough
            // (we'd cap out at 1 letter per frame, which could be slower
            // than the user requested.)
            //
            // Instead, we'll accumulate time every frame, and display as
            // many letters in that frame as we need to in order to achieve
            // the requested speed.
            var accumulator = Time.deltaTime;

            while (text.maxVisibleCharacters < characterCount)
            {
                if (stopToken?.WasInterrupted ?? false)
                {
                    yield break;
                }

                // ok so the change needs to be that if at any point we hit the pause position
                // we instead stop worrying about letters
                // and instead we do a normal wait for the necessary duration
                if (pausePositions != null && pausePositions.Count != 0)
                {
                    if (text.maxVisibleCharacters == pausePositions.Peek().Item1)
                    {
                        var pause = pausePositions.Pop();
                        onPauseStarted?.Invoke();
                        yield return Effects.InterruptableWait(pause.Item2, stopToken);
                        onPauseEnded?.Invoke();

                        // need to reset the accumulator
                        accumulator = Time.deltaTime;
                    }
                }

                // We need to show as many letters as we have accumulated
                // time for.
                while (accumulator >= secondsPerLetter)
                {
                    text.maxVisibleCharacters += 1;
                    onCharacterTyped?.Invoke();
                    accumulator -= secondsPerLetter;
                }
                accumulator += Time.deltaTime;

                yield return null;
            }

            // We either finished displaying everything, or were
            // interrupted. Either way, display everything now.
            text.maxVisibleCharacters = characterCount;

            stopToken?.Complete();
        }
    }
}
