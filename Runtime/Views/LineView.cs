using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Yarn.Unity
{
    public class InterruptionFlag
    {
        public bool Interrupted { get; private set; } = false;
        public void Set() => Interrupted = true;
        public void Clear() => Interrupted = false;
    }

    public static class Effects
    {
        /// <summary>
        /// A coroutine that fades a <see cref="CanvasGroup"/> object's
        /// opacity from <paramref name="from"/> to <paramref name="to"/>
        /// over the course of <see cref="fadeTime"/> seconds, and then
        /// invokes <paramref name="onComplete"/>. An <see
        /// cref="InterruptionFlag"/> may be used to signal that the fade
        /// should be interrupted; if this happens, the opacity is set to
        /// <paramref name="to"/>.
        /// </summary>
        /// <param name="from">The opacity value to start fading from,
        /// ranging from 0 to 1.</param>
        /// <param name="to">The opacity value to end fading at, ranging
        /// from 0 to 1.</param>
        /// <param name="onComplete">A delegate to invoke after fading is
        /// complete.</param>
        public static IEnumerator FadeAlpha(CanvasGroup canvasGroup, float from, float to, float fadeTime, Action onComplete = null, InterruptionFlag interruption = null)
        {
            canvasGroup.alpha = from;

            var timeElapsed = 0f;

            while (timeElapsed < fadeTime && (interruption?.Interrupted ?? false) == false)
            {
                var fraction = timeElapsed / fadeTime;
                timeElapsed += Time.deltaTime;

                float a = Mathf.Lerp(from, to, fraction);

                canvasGroup.alpha = a;
                yield return null;
            }

            canvasGroup.alpha = to;

            if (to == 0)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            onComplete?.Invoke();
        }

        public static IEnumerator Typewriter(TextMeshProUGUI text, float lettersPerSecond, Action onCharacterTyped = null, Action onComplete = null, InterruptionFlag interruption = null)
        {
            // Start with everything invisible
            text.maxVisibleCharacters = 0;

            // Wait a single frame to let the text component process its
            // content, otherwise text.textInfo.characterCount won't be
            // accurate
            yield return null;

            // How many visible characters are present in the text?
            var characterCount = text.textInfo.characterCount;

            // Early out if letter speed is zero or text length is zero
            if (lettersPerSecond <= 0 || characterCount == 0)
            {
                // Show everything and invoke the completion handler
                text.maxVisibleCharacters = characterCount;
                onComplete?.Invoke();
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

            while (text.maxVisibleCharacters < characterCount && (interruption == null || interruption.Interrupted == false))
            {
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

            // Wrap up by invoking our completion handler.
            onComplete?.Invoke();
        }
    }

    public class LineView : DialogueViewBase
    {
        internal enum ContinueActionType
        {
            None,
            KeyCode,
            InputSystemAction,
            InputSystemActionFromAsset,
        }

        [SerializeField]
        internal CanvasGroup canvasGroup;

        [SerializeField]
        internal bool useFadeEffect = true;

        [SerializeField]
        [Min(0)]
        internal float fadeInTime = 0.25f;

        [SerializeField]
        [Min(0)]
        internal float fadeOutTime = 0.05f;

        [SerializeField]
        internal TextMeshProUGUI lineText = null;

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("showCharacterName")]
        internal bool showCharacterNameInLineView = true;

        [SerializeField]
        internal TextMeshProUGUI characterNameText = null;

        [SerializeField]
        internal bool useTypewriterEffect = false;

        [SerializeField]
        internal UnityEngine.Events.UnityEvent onCharacterTyped;

        [SerializeField]
        [Min(0)]
        internal float typewriterEffectSpeed = 0f;

        [SerializeField]
        internal GameObject continueButton = null;

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("skipActionType")]
        internal ContinueActionType continueActionType;

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("skipActionKeyCode")]
        internal KeyCode continueActionKeyCode = KeyCode.Escape;


#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("skipActionReference")]
        internal InputActionReference continueActionReference = null;

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("skipAction")]
        internal InputAction continueAction = new InputAction("Skip", InputActionType.Button, CommonUsages.Cancel);
#endif

        InterruptionFlag interruptionFlag = new InterruptionFlag();

        LocalizedLine currentLine = null;

        public void Start()
        {
            canvasGroup.alpha = 0;

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            // If we are using an action reference, and it's not null,
            // configure it
            if (continueActionType == ContinueActionType.InputSystemActionFromAsset && continueActionReference != null)
            {
                continueActionReference.action.started += UserPerformedSkipAction;
            }

            // The custom skip action always starts disabled
            continueAction?.Disable();
            continueAction.started += UserPerformedSkipAction;
#endif
        }

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        void UserPerformedSkipAction(InputAction.CallbackContext obj)
        {
            OnContinueClicked();
        }
#endif

        public void Reset()
        {
            canvasGroup = GetComponentInParent<CanvasGroup>();
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        public void Update()
        {
            // Should we indicate to the DialogueRunner that we want to
            // interrupt/continue a line? We need to pass a number of
            // checks.
            
            // We need to be configured to use a keycode to interrupt/continue
            // lines.
            if (continueActionType != ContinueActionType.KeyCode)
            {
                return;
            }

            // That keycode needs to have been pressed this frame.
            if (!UnityEngine.Input.GetKeyDown(continueActionKeyCode))
            {
                return;
            }
            
            // The line must not be in the middle of being dismissed.
            if ((currentLine?.Status) == LineStatus.Dismissed)
            {
                return;
            }

            // We're good to indicate that we want to skip/continue.
            OnContinueClicked();
        }
#endif

        public override void DismissLine(Action onDismissalComplete)
        {
#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            if (continueActionType == ContinueActionType.InputSystemAction)
            {
                continueAction?.Disable();
            }
            else if (continueActionType == ContinueActionType.InputSystemActionFromAsset)
            {
                continueActionReference?.action?.Disable();
            }
#endif

            currentLine = null;

            if (useFadeEffect)
            {
                StartCoroutine(Effects.FadeAlpha(canvasGroup, 1, 0, fadeOutTime, onDismissalComplete));
            }
            else
            {
                canvasGroup.interactable = false;
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
                onDismissalComplete();
            }
        }

        public override void OnLineStatusChanged(LocalizedLine dialogueLine)
        {
            switch (dialogueLine.Status)
            {
                case LineStatus.Presenting:
                    break;
                case LineStatus.Interrupted:
                    // We have been interrupted. Set our interruption flag,
                    // so that any animations get skipped.
                    interruptionFlag.Set();
                    break;
                case LineStatus.FinishedPresenting:
                    // The line has finished being delivered by all views.
                    // Display the Continue button.
                    if (continueButton != null)
                    {
                        continueButton.SetActive(true);
                        var selectable = continueButton.GetComponentInChildren<Selectable>();
                        if (selectable != null)
                        {
                            selectable.Select();
                        }
                    }
                    break;
                case LineStatus.Dismissed:
                    break;
            }
        }

        private void OnCharacterTyped() {
            onCharacterTyped?.Invoke();
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            currentLine = dialogueLine;

#if USE_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
            // If we are using a custom Unity Input System action, enable
            // it now.
            if (continueActionType == ContinueActionType.InputSystemAction)
            {
                continueAction?.Enable();
            }
            else if (continueActionType == ContinueActionType.InputSystemActionFromAsset)
            {
                continueActionReference?.action.Enable();
            }
#endif

            lineText.gameObject.SetActive(true);
            canvasGroup.gameObject.SetActive(true);

            if (continueButton != null)
            {
                continueButton.SetActive(false);
            }

            interruptionFlag.Clear();

            if (characterNameText == null)
            {
                if (showCharacterNameInLineView)
                {
                    lineText.text = dialogueLine.Text.Text;
                }
                else
                {
                    lineText.text = dialogueLine.TextWithoutCharacterName.Text;
                }
            }
            else
            {
                characterNameText.text = dialogueLine.CharacterName;
                lineText.text = dialogueLine.TextWithoutCharacterName.Text;
            }

            if (useFadeEffect)
            {
                if (useTypewriterEffect)
                {
                    // If we're also using a typewriter effect, ensure that
                    // there are no visible characters so that we don't
                    // fade in on the text fully visible
                    lineText.maxVisibleCharacters = 0;
                }
                else
                {
                    // Ensure that the max visible characters is effectively unlimited.
                    lineText.maxVisibleCharacters = int.MaxValue;
                }

                // Fade up and then call FadeComplete when done
                StartCoroutine(Effects.FadeAlpha(canvasGroup, 0, 1, fadeInTime, () => FadeComplete(onDialogueLineFinished), interruptionFlag));
            }
            else
            {
                // Immediately appear 
                canvasGroup.interactable = true;
                canvasGroup.alpha = 1;
                canvasGroup.blocksRaycasts = true;

                if (useTypewriterEffect)
                {
                    // Start the typewriter
                    StartCoroutine(Effects.Typewriter(lineText, typewriterEffectSpeed, OnCharacterTyped, onDialogueLineFinished, interruptionFlag));
                }
                else
                {
                    onDialogueLineFinished();
                }
            }

            void FadeComplete(Action onFinished)
            {
                if (useTypewriterEffect)
                {
                    StartCoroutine(Effects.Typewriter(lineText, typewriterEffectSpeed, OnCharacterTyped, onFinished, interruptionFlag));
                }
                else
                {
                    onFinished();
                }
            }
        }

        public void OnContinueClicked()
        {
            if (currentLine == null)
            {
                // We're not actually displaying a line. No-op.
                return;
            }
            ReadyForNextLine();
        }
    }
}
