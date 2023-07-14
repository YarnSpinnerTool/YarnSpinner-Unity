using System;
using System.Collections;
using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// A subclass of <see cref="DialogueViewBase"/> that plays voice-over <see
    /// cref="AudioClip"/>s for lines of dialogue.
    /// </summary>
    /// <remarks>
    /// This class plays audio clip assets that are provided by an <see
    /// cref="AudioLineProvider"/>. To use a <see cref="VoiceOverView"/> in your
    /// game, your <see cref="DialogueRunner"/> must be configured to use an
    /// <see cref="AudioLineProvider"/>, and your Yarn projects must be
    /// configured to use voice-over audio assets. For more information, see
    /// <see
    /// href="/using-yarnspinner-with-unity/assets-and-localization/README.md">Localization
    /// and Assets</see>.
    /// </remarks>
    /// <seealso cref="DialogueViewBase"/>
    public class VoiceOverView : DialogueViewBase
    {
        /// <summary>
        /// The fade out time when <see cref="UserRequestedViewAdvancement"/> is
        /// called.
        /// </summary>
        public float fadeOutTimeOnLineFinish = 0.05f;

        /// <summary>
        /// The amount of time to wait before starting playback of the line.
        /// </summary>
        public float waitTimeBeforeLineStart = 0f;

        /// <summary>
        /// The amount of time after playback has completed before this view
        /// reports that it's finished delivering the line.
        /// </summary>
        public float waitTimeAfterLineComplete = 0f;

        /// <summary>
        /// The <see cref="AudioSource"/> that this voice over view will play
        /// its audio from.
        /// </summary>
        /// <remarks>If this is <see langword="null"/>, a new <see
        /// cref="AudioSource"/> will be added at runtime.</remarks>
        [SerializeField]
        public AudioSource audioSource;

        /// <summary>
        /// The current coroutine that's playing a line.
        /// </summary>
        Coroutine playbackCoroutine;

        /// <summary>
        /// An interrupt token that can be used to interrupt <see
        /// cref="playbackCoroutine"/>.
        /// </summary>
        Effects.CoroutineInterruptToken interruptToken = new Effects.CoroutineInterruptToken();

        /// <summary>
        /// The method that should be called before <see
        /// cref="playbackCoroutine"/> exits.
        /// </summary>
        /// <remarks>
        /// This value is set by <see cref="RunLine"/> and <see
        /// cref="InterruptLine"/>.
        /// </remarks>
        Action completionHandler;

        void Awake()
        {
            if (audioSource == null)
            {
                // If we don't have an audio source, add one. 
                audioSource = gameObject.AddComponent<AudioSource>();

                // Additionally, we'll assume that the user didn't place the
                // game object that this component is attached to deliberately,
                // so we'll set the spatial blend to 0 (which means the audio
                // will not be positioned in 3D space.)
                audioSource.spatialBlend = 0f;
            }
        }

        /// <summary>
        /// Begins playing the associated audio for the specified line.
        /// </summary>
        /// <remarks>
        /// <para style="warning">This method is not intended to be called from
        /// your code. Instead, the <see cref="DialogueRunner"/> class will call
        /// it at the appropriate time.</para>
        /// </remarks>
        /// <inheritdoc cref="DialogueViewBase.RunLine(LocalizedLine, Action)"
        /// path="/param"/>
        /// <seealso cref="DialogueViewBase.RunLine(LocalizedLine, Action)"/>
        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            // If we have a current playback for some reason, stop it
            // immediately.
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                audioSource.Stop();
                playbackCoroutine = null;
            }

            // Set the handler to call when the line has finished presenting.
            // (This might change later, if the line gets interrupted.)
            completionHandler = onDialogueLineFinished;

            playbackCoroutine = StartCoroutine(DoRunLine(dialogueLine));
        }

        private IEnumerator DoRunLine(LocalizedLine dialogueLine)
        {
            // Get the localized voice over audio clip
            var voiceOverClip = dialogueLine.Asset as AudioClip;

            if (voiceOverClip == null)
            {
                Debug.LogError($"Playing voice over failed because the localised line {dialogueLine.TextID} either didn't have an asset, or its asset was not an {nameof(AudioClip)}.", gameObject);
                
                completionHandler?.Invoke();
                yield break;
            }

            if (audioSource.isPlaying)
            {
                // Usually, this shouldn't happen because the DialogueRunner
                // finishes and ends a line first
                audioSource.Stop();
            }

            interruptToken.Start();

            // If we need to wait before starting playback, do this now
            if (waitTimeBeforeLineStart > 0)
            {
                var elaspedTime = 0f;
                while (elaspedTime < waitTimeBeforeLineStart)
                {
                    if (interruptToken.WasInterrupted)
                    {
                        // We were interrupted in the middle of waiting to
                        // start. Stop immediately before playing anything.
                        completionHandler?.Invoke();
                        yield break;
                    }
                    yield return null;
                    elaspedTime += Time.deltaTime;
                }
            }

            // Start playing the audio.
            audioSource.PlayOneShot(voiceOverClip);

            // Wait until either the audio source finishes playing, or the
            // interruption flag is set.
            while (audioSource.isPlaying && !interruptToken.WasInterrupted)
            {
                yield return null;
            }

            // If the line was interrupted, we need to wrap up the playback as
            // quickly as we can. We do this here with a fade-out to zero over
            // fadeOutTimeOnLineFinish seconds.
            if (interruptToken.WasInterrupted)
            {
                // Fade out voice over clip
                float lerpPosition = 0f;
                float volumeFadeStart = audioSource.volume;
                while (audioSource.volume != 0)
                {
                    // We'll use unscaled time here, because if time is scaled,
                    // we might be fading out way too slowly, and that would
                    // sound extremely strange.
                    lerpPosition += Time.unscaledDeltaTime / fadeOutTimeOnLineFinish;
                    audioSource.volume = Mathf.Lerp(volumeFadeStart, 0, lerpPosition);
                    yield return null;
                }

                // We're done fading out. Restore our audio volume to its
                // original point for the next line.
                audioSource.volume = volumeFadeStart;
            }
            audioSource.Stop();

            // We've finished our playback at this point, either by waiting
            // normally or by interrupting it with a fadeout. If we weren't
            // interrupted, and we have additional time to wait after the audio
            // finishes, wait now. (If we were interrupted, we skip this wait,
            // because the user has already indicated that they're fine with
            // things moving faster than sounds normal.)

            if (!interruptToken.WasInterrupted && waitTimeAfterLineComplete > 0)
            {
                var elapsed = 0f;
                while (elapsed < waitTimeAfterLineComplete && !interruptToken.WasInterrupted)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }

            completionHandler?.Invoke();
            interruptToken.Complete();
        }

        /// <summary>
        /// Interrupts the playback of the specified line, and quickly fades the
        /// playback to silent.
        /// </summary>
        /// <inheritdoc cref="RunLine(LocalizedLine, Action)" path="/remarks"/>
        /// <inheritdoc cref="DialogueViewBase.InterruptLine(LocalizedLine,
        /// Action)" path="/param"/>
        /// <seealso cref="DialogueViewBase.InterruptLine(LocalizedLine,
        /// Action)"/>
        public override void InterruptLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            if (interruptToken.CanInterrupt)
            {
                completionHandler = onDialogueLineFinished;
                interruptToken.Interrupt();
            }
            else
            {
                onDialogueLineFinished();
            }
        }

        /// <summary>
        /// Ends any existing playback, and reports that the line has finished
        /// dismissing.
        /// </summary>
        /// <inheritdoc cref="RunLine(LocalizedLine, Action)" path="/remarks"/>
        /// <inheritdoc cref="DialogueViewBase.DismissLine(Action)"
        /// path="/param"/>
        /// <seealso cref="DialogueViewBase.DismissLine(Action)"/>
        public override void DismissLine(Action onDismissalComplete)
        {
            // There's not much to do for a dismissal, since there's nothing
            // visible on screen and any audio playback is likely to have
            // finished as part of RunLine or InterruptLine completing. 

            // We'll stop the audio source, just to be safe, and immediately
            // report that we're done.
            audioSource.Stop();
            onDismissalComplete();
        }

        /// <summary>
        /// Signals to this dialogue view that the user would like to skip
        /// playback.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this method is called, this view indicates to its <see
        /// cref="DialogueRunner"/> that the line should be interrupted.
        /// </para>
        /// <para>
        /// If this view is not currently playing any audio, this method does
        /// nothing.
        /// </para>
        /// </remarks>
        /// <seealso cref="DialogueViewBase.InterruptLine(LocalizedLine, Action)"/>
        public override void UserRequestedViewAdvancement()
        {
            // We arent currently playing a line. There's nothing to interrupt.
            if (!audioSource.isPlaying)
            {
                return;
            }
            // we are playing a line but interruption is already in progress
            // we don't want to double interrupt as weird things can happen
            if (interruptToken.CanInterrupt)
            {
                requestInterrupt?.Invoke();
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Stops any audio if there is still any playing.
        /// </remarks>
        public override void DialogueComplete()
        {
            // just in case we are still playing audio we want it to stop
            audioSource.Stop();
        }
    }
}
