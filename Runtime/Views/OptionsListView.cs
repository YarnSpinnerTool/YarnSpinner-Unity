using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Yarn.Unity
{
    public class OptionsListView : DialogueViewBase
    {
        [SerializeField] CanvasGroup canvasGroup;

        [SerializeField] OptionView optionViewPrefab;

        [SerializeField] TextMeshProUGUI lastLineText;

        [SerializeField] float fadeTime = 0.1f;

        // A cached pool of OptionView objects so that we can reuse them
        private List<OptionView> optionViews = new List<OptionView>();

        // The method we should call when an option has been selected.
        private Action<int> OnOptionSelected;

        // The line we saw most recently.
        private LocalizedLine lastSeenLine;

        public void Start() {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void Reset() {
            canvasGroup = GetComponentInParent<CanvasGroup>();
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            // Don't do anything with this line except note it and
            // immediately indicate that we're finished with it. RunOptions
            // will use it to display the text of the previous line.
            lastSeenLine = dialogueLine;
            onDialogueLineFinished();
        }

        public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            // Hide all existing option views
            foreach (var optionView in optionViews) {
                optionView.gameObject.SetActive(false);
            }

            // If we don't already have enough option views, create more
            while (dialogueOptions.Length > optionViews.Count) {
                CreateNewOptionView();
            }

            // Set up all of the option views
            for (int i = 0; i < dialogueOptions.Length; i++) {

                var optionView = optionViews[i];
                var option = dialogueOptions[i];

                optionView.gameObject.SetActive(true);

                optionView.Option = option;

                // The first option is selected by default
                if (i == 0) {
                    optionView.Select();
                }
            }

            // Update the last line, if one is configured
            if (lastLineText != null) {
                lastLineText.text = lastSeenLine.Text.Text;
            }

            // Note the delegate to call when an option is selected
            OnOptionSelected = onOptionSelected;

            // Fade it all in
            StartCoroutine(Effects.FadeAlpha(canvasGroup, 0, 1, fadeTime));
        }

        /// <summary>
        /// /// Creates and configures a new <see cref="OptionView"/>, and adds
        /// it to <see cref="optionViews"/>.
        /// </summary>
        private void CreateNewOptionView()
        {
            var optionView = Instantiate(optionViewPrefab);
            optionView.transform.SetParent(transform,false);
            optionView.transform.SetAsLastSibling();

            optionView.OnOptionSelected = OptionViewWasSelected;
            optionViews.Add(optionView);
        }

        /// <summary>
        /// Called by <see cref="OptionView"/> objects.
        /// </summary>
        private void OptionViewWasSelected(DialogueOption option) {
            StartCoroutine(Effects.FadeAlpha(canvasGroup, 1, 0, fadeTime, () => OnOptionSelected(option.DialogueOptionID)));
            ;
        }
    }
}
