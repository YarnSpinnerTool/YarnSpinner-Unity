/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity.Attributes;

#nullable enable

#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = Yarn.Unity.TMPShim;
using TMP_Text = Yarn.Unity.TMPShim;
#endif

namespace Yarn.Unity
{
    /// <summary>
    /// A Dialogue Presenter that presents lines of dialogue, using Unity UI
    /// elements.
    /// </summary>
    [HelpURL("https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-view/line-view")]
    public sealed class LinePresenter : DialoguePresenterBase
    {
        internal enum TypewriterType
        {
            Instant, ByLetter, ByWord, Custom,
        }

        /// <summary>
        /// The canvas group that contains the UI elements used by this Line
        /// View.
        /// </summary>
        /// <remarks>
        /// If <see cref="useFadeEffect"/> is true, then the alpha value of this
        /// <see cref="CanvasGroup"/> will be animated during line presentation
        /// and dismissal.
        /// </remarks>
        /// <seealso cref="useFadeEffect"/>
        [Space]
        [MustNotBeNull]
        public CanvasGroup? canvasGroup;

        /// <summary>
        /// The <see cref="TMP_Text"/> object that displays the text of
        /// dialogue lines.
        /// </summary>
        [MustNotBeNull]
        public TMP_Text? lineText;

        /// <summary>
        /// Controls whether the <see cref="lineText"/> object will show the
        /// character name present in the line or not.
        /// </summary>
        /// <remarks>
        /// <para style="note">This value is only used if <see
        /// cref="characterNameText"/> is <see langword="null"/>.</para>
        /// <para>If this value is <see langword="true"/>, any character names
        /// present in a line will be shown in the <see cref="lineText"/>
        /// object.</para>
        /// <para>If this value is <see langword="false"/>, character names will
        /// not be shown in the <see cref="lineText"/> object.</para>
        /// </remarks>
        [Group("Character")]
        [Label("Shows Name In Line")]
        public bool showCharacterNameInLine = true;

        /// <summary>
        /// The <see cref="TMP_Text"/> object that displays the character
        /// names found in dialogue lines.
        /// </summary>
        /// <remarks>
        /// If the <see cref="LinePresenter"/> receives a line that does not contain
        /// a character name, this object will be left blank.
        /// </remarks>
        [Group("Character")]
        [Label("Name Field")]
        public TMP_Text? characterNameText = null;

        /// <summary>
        /// The game object that holds the <see cref="characterNameText"/> text
        /// field.
        /// </summary>
        /// <remarks>
        /// This is needed in situations where the character name is contained
        /// within an entirely different game object. Most of the time this will
        /// just be the same game object as <see cref="characterNameText"/>.
        /// </remarks>
        [Group("Character")]
        public GameObject? characterNameContainer = null;


        /// <summary>
        /// Controls whether the line view should fade in when lines appear, and
        /// fade out when lines disappear.
        /// </summary>
        /// <remarks><para>If this value is <see langword="true"/>, the <see
        /// cref="canvasGroup"/> object's alpha property will animate from 0 to
        /// 1 over the course of <see cref="fadeUpDuration"/> seconds when lines
        /// appear, and animate from 1 to zero over the course of <see
        /// cref="fadeDownDuration"/> seconds when lines disappear.</para>
        /// <para>If this value is <see langword="false"/>, the <see
        /// cref="canvasGroup"/> object will appear instantaneously.</para>
        /// </remarks>
        /// <seealso cref="canvasGroup"/>
        /// <seealso cref="fadeUpDuration"/>
        /// <seealso cref="fadeDownDuration"/>
        [Group("Fade")]
        [Label("Fade UI")]
        public bool useFadeEffect = true;

        /// <summary>
        /// The time that the fade effect will take to fade lines in.
        /// </summary>
        /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
        /// <see langword="true"/>.</remarks>
        /// <seealso cref="useFadeEffect"/>
        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        public float fadeUpDuration = 0.25f;

        /// <summary>
        /// The time that the fade effect will take to fade lines out.
        /// </summary>
        /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
        /// <see langword="true"/>.</remarks>
        /// <seealso cref="useFadeEffect"/>
        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        public float fadeDownDuration = 0.1f;


        /// <summary>
        /// Controls whether this Line View will automatically to the Dialogue
        /// Runner that the line is complete as soon as the line has finished
        /// appearing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value is true, the Line View will 
        /// </para>
        /// <para style="note"><para>The <see cref="DialogueRunner"/> will not
        /// proceed to the next piece of content (e.g. the next line, or the
        /// next options) until all Dialogue Presenters have reported that they have
        /// finished presenting their lines. If a <see cref="LinePresenter"/>
        /// doesn't report that it's finished until it receives input, the <see
        /// cref="DialogueRunner"/> will end up pausing.</para>
        /// <para>
        /// This is useful for games in which you want the player to be able to
        /// read lines of dialogue at their own pace, and give them control over
        /// when to advance to the next line.</para></para>
        /// </remarks>
        [Group("Automatically Advance Dialogue")]
        public bool autoAdvance = false;

        /// <summary>
        /// The amount of time after the line finishes appearing before
        /// automatically ending the line, in seconds.
        /// </summary>
        /// <remarks>This value is only used when <see cref="autoAdvance"/> is
        /// <see langword="true"/>.</remarks>
        [Group("Automatically Advance Dialogue")]
        [ShowIf(nameof(autoAdvance))]
        [Label("Delay Before Advancing")]
        public float autoAdvanceDelay = 1f;


        // typewriter fields

        [Group("Typewriter")]
        [SerializeField] internal TypewriterType typewriterStyle = TypewriterType.ByLetter;

        /// <summary>
        /// The number of characters per second that should appear during a
        /// typewriter effect.
        /// </summary>
        [Group("Typewriter")]
        [ShowIf(nameof(typewriterStyle), TypewriterType.ByLetter)]
        [Label("Letters per Second")]
        [Min(0)]
        public int lettersPerSecond = 60;

        [Group("Typewriter")]
        [ShowIf(nameof(typewriterStyle), TypewriterType.ByWord)]
        [Label("Words per Second")]
        [Min(0)]
        public int wordsPerSecond = 10;

        [Group("Typewriter")]
        [ShowIf(nameof(typewriterStyle), TypewriterType.Custom)]
        [UnityEngine.Serialization.FormerlySerializedAs("CustomTypewriter")]
        [MustNotBeNull("Attach a component that implements the " + nameof(IAsyncTypewriter) + " interface.")]
        public UnityEngine.Object? customTypewriter;

        /// <summary>
        /// A list of <see cref="ActionMarkupHandler"/> objects that will be
        /// used to handle markers in the line.
        /// </summary>
        [Group("Typewriter")]
        [Label("Event Handlers")]
        [UnityEngine.Serialization.FormerlySerializedAs("actionMarkupHandlers")]
        [SerializeField] List<ActionMarkupHandler> eventHandlers = new List<ActionMarkupHandler>();
        private List<IActionMarkupHandler> ActionMarkupHandlers
        {
            get
            {
                var pauser = new PauseEventProcessor();
                List<IActionMarkupHandler> ActionMarkupHandlers = new()
                {
                    pauser,
                };
                ActionMarkupHandlers.AddRange(eventHandlers);
                return ActionMarkupHandlers;
            }
        }

        /// <inheritdoc/>
        public override YarnTask OnDialogueCompleteAsync()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }
            return YarnTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override YarnTask OnDialogueStartedAsync()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
            }
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Called by Unity on first frame.
        /// </summary>
        private void Awake()
        {
            if (characterNameContainer == null && characterNameText != null)
            {
                characterNameContainer = characterNameText.gameObject;
            }

            switch (typewriterStyle)
            {
                case TypewriterType.Instant:
                    Typewriter = new InstantTypewriter()
                    {
                        ActionMarkupHandlers = ActionMarkupHandlers,
                        Text = this.lineText,
                    };
                    break;

                case TypewriterType.ByLetter:
                    Typewriter = new LetterTypewriter()
                    {
                        ActionMarkupHandlers = ActionMarkupHandlers,
                        Text = this.lineText,
                        CharactersPerSecond = this.lettersPerSecond,
                    };
                    break;

                case TypewriterType.ByWord:
                    Typewriter = new WordTypewriter()
                    {
                        ActionMarkupHandlers = ActionMarkupHandlers,
                        Text = this.lineText,
                        WordsPerSecond = this.wordsPerSecond,
                    };
                    break;

                case TypewriterType.Custom:
                    Typewriter = ValidateCustomTypewriter();
                    Typewriter?.ActionMarkupHandlers.AddRange(ActionMarkupHandlers);
                    if (Typewriter == null)
                    {
                        Debug.LogWarning("Typewriter mode is set to custom but there is no typewriter set.");
                    }
                    break;
            }
        }

        void OnValidate()
        {
            var tw = ValidateCustomTypewriter();
            if (tw == null)
            {
                customTypewriter = null;
            }
            else
            {
                customTypewriter = tw as Component;
            }
        }

        private IAsyncTypewriter? ValidateCustomTypewriter()
        {
            if (customTypewriter is GameObject gameObject)
            {
                foreach (var component in gameObject.GetComponents<Component>())
                {
                    if (component is IAsyncTypewriter)
                    {
                        customTypewriter = component;
                        return component as IAsyncTypewriter;
                    }
                }
            }

            if (customTypewriter is Component)
            {
                if (customTypewriter is IAsyncTypewriter)
                {
                    return customTypewriter as IAsyncTypewriter;
                }
            }

            return null;
        }

        /// <summary>Presents a line using the configured text view.</summary>
        /// <inheritdoc cref="DialoguePresenterBase.RunLineAsync(LocalizedLine, LineCancellationToken)" path="/param"/>
        /// <inheritdoc cref="DialoguePresenterBase.RunLineAsync(LocalizedLine, LineCancellationToken)" path="/returns"/>
        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (lineText == null)
            {
                Debug.LogError($"{nameof(LinePresenter)} does not have a text view. Skipping line {line.TextID} (\"{line.RawText}\")");
                return;
            }

            MarkupParseResult text;

            // configuring the text fields
            if (characterNameText == null)
            {
                if (showCharacterNameInLine)
                {
                    text = line.Text;
                }
                else
                {
                    text = line.TextWithoutCharacterName;
                }
            }
            else
            {
                text = line.TextWithoutCharacterName;

                // we are configured to show character names in their own little box, but this line doesn't have one
                if (characterNameContainer != null)
                {
                    if (string.IsNullOrWhiteSpace(line.CharacterName))
                    {
                        characterNameContainer.SetActive(false);
                    }
                    else
                    {
                        characterNameContainer.SetActive(true);
                        characterNameText.text = line.CharacterName;
                    }
                }
            }

            Typewriter ??= new InstantTypewriter()
            {
                ActionMarkupHandlers = this.ActionMarkupHandlers,
                Text = this.lineText,
            };

            Typewriter.PrepareForContent(text);

            if (canvasGroup != null)
            {
                // fading up the UI
                if (useFadeEffect)
                {
                    await Effects.FadeAlphaAsync(canvasGroup, 0, 1, fadeUpDuration, token.HurryUpToken);
                }
                else
                {
                    // We're not fading up, so set the canvas group's alpha to 1 immediately.
                    canvasGroup.alpha = 1;
                }
            }

            await Typewriter.RunTypewriter(text, token.HurryUpToken).SuppressCancellationThrow();

            // if we are set to autoadvance how long do we hold for before continuing?
            if (autoAdvance)
            {
                await YarnTask.Delay((int)(autoAdvanceDelay * 1000), token.NextContentToken).SuppressCancellationThrow();
            }
            else
            {
                await YarnTask.WaitUntilCanceled(token.NextContentToken).SuppressCancellationThrow();
            }

            Typewriter.ContentWillDismiss();

            if (canvasGroup != null)
            {
                // we fade down the UI
                if (useFadeEffect)
                {
                    await Effects.FadeAlphaAsync(canvasGroup, 1, 0, fadeDownDuration, token.HurryUpToken).SuppressCancellationThrow();
                }
                else
                {
                    canvasGroup.alpha = 0;
                }
            }
        }
    }
}
