/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity.Attributes;

#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = Yarn.Unity.TMPShim;
#endif

#nullable enable

using System.Threading;

namespace Yarn.Unity
{
    /// <summary>
    /// Receives options from a <see cref="DialogueRunner"/>, and displays and
    /// manages a collection of <see cref="OptionItem"/> views for the user
    /// to choose from.
    /// </summary>
    [HelpURL("https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-view/options-list-view")]
    public sealed class OptionsPresenter : DialoguePresenterBase
    {
        [SerializeField] CanvasGroup? canvasGroup;

        [MustNotBeNull]
        [SerializeField] OptionItem? optionViewPrefab;

        // A cached pool of OptionView objects so that we can reuse them
        List<OptionItem> optionViews = new List<OptionItem>();

        [Space]
        [SerializeField] bool showsLastLine;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [MustNotBeNullWhen(nameof(showsLastLine))]
        [SerializeField] TextMeshProUGUI? lastLineText;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [SerializeField] GameObject? lastLineContainer;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [SerializeField] TextMeshProUGUI? lastLineCharacterNameText;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
        [SerializeField] GameObject? lastLineCharacterNameContainer;

        LocalizedLine? lastSeenLine;

        /// <summary>
        /// Controls whether or not to display options whose <see
        /// cref="OptionSet.Option.IsAvailable"/> value is <see
        /// langword="false"/>.
        /// </summary>
        [Space]
        public bool showUnavailableOptions = false;

        [Group("Fade")]
        [Label("Fade UI")]
        public bool useFadeEffect = true;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        public float fadeUpDuration = 0.25f;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        public float fadeDownDuration = 0.1f;

        private const string TruncateLastLineMarkupName = "lastline";

        /// <summary>
        /// Called by a <see cref="DialogueRunner"/> to dismiss the options view
        /// when dialogue is complete.
        /// </summary>
        /// <returns>A completed task.</returns>
        public override YarnTask OnDialogueCompleteAsync()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Called by Unity to set up the object.
        /// </summary>
        private void Start()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (lastLineContainer == null && lastLineText != null)
            {
                lastLineContainer = lastLineText.gameObject;
            }
            if (lastLineCharacterNameContainer == null && lastLineCharacterNameText != null)
            {
                lastLineCharacterNameContainer = lastLineCharacterNameText.gameObject;
            }
        }

        /// <summary>
        /// Called by a <see cref="DialogueRunner"/> to set up the options view
        /// when dialogue begins.
        /// </summary>
        /// <returns>A completed task.</returns>
        public override YarnTask OnDialogueStartedAsync()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Called by a <see cref="DialogueRunner"/> when a line needs to be
        /// presented, and stores the line as the 'last seen line' so that it
        /// can be shown when options appear.
        /// </summary>
        /// <remarks>This view does not display lines directly, but instead
        /// stores lines so that when options are run, the last line that ran
        /// before the options appeared can be shown.</remarks>
        /// <inheritdoc cref="DialoguePresenterBase.RunLineAsync"
        /// path="/param"/>
        /// <returns>A completed task.</returns>
        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (showsLastLine)
            {
                lastSeenLine = line;
            }
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Called by a <see cref="DialogueRunner"/> to display a collection of
        /// options to the user. 
        /// </summary>
        /// <inheritdoc cref="DialoguePresenterBase.RunOptionsAsync"
        /// path="/param"/>
        /// <inheritdoc cref="DialoguePresenterBase.RunOptionsAsync"
        /// path="/returns"/>
        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            // if all options are unavailable then we need to return null
            // it's the responsibility of the dialogue runner to handle this, not the presenter
            bool anyAvailable = false;
            foreach (var option in dialogueOptions)
            {
                if (option.IsAvailable)
                {
                    anyAvailable = true;
                    break;
                }
            }
            if (!anyAvailable)
            {
                return null;
            }

            // If we don't already have enough option views, create more
            while (dialogueOptions.Length > optionViews.Count)
            {
                var optionView = CreateNewOptionView();
                optionViews.Add(optionView);
            }

            // A completion source that represents the selected option.
            YarnTaskCompletionSource<DialogueOption?> selectedOptionCompletionSource = new YarnTaskCompletionSource<DialogueOption?>();

            // A cancellation token source that becomes cancelled when any
            // option item is selected, or when this entire option view is
            // cancelled
            var completionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.NextContentToken);

            async YarnTask CancelSourceWhenDialogueCancelled()
            {
                await YarnTask.WaitUntilCanceled(completionCancellationSource.Token);

                if (cancellationToken.IsNextContentRequested == true)
                {
                    // The overall cancellation token was fired, not just our
                    // internal 'something was selected' cancellation token.
                    // This means that the dialogue view has been informed that
                    // any value it returns will not be used. Set a 'null'
                    // result on our completion source so that that we can get
                    // out of here as quickly as possible.
                    selectedOptionCompletionSource.TrySetResult(null);
                }
            }

            // Start waiting 
            CancelSourceWhenDialogueCancelled().Forget();

            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var optionView = optionViews[i];
                var option = dialogueOptions[i];

                if (option.IsAvailable == false && showUnavailableOptions == false)
                {
                    // option is unavailable, skip it
                    continue;
                }

                optionView.gameObject.SetActive(true);
                optionView.Option = option;

                optionView.OnOptionSelected = selectedOptionCompletionSource;
                optionView.completionToken = completionCancellationSource.Token;
            }

            // There is a bug that can happen where in-between option items being configured one can be selected
            // and because the items are still being configured the others don't get the deselect message
            // which means visually two items are selected.
            // So instead now after configuring them we find if any are highlighted, and if so select that one
            // otherwise select the first non-deactivated one
            // because at this point now all of them are configured they will all get the select/deselect message
            int optionIndexToSelect = -1;
            for (int i = 0; i < optionViews.Count; i++)
            {
                var view = optionViews[i];
                if (!view.isActiveAndEnabled)
                {
                    continue;
                }

                if (view.IsHighlighted)
                {
                    optionIndexToSelect = i;
                    break;
                }

                // ok at this point the view is enabled
                // but not highlighted
                // so if we haven't already decreed we have found one to select
                // we select this one
                if (optionIndexToSelect == -1)
                {
                    optionIndexToSelect = i;
                }
            }
            if (optionIndexToSelect > -1)
            {
                optionViews[optionIndexToSelect].Select();
            }

            // Update the last line, if one is configured
            if (lastLineContainer != null)
            {
                if (lastSeenLine != null && showsLastLine)
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
                            if (lastLineCharacterNameText != null)
                            {
                                lastLineCharacterNameText.text = lastSeenLine.CharacterName;
                            }
                        }
                    }
                    else
                    {
                        line = lastSeenLine.TextWithoutCharacterName;
                    }

                    var lineText = line.Text;
                    // if the line was tagged with the TruncateLastLineMarkupName marker we want to clean that up before display
                    if (line.TryGetAttributeWithName(TruncateLastLineMarkupName, out var markup))
                    {
                        // we get the substring of 0 -> markup position
                        // and replace that range with ...
                        var end = lineText.Substring(markup.Position);
                        lineText = "..." + end;
                    }

                    if (lastLineText != null)
                    {
                        lastLineText.text = lineText;
                    }

                    lastLineContainer.SetActive(true);
                }
                else
                {
                    lastLineContainer.SetActive(false);
                }
            }

            if (useFadeEffect && canvasGroup != null)
            {
                // fade up the UI now
                await Effects.FadeAlphaAsync(canvasGroup, 0, 1, fadeUpDuration, cancellationToken.HurryUpToken);
            }

            // allow interactivity and wait for an option to be selected
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Wait for a selection to be made, or for the task to be completed.
            var completedTask = await selectedOptionCompletionSource.Task;
            completionCancellationSource.Cancel();

            // now one of the option items has been selected so we do cleanup
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (useFadeEffect && canvasGroup != null)
            {
                // fade down
                await Effects.FadeAlphaAsync(canvasGroup, 1, 0, fadeDownDuration, cancellationToken.HurryUpToken);
            }

            // disabling ALL the options views now
            foreach (var optionView in optionViews)
            {
                optionView.gameObject.SetActive(false);
            }
            await YarnTask.Yield();

            // if we are cancelled we still need to return but we don't want to have a selection, so we return no selected option
            if (cancellationToken.NextContentToken.IsCancellationRequested)
            {
                return await DialogueRunner.NoOptionSelected;
            }

            // finally we return the selected option
            return completedTask;
        }

        private OptionItem CreateNewOptionView()
        {
            var optionView = Instantiate(optionViewPrefab);

            var targetTransform = canvasGroup != null ? canvasGroup.transform : this.transform;

            if (optionView == null)
            {
                throw new System.InvalidOperationException($"Can't create new option view: {nameof(optionView)} is null");
            }

            optionView.transform.SetParent(targetTransform.transform, false);
            optionView.transform.SetAsLastSibling();
            optionView.gameObject.SetActive(false);

            return optionView;
        }
    }
}
