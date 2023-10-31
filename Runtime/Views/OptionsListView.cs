using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = TMPShim;
#endif

namespace Yarn.Unity
{
    public class OptionsListView : DialogueViewBase
    {
        [SerializeField] CanvasGroup canvasGroup;

        [SerializeField] OptionView optionViewPrefab;

        [SerializeField] MarkupPalette palette;

        [SerializeField] float fadeTime = 0.1f;

        [SerializeField] bool showUnavailableOptions = false;

        [Header("Last Line Components")]
        [SerializeField] TextMeshProUGUI lastLineText;
        [SerializeField] GameObject lastLineContainer;

        [SerializeField] TextMeshProUGUI lastLineCharacterNameText;
        [SerializeField] GameObject lastLineCharacterNameContainer;

        // A cached pool of OptionView objects so that we can reuse them
        List<OptionView> optionViews = new List<OptionView>();

        // The method we should call when an option has been selected.
        Action<int> OnOptionSelected;

        // The line we saw most recently.
        LocalizedLine lastSeenLine;

        public void Start()
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void Reset()
        {
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
            // If we don't already have enough option views, create more
            while (dialogueOptions.Length > optionViews.Count)
            {
                var optionView = CreateNewOptionView();
                optionView.gameObject.SetActive(false);
            }

            // Set up all of the option views
            int optionViewsCreated = 0;

            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var optionView = optionViews[i];
                var option = dialogueOptions[i];

                if (option.IsAvailable == false && showUnavailableOptions == false)
                {
                    // Don't show this option.
                    continue;
                }

                optionView.gameObject.SetActive(true);

                optionView.palette = this.palette;
                optionView.Option = option;

                // The first available option is selected by default
                if (optionViewsCreated == 0)
                {
                    optionView.Select();
                }

                optionViewsCreated += 1;
            }

            // Update the last line, if one is configured
            if (lastLineContainer != null)
            {
                if (lastSeenLine != null)
                {
                    // if we have a last line character name container
                    // and the last line has a character then we show the nameplate
                    // otherwise we turn off the nameplate
                    var line = lastSeenLine.Text;
                    if (lastLineCharacterNameContainer != null)
                    {
                        if (string.IsNullOrWhiteSpace(lastSeenLine.CharacterName))
                        {
                            lastLineCharacterNameContainer.SetActive(false);
                        }
                        else
                        {
                            line = lastSeenLine.TextWithoutCharacterName;
                            lastLineCharacterNameContainer.SetActive(true);
                            lastLineCharacterNameText.text = lastSeenLine.CharacterName;
                        }
                    }

                    if (palette != null)
                    {
                        lastLineText.text = LineView.PaletteMarkedUpText(line, palette);
                    }
                    else
                    {
                        lastLineText.text = line.Text;
                    }

                    lastLineContainer.SetActive(true);
                }
                else
                {
                    lastLineContainer.SetActive(false);
                }
            }

            // Note the delegate to call when an option is selected
            OnOptionSelected = onOptionSelected;

            // sometimes (not always) the TMP layout in conjunction with the
            // content size fitters doesn't update the rect transform
            // until the next frame, and you get a weird pop as it resizes
            // just forcing this to happen now instead of then
            Relayout();

            // Fade it all in
            StartCoroutine(Effects.FadeAlpha(canvasGroup, 0, 1, fadeTime));

            /// <summary>
            /// Creates and configures a new <see cref="OptionView"/>, and adds
            /// it to <see cref="optionViews"/>.
            /// </summary>
            OptionView CreateNewOptionView()
            {
                var optionView = Instantiate(optionViewPrefab);
                optionView.transform.SetParent(transform, false);
                optionView.transform.SetAsLastSibling();

                optionView.OnOptionSelected = OptionViewWasSelected;
                optionViews.Add(optionView);

                return optionView;
            }

            /// <summary>
            /// Called by <see cref="OptionView"/> objects.
            /// </summary>
            void OptionViewWasSelected(DialogueOption option)
            {
                StartCoroutine(OptionViewWasSelectedInternal(option));

                IEnumerator OptionViewWasSelectedInternal(DialogueOption selectedOption)
                {
                    yield return StartCoroutine(FadeAndDisableOptionViews(canvasGroup, 1, 0, fadeTime));
                    OnOptionSelected(selectedOption.DialogueOptionID);
                }
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// If options are still shown dismisses them.
        /// </remarks>
        public override void DialogueComplete()
        {   
            // do we still have any options being shown?
            if (canvasGroup.alpha > 0)
            {
                StopAllCoroutines();
                lastSeenLine = null;
                OnOptionSelected = null;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                StartCoroutine(FadeAndDisableOptionViews(canvasGroup, canvasGroup.alpha, 0, fadeTime));
            }
        }

        /// <summary>
        /// Fades canvas and then disables all option views.
        /// </summary>
        private IEnumerator FadeAndDisableOptionViews(CanvasGroup canvasGroup, float from, float to, float fadeTime)
        {
            yield return Effects.FadeAlpha(canvasGroup, from, to, fadeTime);

            // Hide all existing option views
            foreach (var optionView in optionViews)
            {
                optionView.gameObject.SetActive(false);
            }
        }

        public void OnEnable()
        {
            Relayout();
        }

        private void Relayout()
        {
            // Force re-layout
            var layouts = GetComponentsInChildren<UnityEngine.UI.LayoutGroup>();

            // Perform the first pass of re-layout. This will update the inner horizontal group's sizing, based on the text size.
            foreach (var layout in layouts)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
            }
            
            // Perform the second pass of re-layout. This will update the outer vertical group's positioning of the individual elements.
            foreach (var layout in layouts)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
            }
        }
    }
}
