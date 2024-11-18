using System.Collections.Generic;
using UnityEngine;
using Yarn.Markup;
using System.Threading;

#if USE_TMP
    using TMPro;
#else
    using TextMeshProUGUI = Yarn.Unity.TMPShim;
#endif

#if USE_UNITASK
    using Cysharp.Threading.Tasks;
    using YarnTask = Cysharp.Threading.Tasks.UniTask;
    using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.DialogueOption>;
#elif UNITY_2023_1_OR_NEWER
    using YarnTask = UnityEngine.Awaitable;
    using YarnOptionTask = UnityEngine.Awaitable<Yarn.Unity.DialogueOption>;
#else
    using YarnTask = System.Threading.Tasks.Task;
    using YarnOptionTask = System.Threading.Tasks.Task<Yarn.Unity.DialogueOption>;
#endif

namespace Yarn.Unity
{
    public class AsyncLineView : AsyncDialogueViewBase
    {
        // main ui fields
        public CanvasGroup canvas;
        public TMP_Text LineText;
        public UnityEngine.UI.Button continueButton;

        // Showing character name fields
        [Group("Character")] [Label("Shows Name")]
        public bool ShowsCharacterName = true;

        [Group("Character")] [ShowIf(nameof(ShowsCharacterName))][Label("Name field")]
        public TMP_Text CharacterName;

        [Group("Character")] [ShowIf(nameof(ShowsCharacterName))]
        public GameObject characterNameContainer = null;


        // fade up and down fields
        [Group("Fade")] [Label("Fade UI")]
        public bool fade = true;

        [Group("Fade")] [ShowIf(nameof(fade))]
        public float fadeUpDuration = 0.25f;

        [Group("Fade")] [ShowIf(nameof(fade))]
        public float fadeDownDuration = 0.1f;


        // auto advance dialogue fields
        [Group("Automatically Advance Dialogue")]
        public bool AutoAdvance = false;

        [Group("Automatically Advance Dialogue")] [ShowIf(nameof(AutoAdvance))] [Label("Delay before advancing")]
        public float AutoAdvanceDelay = 0;


        // typewriter fields
        [Group("Typewriter")]
        public bool TypewriterEffect = true;
        
        [Group("Typewriter")] [ShowIf(nameof(TypewriterEffect))] [Label("Letters per second")]
        public int TypewriterLettersPerSecond = 60;
        
        [Group("Typewriter")] [ShowIf(nameof(TypewriterEffect))]
        public UnityEngine.Events.UnityEvent onCharacterTyped;
        private TypewriterHandler typewriter;
        
        public List<TemporalMarkupHandler> temporalProcessors = new List<TemporalMarkupHandler>();

        public override YarnTask OnDialogueCompleteAsync()
        {
            canvas.alpha = 0;
            return YarnAsync.CompletedTask;
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            canvas.alpha = 0;
            return YarnAsync.CompletedTask;
        }


        private DialogueRunner runner;
        public void Awake()
        {
            if (TypewriterEffect)
            {
                typewriter = gameObject.AddComponent<TypewriterHandler>();
                typewriter.lettersPerSecond = TypewriterLettersPerSecond;
                typewriter.onCharacterTyped = onCharacterTyped;
                typewriter.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInEditor;
                temporalProcessors.Add(typewriter);
            }

            if (characterNameContainer == null && CharacterName != null)
            {
                characterNameContainer = CharacterName.gameObject;
            }

            runner = FindObjectOfType<DialogueRunner>();
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            MarkupParseResult text;
            // configuring the text fields
            if (ShowsCharacterName)
            {
                CharacterName.text = line.CharacterName;
                text = line.TextWithoutCharacterName;
            }
            else
            {
                // we don't want to show character names but do have a valid container for showing them
                // so we should just disable that and continue as if it didn't exist
                if (characterNameContainer != null)
                {
                    characterNameContainer.SetActive(false);
                }
                text = line.Text;
            }
            LineText.text = text.Text;

            // setting the continue button up to let us advance dialogue
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(() => runner.CancelCurrentLine());
            }

            // letting every temporal processor know that fade up (if set) is about to begin
            if (temporalProcessors.Count > 0)
            {
                foreach (var processor in temporalProcessors)
                {
                    processor.PreFadeSetup(text, LineText);
                }
            }

            // fading up the UI
            if (fade)
            {
                await Effects.FadeAlpha(canvas, 0, 1, fadeDownDuration, token.SoftToken);
            }
            else
            {
                canvas.alpha = 1;
            }

            if (temporalProcessors.Count > 0)
            {
                // letting every temporal processor know that fading is done and display is about to begin
                foreach (var processor in temporalProcessors)
                {
                    processor.PrepareForMarkup(text, LineText);
                }

                // going through each character of the line and letting the processors know about it
                for (int i = 0; i < text.Text.Length; i++)
                {
                    // telling every processor that it is time to process the current character
                    foreach (var processor in temporalProcessors)
                    {
                        // if the typewriter exists we need to turn it on and off depending on if a line is blocking or not
                        if (TypewriterEffect)
                        {
                            var task = processor.ProcessMarkedUpCharacter(i, LineText, token.SoftToken);
                            if (!task.IsCompleted && processor != typewriter)
                            {
                                typewriter.stopwatchRunning = false;
                            }
                            await task;
                            typewriter.stopwatchRunning = true;
                        }
                        else
                        {
                            await processor.ProcessMarkedUpCharacter(i, LineText, token.SoftToken);
                        }
                    }
                }

                // letting each temporal processor know the line has finished displaying
                foreach (var processor in temporalProcessors)
                {
                    processor.LineDisplayComplete();
                }
            }

            // if we are set to autoadvance how long do we hold for before continuing?
            if (AutoAdvance)
            {
                await YarnAsync.Delay((int)(AutoAdvanceDelay * 1000), token.HardToken);
            }
            else
            {
                await YarnAsync.WaitUntilCanceled(token.HardToken);
            }

            // we fade down the UI
            if (fade)
            {
                await Effects.FadeAlpha(canvas, 1, 0, fadeDownDuration, token.SoftToken);
            }
            else
            {
                canvas.alpha = 0;
            }

            // the final bit of clean up is to remove the cancel listener from the button
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
            }
        }

        public override YarnOptionTask RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            return YarnAsync.NoOptionSelected;
        }
    }

    public abstract class TemporalMarkupHandler: MonoBehaviour
    {
        public abstract void PreFadeSetup(MarkupParseResult line, TMP_Text text);
        public abstract void PrepareForMarkup(MarkupParseResult line, TMP_Text text);
        public abstract YarnTask ProcessMarkedUpCharacter(int currentCharacterIndex, TMP_Text text, CancellationToken cancellationToken);
        public abstract void LineDisplayComplete();
    }

    public sealed class TypewriterHandler: TemporalMarkupHandler
    {
        public int lettersPerSecond = 60;
        public UnityEngine.Events.UnityEvent onCharacterTyped;

        private float accumulatedTime = 0;
        internal bool stopwatchRunning = false;
        private Stack<(int position, float duration)> pauses;
        private float accumulatedPauses = 0;

        private float secondsPerLetter
        {
            get
            {
                return 1f / lettersPerSecond;
            }
        }

        void Update()
        {
            if (stopwatchRunning)
            {
                accumulatedTime += Time.deltaTime;
            }
        }

        public override void PreFadeSetup(MarkupParseResult line, TMP_Text text)
        {
            text.maxVisibleCharacters = 0;
            accumulatedPauses = 0;
        }
        public override void PrepareForMarkup(MarkupParseResult line, TMP_Text text)
        {
            pauses = LineView.GetPauseDurationsInsideLine(line);

            accumulatedTime = 0;
            stopwatchRunning = true;
        }
        public override void LineDisplayComplete()
        {
            stopwatchRunning = false;
            accumulatedTime = 0;
            pauses.Clear();
            accumulatedPauses = 0;
        }
        public override async YarnTask ProcessMarkedUpCharacter(int currentCharacterIndex, TMP_Text text, CancellationToken cancellationToken)
        {
            float pauseDuration = 0;
            if (pauses.Count > 0)
            {
                var pause = pauses.Peek();
                if (pause.position == currentCharacterIndex)
                {
                    pauses.Pop();
                    pauseDuration = pause.duration;
                }
            }
            accumulatedPauses += pauseDuration;

            float timePoint = accumulatedPauses;
            if (lettersPerSecond > 0)
            {
                timePoint += (float)currentCharacterIndex * secondsPerLetter;
            }

            await YarnAsync.WaitUntil(() => accumulatedTime >= timePoint, cancellationToken);
            text.maxVisibleCharacters += 1;
            onCharacterTyped.Invoke();
        }
    }
}
