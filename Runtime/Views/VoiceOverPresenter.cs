/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using UnityEngine;
using Yarn.Unity.Attributes;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// A subclass of <see cref="DialoguePresenterBase"/> that plays voice-over
    /// <see cref="AudioClip"/>s for lines of dialogue.
    /// </summary>
    public sealed class VoiceOverPresenter : DialoguePresenterBase
    {

        /// <summary>
        /// If <see langword="true"/>, the voice over view will request that the
        /// dialogue runner proceed to the next line when audio for the line has
        /// finished playing.
        /// </summary>
        [Group("Line Management")]
        public bool endLineWhenVoiceoverComplete = true;

        /// <summary>
        /// The fade out time when the line is interrupted during playback.
        /// </summary>
        [Group("Timing")]
        public float fadeOutTimeOnLineFinish = 0.05f;

        /// <summary>
        /// The amount of time to wait before starting playback of the line.
        /// </summary>
        [Group("Timing")]
        public float waitTimeBeforeLineStart = 0f;

        /// <summary>
        /// The amount of time after playback has completed before this view
        /// reports that it's finished delivering the line.
        /// </summary>
        [Group("Timing")]
        public float waitTimeAfterLineComplete = 0f;

        /// <summary>
        /// The <see cref="AudioSource"/> that this voice over view will play
        /// its audio from.
        /// </summary>
        /// <remarks>If this is <see langword="null"/>, a new <see
        /// cref="AudioSource"/> will be added at runtime.</remarks>
        [SerializeField]
        [NotNull]
        // for some reason Unity doesn't seem to respect the [NotNull] attribute
        // presumably this will be fixed in a future version of Unity
#pragma warning disable CS8618
        public AudioSource audioSource;
#pragma warning restore CS8618

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

        void Reset()
        {
            if (audioSource == null)
            {
                audioSource = GetComponentInChildren<AudioSource>();
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
        /// <inheritdoc cref="DialoguePresenterBase.RunLineAsync(LocalizedLine,
        /// LineCancellationToken)" path="/param"/>
        /// <seealso cref="DialoguePresenterBase.RunLineAsync(LocalizedLine,
        /// LineCancellationToken)"/>
        public override async YarnTask RunLineAsync(LocalizedLine dialogueLine, LineCancellationToken lineCancellationToken)
        {
            // Get the localized voice over audio clip
            AudioClip? voiceOverClip = null;

            if (dialogueLine.Asset is AudioClip clip)
            {
                voiceOverClip = clip;
            }
            else if (dialogueLine.Asset is IAssetProvider provider && provider.TryGetAsset(out AudioClip? result))
            {
                voiceOverClip = result;
            }

            DialogueRunner? dialogueRunner = dialogueLine.Source as DialogueRunner;

            if (voiceOverClip == null)
            {
                Debug.LogError($"Playing voice over failed because the localised line {dialogueLine.TextID} " +
                    $"either didn't have an asset, or its asset was not an {nameof(AudioClip)}.", gameObject);

                if (this.endLineWhenVoiceoverComplete && dialogueRunner != null)
                {
                    // If we didn't get a line, but we were configured to
                    // advance the line on end, then we should act as though
                    // we've reached the end of the line now and advance.
                    dialogueRunner.RequestNextLine();
                }
                return;
            }

            if (audioSource.isPlaying)
            {
                // Usually, this shouldn't happen because the DialogueRunner
                // finishes and ends a line first
                audioSource.Stop();
            }

            // If we need to wait before starting playback, do this now
            if (waitTimeBeforeLineStart > 0)
            {
                await YarnTask.Delay(
                    TimeSpan.FromSeconds(waitTimeBeforeLineStart),
                    lineCancellationToken.NextContentToken).SuppressCancellationThrow();
            }

            // Start playing the audio.
            audioSource.PlayOneShot(voiceOverClip);

            // Playback may not begin immediately, so wait until it does (or if
            // the line is interrupted.)
            await YarnTask.WaitUntil(() => audioSource.isPlaying, lineCancellationToken.NextContentToken).SuppressCancellationThrow();

            if (!DialogueRunner.IsInPlaymode)
            {
                return;
            }

            // Now wait until either the audio source finishes playing, or the
            // line is interrupted.
            await YarnTask.WaitUntil(() => !audioSource.isPlaying, lineCancellationToken.NextContentToken).SuppressCancellationThrow();

            if (!DialogueRunner.IsInPlaymode)
            {
                return;
            }

            // If the line was interrupted while we were playing, we need to
            // wrap up the playback as quickly as we can. We do this here with a
            // fade-out to zero over fadeOutTimeOnLineFinish seconds.
            if (audioSource.isPlaying && lineCancellationToken.IsNextContentRequested)
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
                    await YarnTask.Yield();
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

            if (!lineCancellationToken.IsNextContentRequested && waitTimeAfterLineComplete > 0)
            {
                await YarnTask.Delay(
                    TimeSpan.FromSeconds(waitTimeAfterLineComplete),
                    lineCancellationToken.NextContentToken
                ).SuppressCancellationThrow();
            }

            if (endLineWhenVoiceoverComplete)
            {
                if (dialogueRunner == null)
                {
                    Debug.LogError($"Can't end line due to voice over being complete: {nameof(dialogueRunner)} is null", this);
                }
                else
                {
                    dialogueRunner.RequestNextLine();
                }
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Stops any audio if there is still any playing.
        /// </remarks>
        public override YarnTask OnDialogueCompleteAsync()
        {
            // just in case we are still playing audio we want it to stop
            audioSource.Stop();

            return YarnTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override YarnTask OnDialogueStartedAsync()
        {
            return YarnTask.CompletedTask;
        }
    }
}
