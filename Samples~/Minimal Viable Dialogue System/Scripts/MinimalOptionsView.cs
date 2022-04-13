using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using TMPro;

public class MinimalOptionsView : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;

    [SerializeField] OptionView optionViewPrefab;

    [SerializeField] TextMeshProUGUI lastLineText;

    [SerializeField] float fadeTime = 0.1f;

    [SerializeField] bool showUnavailableOptions = false;

    // A cached pool of OptionView objects so that we can reuse them
    List<OptionView> optionViews = new List<OptionView>();

    // The line we saw most recently.
    LocalizedLine lastSeenLine;

    private MinimalDialogueRunner runner;

    public void Start()
    {
        runner = FindObjectOfType<MinimalDialogueRunner>();

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Reset()
    {
        canvasGroup = GetComponentInParent<CanvasGroup>();
    }

    public void RunLine(LocalizedLine dialogueLine)
    {
        // Don't do anything with this line except note it and
        // immediately indicate that we're finished with it. RunOptions
        // will use it to display the text of the previous line.
        lastSeenLine = dialogueLine;
    }

    public void RunOptions(DialogueOption[] options)
    {
        // Note the delegate to call when an option is selected
        System.Action<Yarn.Unity.DialogueOption> onOptionSelected = delegate(DialogueOption selectedOption)
        {
            StartCoroutine(OptionViewWasSelectedInternal(selectedOption));
            IEnumerator OptionViewWasSelectedInternal(DialogueOption selectedOption)
            {
                yield return StartCoroutine(Yarn.Unity.Effects.FadeAlpha(canvasGroup, 1, 0, fadeTime));
                runner.SetSelectedOption(selectedOption.DialogueOptionID);
            }
        };
        
        // Hide all existing option views
        foreach (var optionView in optionViews)
        {
            optionView.gameObject.SetActive(false);
        }

        // If we don't already have enough option views, create more
        while (options.Length > optionViews.Count)
        {
            var optionView = CreateNewOptionView(onOptionSelected);
            optionView.gameObject.SetActive(false);
        }

        // Set up all of the option views
        int optionViewsCreated = 0;

        for (int i = 0; i < options.Length; i++)
        {
            var optionView = optionViews[i];
            var option = options[i];

            if (option.IsAvailable == false && showUnavailableOptions == false)
            {
                // Don't show this option.
                continue;
            }

            optionView.gameObject.SetActive(true);

            optionView.Option = option;
            optionView.OnOptionSelected = onOptionSelected;

            // The first available option is selected by default
            if (optionViewsCreated == 0)
            {
                optionView.Select();
            }

            optionViewsCreated += 1;
        }

        // Update the last line, if one is configured
        if (lastLineText != null)
        {
            if (lastSeenLine != null)
            {
                lastLineText.gameObject.SetActive(true);
                lastLineText.text = lastSeenLine.Text.Text;
            }
            else
            {
                lastLineText.gameObject.SetActive(false);
            }
        }

        // Fade it all in
        StartCoroutine(Effects.FadeAlpha(canvasGroup, 0, 1, fadeTime));

        /// <summary>
        /// Creates and configures a new <see cref="OptionView"/>, and adds
        /// it to <see cref="optionViews"/>.
        /// </summary>
        OptionView CreateNewOptionView(System.Action<Yarn.Unity.DialogueOption> callback)
        {
            var optionView = Instantiate(optionViewPrefab);
            optionView.transform.SetParent(transform, false);
            optionView.transform.SetAsLastSibling();

            optionView.OnOptionSelected = callback;
            optionViews.Add(optionView);

            return optionView;
        }
    }
}
