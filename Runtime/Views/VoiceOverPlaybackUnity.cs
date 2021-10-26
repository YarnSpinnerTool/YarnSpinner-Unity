using System;
using System.Collections;
using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// Handles playback of voice over <see cref="AudioClip"/>s referenced
    /// on <see cref="YarnScript"/>s.
    /// </summary>
    public class VoiceOverPlaybackUnity : DialogueViewBase
    {
        /// <summary>
        /// The fade out time when <see cref="FinishCurrentLine"/> is
        /// called.
        /// </summary>
        public float fadeOutTimeOnLineFinish = 0.05f;

        /// <summary>
        /// The amount of time to wait before starting playback of the
        /// line.
        /// </summary>
        public float waitTimeBeforeLineStart = 0f;

        /// <summary>
        /// The amount of time after playback has completed before this
        /// view reports that it's finished delivering the line.
        /// </summary>
        public float waitTimeAfterLineComplete = 0f;

        [SerializeField]
        AudioSource audioSource;

        /// <summary>
        /// When true, the <see cref="DialogueRunner"/> has signaled to
        /// finish the current line asap.
        /// </summary>
        bool interrupted = false;

        void Awake()
        {
            if (!audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
            }
        }

        /// <summary>
        /// Start playback of the associated voice over <see
        /// cref="AudioClip"/> of the given <see cref="LocalizedLine"/>.
        /// </summary>
        /// <param name="dialogueLine"></param>
        /// <returns></returns>
        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            interrupted = false;

            if (!(dialogueLine is AudioLocalizedLine audioLine))
            {
                Debug.LogError($"Playing voice over failed because {nameof(RunLine)} expected to receive an {nameof(AudioLocalizedLine)}, but instead received a {dialogueLine?.GetType().ToString() ?? "null"}. Is your {nameof(DialogueRunner)} set up to use a {nameof(AudioLineProvider)}?", gameObject);
                onDialogueLineFinished();
                return;
            }

            // Get the localized voice over audio clip
            var voiceOverClip = audioLine.AudioClip;

            if (!voiceOverClip)
            {
                Debug.Log("Playing voice over failed since the AudioClip of the voice over audio language or the base language was null.", gameObject);
                onDialogueLineFinished();
                return;
            }

            if (audioSource.isPlaying)
            {
                // Usually, this shouldn't happen because the
                // DialogueRunner finishes and ends a line first
                audioSource.Stop();
            }

            StartCoroutine(DoPlayback(voiceOverClip, onDialogueLineFinished));

            IEnumerator DoPlayback(AudioClip clip, Action onFinished)
            {
                // If we need to wait before starting playback, do this now
                if (waitTimeBeforeLineStart > 0)
                {
                    yield return new WaitForSeconds(waitTimeBeforeLineStart);
                }

                // Start playing the audio.
                audioSource.PlayOneShot(clip);

                // Wait until either the audio source finishes playing, or the
                // interruption flag is set.
                while (audioSource.isPlaying && !interrupted)
                {
                    yield return null;
                }

                // If the line was interrupted, we need to wrap up the playback
                // as quickly as we can. We do this here with a fade-out to
                // zero over fadeOutTimeOnLineFinish seconds.
                if (audioSource.isPlaying && interrupted)
                {
                    // Fade out voice over clip
                    float lerpPosition = 0f;
                    float volumeFadeStart = audioSource.volume;
                    while (audioSource.volume != 0)
                    {
                        lerpPosition += Time.unscaledDeltaTime / fadeOutTimeOnLineFinish;
                        audioSource.volume = Mathf.Lerp(volumeFadeStart, 0, lerpPosition);
                        yield return null;
                    }
                    audioSource.Stop();
                    audioSource.volume = volumeFadeStart;
                }
                else
                {
                    audioSource.Stop();
                }

                // We've finished our playback at this point, either by waiting
                // normally or by interrupting it with a fadeout. If we weren't
                // interrupted, and we have additional time to wait after the
                // audio finishes, wait now. (If we were interrupted, we skip
                // this wait, because the user has already indicated that
                // they're fine with things moving faster than sounds normal.)

                if (interrupted == false && waitTimeAfterLineComplete > 0)
                {
                    yield return new WaitForSeconds(waitTimeAfterLineComplete);
                }

                // We can now signal that the line delivery has finished.
                onFinished();
            }
        }

        public override void OnLineStatusChanged(LocalizedLine dialogueLine)
        {
            switch (dialogueLine.Status)
            {
                case LineStatus.Presenting:
                    // Nothing to do here - continue running.
                    break;
                case LineStatus.Interrupted:
                    // The user wants us to wrap up the audio quickly. The
                    // DoPlayback coroutine will apply the fade out defined
                    // by fadeOutTimeOnLineFinish.
                    interrupted = true;
                    break;
                case LineStatus.FinishedPresenting:
                    // The line has finished delivery on all views. Nothing
                    // left to do for us, since the audio will have already
                    // finished playing out.
                    break;
                case LineStatus.Dismissed:
                    // The line is being dismissed; we should ensure that
                    // audio playback has ended.
                    audioSource.Stop();
                    break;
            }
        }
    }
}
