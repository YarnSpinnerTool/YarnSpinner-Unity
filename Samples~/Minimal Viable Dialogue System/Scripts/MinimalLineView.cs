/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Yarn.Unity;

// this class is based on the full LineView in Runtime/Views/LineView.cs
// unlike that view this is not a subclass of DialogueViewBase but just a regular game object/
// Reason we used the existing line view was just to reduce the amount of code needed to be written
public class MinimalLineView : MonoBehaviour
{
    [SerializeField] internal CanvasGroup canvasGroup;

    [SerializeField] internal bool useFadeEffect = true;

    [SerializeField]
    [Min(0)]
    internal float fadeInTime = 0.25f;

    [SerializeField]
    [Min(0)]
    internal float fadeOutTime = 0.05f;

    [SerializeField] internal TextMeshProUGUI lineText = null;

    [SerializeField] internal bool showCharacterNameInLineView = true;

    [SerializeField] internal TextMeshProUGUI characterNameText = null;

    [SerializeField] internal bool useTypewriterEffect = false;

    [SerializeField]
    [Min(0)]
    internal float typewriterEffectSpeed = 0f;

    [SerializeField] internal GameObject continueButton = null;

    [SerializeField]
    [Min(0)]
    internal float holdTime = 1f;
    
    LocalizedLine currentLine = null;

    Effects.CoroutineInterruptToken currentStopToken = new Effects.CoroutineInterruptToken();

    private MinimalDialogueRunner runner;

    private void Awake()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }
    void Start()
    {
        runner = FindObjectOfType<MinimalDialogueRunner>();
    }

    private void Reset()
    {
        canvasGroup = GetComponentInParent<CanvasGroup>();
    }

    public void DismissLine()
    {
        currentLine = null;

        StartCoroutine(DismissLineInternal());
    }

    private IEnumerator DismissLineInternal()
    {
        // disabling interaction temporarily while dismissing the line
        // we don't want people to interrupt a dismissal
        var interactable = canvasGroup.interactable;
        canvasGroup.interactable = false;

        // If we're using a fade effect, run it, and wait for it to finish.
        if (useFadeEffect)
        {
            yield return StartCoroutine(Effects.FadeAlpha(canvasGroup, 1, 0, fadeOutTime, currentStopToken));
            currentStopToken.Complete();
        }
        
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        // turning interaction back on, if it needs it
        canvasGroup.interactable = interactable;
        runner.Continue();
    }

    public void RunLine(LocalizedLine dialogueLine)
    {
        // Stop any coroutines currently running on this line view (for
        // example, any other RunLine that might be running)
        StopAllCoroutines();

        // Begin running the line as a coroutine.
        StartCoroutine(RunLineInternal(dialogueLine));
    }

    private IEnumerator RunLineInternal(LocalizedLine dialogueLine)
    {
        IEnumerator PresentLine()
        {
            lineText.gameObject.SetActive(true);
            canvasGroup.gameObject.SetActive(true);

            // Hide the continue button until presentation is complete (if
            // we have one).
            if (continueButton != null)
            {
                continueButton.SetActive(false);
            }

            if (characterNameText != null)
            {
                // If we have a character name text view, show the character
                // name in it, and show the rest of the text in our main
                // text view.
                characterNameText.text = dialogueLine.CharacterName;
                lineText.text = dialogueLine.TextWithoutCharacterName.Text;
            }
            else
            {
                // We don't have a character name text view. Should we show
                // the character name in the main text view?
                if (showCharacterNameInLineView)
                {
                    // Yep! Show the entire text.
                    lineText.text = dialogueLine.Text.Text;
                }
                else
                {
                    // Nope! Show just the text without the character name.
                    lineText.text = dialogueLine.TextWithoutCharacterName.Text;
                }
            }

            if (useTypewriterEffect)
            {
                // If we're using the typewriter effect, hide all of the
                // text before we begin any possible fade (so we don't fade
                // in on visible text).
                lineText.maxVisibleCharacters = 0;
            }
            else
            {
                // Ensure that the max visible characters is effectively
                // unlimited.
                lineText.maxVisibleCharacters = int.MaxValue;
            }

            // If we're using the fade effect, start it, and wait for it to
            // finish.
            if (useFadeEffect)
            {
                yield return StartCoroutine(Effects.FadeAlpha(canvasGroup, 0, 1, fadeInTime, currentStopToken));
                if (currentStopToken.WasInterrupted) {
                    // The fade effect was interrupted. Stop this entire
                    // coroutine.
                    yield break;
                }
            }

            // If we're using the typewriter effect, start it, and wait for
            // it to finish.
            if (useTypewriterEffect)
            {
                // setting the canvas all back to its defaults because if we didn't also fade we don't have anything visible
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                yield return StartCoroutine(
                    Effects.Typewriter(
                        lineText,
                        typewriterEffectSpeed,
                        null,
                        currentStopToken
                    )
                );
                if (currentStopToken.WasInterrupted) {
                    // The typewriter effect was interrupted. Stop this
                    // entire coroutine.
                    yield break;
                }
            }
        }
        currentLine = dialogueLine;

        // Run any presentations as a single coroutine. If this is stopped,
        // which UserRequestedViewAdvancement can do, then we will stop all
        // of the animations at once.
        yield return StartCoroutine(PresentLine());

        currentStopToken.Complete();

        // All of our text should now be visible.
        lineText.maxVisibleCharacters = int.MaxValue;

        // Our view should at be at full opacity.
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // If we have a hold time, wait that amount of time, and then
        // continue.
        if (holdTime > 0)
        {
            yield return new WaitForSeconds(holdTime);
        }

        // Show the continue button, if we have one.
        if (continueButton != null)
        {
            continueButton.SetActive(true);
        }
    }

    public void OnContinueClicked()
    {
        if (currentLine == null)
        {
            return;
        }

        DismissLine();
    }
}
