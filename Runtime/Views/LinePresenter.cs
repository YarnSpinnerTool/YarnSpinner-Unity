/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Markup;
using Yarn.Unity.Attributes;

#nullable enable

#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = Yarn.Unity.TMPShim;
#endif

namespace Yarn.Unity
{
    /// <summary>
    /// A Dialogue View that presents lines of dialogue, using Unity UI
    /// elements.
    /// </summary>
    [HelpURL("https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-view/line-view")]
    public sealed class LinePresenter : DialoguePresenterBase
    {
        [MustNotBeNullWhen(nameof(continueButton), "A " + nameof(DialogueRunner) + " must be provided when a continue button is set.")]
        [SerializeField] DialogueRunner? dialogueRunner;

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
        /// The <see cref="Button"/> that represents an on-screen button that
        /// the user can click to continue to the next piece of dialogue.
        /// </summary>
        /// <remarks>
        /// <para>This button's game object will be made inactive when a line
        /// begins appearing, and active when the line has finished
        /// appearing.</para>
        /// <para>When the button is clicked, <see cref="dialogueRunner"/>'s
        /// <see cref="DialogueRunner.RequestNextLine"/> will be called to
        /// signal that the current line should finish up.</para>
        /// </remarks>
        /// <seealso cref="autoAdvance"/>
        public Button? continueButton;

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
        [Label("Shows Name")]
        public bool showCharacterNameInLineView = true;

        /// <summary>
        /// The <see cref="TMP_Text"/> object that displays the character
        /// names found in dialogue lines.
        /// </summary>
        /// <remarks>
        /// If the <see cref="LineView"/> receives a line that does not contain
        /// a character name, this object will be left blank.
        /// </remarks>
        [Group("Character")]
        [ShowIf(nameof(showCharacterNameInLineView))]
        [Label("Name field")]
        [MustNotBeNullWhen(nameof(showCharacterNameInLineView), "A text field must be provided when Shows Name is set")]
        public TMP_Text? characterNameText;

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
        [ShowIf(nameof(showCharacterNameInLineView))]
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
        /// next options) until all Dialogue Views have reported that they have
        /// finished presenting their lines. If a <see cref="LineView"/> doesn't
        /// report that it's finished until it receives input, the <see
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
        [Label("Delay before advancing")]
        public float autoAdvanceDelay = 1f;


        // typewriter fields

        /// <summary>
        /// Controls whether the text of <see cref="lineText"/> should be
        /// gradually revealed over time.
        /// </summary>
        /// <remarks><para>If this value is <see langword="true"/>, the <see
        /// cref="lineText"/> object's <see
        /// cref="TMP_Text.maxVisibleCharacters"/> property will animate from 0
        /// to the length of the text, at a rate of <see
        /// cref="typewriterEffectSpeed"/> letters per second when the line
        /// appears. <see cref="onCharacterTyped"/> is called for every new
        /// character that is revealed.</para>
        /// <para>If this value is <see langword="false"/>, the <see
        /// cref="lineText"/> will all be revealed at the same time.</para>
        /// <para style="note">If <see cref="useFadeEffect"/> is <see
        /// langword="true"/>, the typewriter effect will run after the fade-in
        /// is complete.</para>
        /// </remarks>
        /// <seealso cref="lineText"/>
        /// <seealso cref="onCharacterTyped"/>
        /// <seealso cref="typewriterEffectSpeed"/>
        [Group("Typewriter")]
        public bool useTypewriterEffect = true;

        /// <summary>
        /// The number of characters per second that should appear during a
        /// typewriter effect.
        /// </summary>
        [Group("Typewriter")]
        [ShowIf(nameof(useTypewriterEffect))]
        [Label("Letters per second")]
        [Min(0)]
        public int typewriterEffectSpeed = 60;

        /// <summary>
        /// A list of <see cref="ActionMarkupHandler"/> objects that will be
        /// used to handle markers in the line.
        /// </summary>
        [Group("Typewriter")]
        [ShowIf(nameof(useTypewriterEffect))]
        [Label("Event Processors")]
        public List<ActionMarkupHandler> temporalProcessors = new List<ActionMarkupHandler>();

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
        protected void Awake()
        {
            if (useTypewriterEffect)
            {
                // need to add a pause handler also
                // and add it to the front of the list
                // that way it always happens first
                var pauser = gameObject.AddComponent<PauseEventProcessor>();
                temporalProcessors.Insert(0, pauser);
            }

            if (characterNameContainer == null && characterNameText != null)
            {
                characterNameContainer = characterNameText.gameObject;
            }

            if (dialogueRunner == null)
            {
                // If we weren't provided with a dialogue runner at design time, try to find one now
                dialogueRunner = FindAnyObjectByType<DialogueRunner>();
                if (dialogueRunner == null)
                {
                    Debug.LogWarning($"{nameof(LinePresenter)} failed to find a dialogue runner! Please ensure that a {nameof(DialogueRunner)} is present, or set the {nameof(dialogueRunner)} property in the Inspector.", this);
                }
            }
        }

        /// <summary>Presents a line using the configured text view.</summary>
        /// <inheritdoc cref="DialoguePresenterBase.RunLineAsync(LocalizedLine, LineCancellationToken)" path="/param"/>
        /// <inheritdoc cref="DialoguePresenterBase.RunLineAsync(LocalizedLine, LineCancellationToken)" path="/returns"/>
        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (lineText == null)
            {
                Debug.LogError($"Line view does not have a text view. Skipping line {line.TextID} (\"{line.RawText}\")");
                return;
            }

            MarkupParseResult text;

            // configuring the text fields
            if (showCharacterNameInLineView)
            {
                if (characterNameText == null)
                {
                    Debug.LogWarning($"Line view is configured to show character names, but no character name text view was provided.", this);
                }
                else
                {
                    characterNameText.text = line.CharacterName;
                }
                text = line.TextWithoutCharacterName;

                if (line.Text.TryGetAttributeWithName("character", out var characterAttribute))
                {
                    text.Attributes.Add(characterAttribute);
                }
            }
            else
            {
                // we don't want to show character names but do have a valid container for showing them
                // so we should just disable that and continue as if it didn't exist
                if (characterNameContainer != null)
                {
                    characterNameContainer.SetActive(false);
                }
                text = line.TextWithoutCharacterName;
            }
            lineText.text = text.Text;

            // note to self: this can be removed and made it's own view
            // setting the continue button up to let us advance dialogue
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(() =>
                {
                    if (dialogueRunner == null)
                    {
                        Debug.LogWarning($"Continue button was clicked, but {nameof(dialogueRunner)} is null!", this);
                        return;
                    }

                    dialogueRunner.RequestNextLine();
                });
            }

            // the typewriter requires all characters to be hidden at the start so they can be shown one at a time
            if (useTypewriterEffect)
            {
                lineText.maxVisibleCharacters = 0;
                // letting every temporal processor know that fade up (if set) is about to begin
                foreach (var processor in temporalProcessors)
                {
                    processor.OnPrepareForLine(text, lineText);
                }
            }
            else
            {
                lineText.maxVisibleCharacters = text.Text.Length;
            }

            if (canvasGroup != null)
            {
                // fading up the UI
                if (useFadeEffect)
                {
                    await Effects.FadeAlphaAsync(canvasGroup, 0, 1, fadeDownDuration, token.HurryUpToken);
                }
                else
                {
                    // We're not fading up, so set the canvas group's alpha to 1 immediately.
                    canvasGroup.alpha = 1;
                }
            }

            if (useTypewriterEffect)
            {
                // letting every temporal processor know that fading is done and display is about to begin
                foreach (var processor in temporalProcessors)
                {
                    processor.OnLineDisplayBegin(text, lineText);
                }

                int milliSecondsPerLetter = 0;
                if (typewriterEffectSpeed > 0)
                {
                    milliSecondsPerLetter = (int)(1000f / typewriterEffectSpeed);
                }

                // going through each character of the line and letting the processors know about it
                for (int i = 0; i < text.Text.Length; i++)
                {
                    // telling every processor that it is time to process the current character
                    foreach (var processor in temporalProcessors)
                    {
                        await processor.OnCharacterWillAppear(i, text, token.HurryUpToken).SuppressCancellationThrow();
                    }

                    lineText.maxVisibleCharacters += 1;
                    if (milliSecondsPerLetter > 0)
                    {
                        await YarnTask.Delay(System.TimeSpan.FromMilliseconds(milliSecondsPerLetter), token.HurryUpToken).SuppressCancellationThrow();
                    }
                }

                lineText.maxVisibleCharacters = text.Text.Length;

                // letting each temporal processor know the line has finished displaying
                foreach (var processor in temporalProcessors)
                {
                    processor.OnLineDisplayComplete();
                }
            }

            // if we are set to autoadvance how long do we hold for before continuing?
            if (autoAdvance)
            {
                await YarnTask.Delay((int)(autoAdvanceDelay * 1000), token.NextLineToken).SuppressCancellationThrow();
            }
            else
            {
                await YarnTask.WaitUntilCanceled(token.NextLineToken).SuppressCancellationThrow();
            }

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

            // the final bit of clean up is to remove the cancel listener from the button
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
            }
        }

        /// <inheritdoc cref="DialoguePresenterBase.RunOptionsAsync(DialogueOption[], CancellationToken)" path="/summary"/> 
        /// <inheritdoc cref="DialoguePresenterBase.RunOptionsAsync(DialogueOption[], CancellationToken)" path="/param"/> 
        /// <inheritdoc cref="DialoguePresenterBase.RunOptionsAsync(DialogueOption[], CancellationToken)" path="/returns"/> 
        /// <remarks>
        /// This dialogue view does not handle any options.
        /// </remarks>
        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            return YarnTask<DialogueOption?>.FromResult(null);
        }
    }
}
