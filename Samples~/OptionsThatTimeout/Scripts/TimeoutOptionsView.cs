/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using Yarn.Unity.Attributes;

#nullable enable

namespace Yarn.Unity.Samples
{
    public class TimeoutOptionsView : DialoguePresenterBase
    {
        [SerializeField] CanvasGroup? canvasGroup;

        [MustNotBeNull]
        [SerializeField] OptionItem? optionViewPrefab;

        [MustNotBeNull]
        [SerializeField] TimeoutBar? timedBar;

        // A cached pool of OptionView objects so that we can reuse them
        List<OptionItem> optionViews = new List<OptionItem>();

        [Space]
        public float autoSelectDuration = 10f;

        [Space]
        [Group("Fade")]
        public float fadeUpDuration = 0.25f;

        [Group("Fade")]
        public float fadeDownDuration = 0.1f;

        // the metadata string compared to for setting an option as the visible default option
        private const string VisibleDefault = "default";
        // the metadata string compared to for setting an option as the invisible fallback option
        private const string HiddenFallback = "fallback";

        // flag used to determine if the last highlighted option should be selected when the timer runs out
        // this is set by the auto_opt command in yarn
        private bool LastHighlightedOptionIsSelectedAfterDuration = false;

        // represents what type of timeout option (if any) the option group has
        // the default is none, aka the normal options list view
        enum TimeOutOptionType
        {
            None, HiddenFallback, VisibleDefault, LastHighlighted,
        }

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
        protected void Start()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            var runner = FindAnyObjectByType<DialogueRunner>();
            runner.AddCommandHandler<float>("auto_opt", SetSelectedOptionsToAutoComplete);
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
        /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync"
        /// path="/param"/>
        /// <returns>A completed task.</returns>
        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            return YarnTask.CompletedTask;
        }

        // this handles the two option specific situations of either the invisible fallback or the visible default
        // if the timer reaches the end of its run without being cancelled it will return the supplied option
        // which the tun option method will then use as the selected option for the group
        internal async YarnTask BeginDefaultSelectTimeout(YarnTaskCompletionSource<DialogueOption?> selectedOptionCompletionSource, DialogueOption? option, CancellationToken cancellationToken)
        {
            if (timedBar == null)
            {
                return;
            }

            timedBar.duration = autoSelectDuration;
            await timedBar.Shrink(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                selectedOptionCompletionSource.TrySetResult(option);
            }
        }

        // this handles the situation for the last highlighted option is selected
        // if the time reaches the end of its run without being cancelled it will look through all the option item views
        // find the one that is highlighted
        // and use that
        // if none are highlighted then it returns the first one
        internal async YarnTask BeginLastSelectedOptionTimeout(YarnTaskCompletionSource<DialogueOption?> selectedOptionCompletionSource, List<OptionItem> options, CancellationToken cancellationToken)
        {
            if (timedBar == null)
            {
                return;
            }
            timedBar.duration = autoSelectDuration;
            await timedBar.Shrink(cancellationToken);

            bool foundIt = false;

            if (!cancellationToken.IsCancellationRequested)
            {
                foreach (var option in options)
                {
                    if (option.IsHighlighted)
                    {
                        foundIt = true;
                        selectedOptionCompletionSource.TrySetResult(option.Option);
                        break;
                    }
                }
            }

            if (!foundIt)
            {
                selectedOptionCompletionSource.TrySetResult(options[0].Option);
            }
        }

        // command to set the last highlighted option is selected after duration ends flag to be true
        public void SetSelectedOptionsToAutoComplete(float duration = 3f)
        {
            autoSelectDuration = duration;
            LastHighlightedOptionIsSelectedAfterDuration = true;
        }

        /// <summary>
        /// Called by a <see cref="DialogueRunner"/> to display a collection of
        /// options to the user. 
        /// </summary>
        /// <inheritdoc cref="AsyncDialogueViewBase.RunOptionsAsync"
        /// path="/param"/>
        /// <inheritdoc cref="AsyncDialogueViewBase.RunOptionsAsync"
        /// path="/returns"/>
        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            int hasDefault = 0;
            TimeOutOptionType defaultOptionType = TimeOutOptionType.None;
            DialogueOption? defaultOption = null;

            // we run through all the options quickly to see if there are any that are configured for timeouts
            foreach (var option in dialogueOptions)
            {
                int currentDefaults = hasDefault;
                foreach (var metadata in option.Line.Metadata)
                {
                    if (metadata == HiddenFallback)
                    {
                        // if the hidden fallback option has failed it's condition then we don't want it to impact the rest of the option group
                        if (!option.IsAvailable)
                        {
                            continue;
                        }

                        defaultOptionType = TimeOutOptionType.HiddenFallback;
                        defaultOption = option;
                        hasDefault += 1;

                        break;
                    }
                    else if (metadata == VisibleDefault)
                    {
                        // if the visible default option has failed it's condition then we don't want it to impact the rest of the option group
                        if (!option.IsAvailable)
                        {
                            continue;
                        }

                        // we are a visible default
                        // so we still want to be shown
                        // but will also want the 
                        defaultOptionType = TimeOutOptionType.VisibleDefault;
                        defaultOption = option;
                        hasDefault += 1;

                        break;
                    }
                }
            }

            // if the auto complete selected option flag has been set
            if (LastHighlightedOptionIsSelectedAfterDuration)
            {
                defaultOptionType = TimeOutOptionType.LastHighlighted;
            }

            // now we do some error checking
            // because these are related but incompatible means of doing auto selection we need to work out what we are going to do
            switch (defaultOptionType)
            {
                case TimeOutOptionType.VisibleDefault:
                {
                    // we are a default advancing case
                    // this happens to be the same as the hidden fallback case from a "are we in the right state" perspective
                    // so we will fallthrough to that case
                    goto case TimeOutOptionType.HiddenFallback;
                }
                case TimeOutOptionType.HiddenFallback:
                {
                    // we are the hidden fallback case
                    // this means we need to have only found one default tagged line 
                    // and we need to have the defaultOption value be not null

                    if (hasDefault != 1)
                    {
                        Debug.LogError("Encountered more than one option with timeout tags");

                        // we turn off the auto complete flag just in case it was on
                        // and return
                        LastHighlightedOptionIsSelectedAfterDuration = false;
                        return await DialogueRunner.NoOptionSelected;
                    }
                    if (defaultOption == null)
                    {
                        Debug.LogError("Encountered have an option tagged as a default but have no option value set.");

                        // we turn off the auto complete flag just in case it was on
                        // and return
                        LastHighlightedOptionIsSelectedAfterDuration = false;
                        return await DialogueRunner.NoOptionSelected;
                    }
                    
                    // otherwise we are fine
                    break;
                }
                case TimeOutOptionType.LastHighlighted:
                {
                    // we are the last highlighted option is autoselected case
                    // this means we need to not have a default option
                    // and have no default tagged options

                    if (hasDefault != 0)
                    {
                        Debug.LogError("Asked to select the last highlighted option but also have tagged options");

                        // we turn off the auto complete flag off
                        // and return
                        LastHighlightedOptionIsSelectedAfterDuration = false;
                        return await DialogueRunner.NoOptionSelected;
                    }
                    if (defaultOption != null)
                    {
                        Debug.LogError("Asked to select the last highlighted option but somehow also have a default option set");

                        // we turn off the auto complete flag off
                        // and return
                        LastHighlightedOptionIsSelectedAfterDuration = false;
                        return await DialogueRunner.NoOptionSelected;
                    }

                    break;
                }
            }

            // A completion source that represents the selected option.
            YarnTaskCompletionSource<DialogueOption?> selectedOptionCompletionSource = new YarnTaskCompletionSource<DialogueOption?>();

            // A cancellation token source that becomes cancelled when any
            // option item is selected, or when this entire option view is
            // cancelled
            var completionCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            async YarnTask CancelSourceWhenDialogueCancelled()
            {
                await YarnTask.WaitUntilCanceled(completionCancellationSource.Token);

                if (cancellationToken.IsCancellationRequested == true)
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

            // adding in any additional option item views if necessary
            if (optionViews.Count < dialogueOptions.Length)
            {
                var newViews = dialogueOptions.Length - optionViews.Count;
                for (int i = 0; i < newViews; i++)
                {
                    var option = CreateNewOptionView();
                    optionViews.Add(option);
                }
            }

            // configuring all the dialogue items
            int optionViewsCreated = 0;
            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var optionView = optionViews[i];
                var option = dialogueOptions[i];

                if (option.IsAvailable == false)
                {
                    // option is unavailable, skip it
                    continue;
                }

                // if we are set to have a hidden fallback option
                // and that option is THIS option we are configuring the view for
                // we want to skip over it
                if (defaultOptionType == TimeOutOptionType.HiddenFallback && defaultOption != null && option.DialogueOptionID == defaultOption.DialogueOptionID)
                {
                    continue;
                }

                optionView.gameObject.SetActive(true);
                optionView.Option = option;

                optionView.OnOptionSelected = selectedOptionCompletionSource;
                optionView.completionToken = completionCancellationSource.Token;

                // The first available option is selected by default
                if (optionViewsCreated == 0)
                {
                    optionView.Select();
                }
                optionViewsCreated += 1;
            }
            
            // now we add in the timer bar if necessary or turn it off if it isn't needed
            if (defaultOptionType == TimeOutOptionType.None)
            {
                if (timedBar != null)
                {
                    timedBar.gameObject.SetActive(false);
                }
            }
            else
            {
                // we always want it at the bottom regardless of how many option item views there are
                if (timedBar != null)
                {
                    timedBar.gameObject.SetActive(true);
                    timedBar.ResetBar();
                    timedBar.transform.parent.SetAsLastSibling();
                }
            }

            // fade up the UI now
            await Effects.FadeAlphaAsync(canvasGroup, 0, 1, fadeUpDuration, cancellationToken);

            // allow interactivity and wait for an option to be selected
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // now we kick off the timer bar if needed
            switch (defaultOptionType)
            {
                case TimeOutOptionType.VisibleDefault:
                {
                    BeginDefaultSelectTimeout(selectedOptionCompletionSource, defaultOption, completionCancellationSource.Token).Forget();
                    break;
                }
                case TimeOutOptionType.HiddenFallback:
                {
                    BeginDefaultSelectTimeout(selectedOptionCompletionSource, defaultOption, completionCancellationSource.Token).Forget();
                    break;
                }
                case TimeOutOptionType.LastHighlighted:
                {
                    BeginLastSelectedOptionTimeout(selectedOptionCompletionSource, optionViews, completionCancellationSource.Token).Forget();
                    break;
                }
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

            // disable the last highlighted selection flag, just in case it was set
            LastHighlightedOptionIsSelectedAfterDuration = false;

            // fade down
            await Effects.FadeAlphaAsync(canvasGroup, 1, 0, fadeDownDuration, cancellationToken);

            // disabling ALL the options views now
            foreach (var optionView in optionViews)
            {
                optionView.gameObject.SetActive(false);
            }
            await YarnTask.Yield();

            // if we are cancelled we still need to return but we don't want to have a selection, so we return no selected option
            if (cancellationToken.IsCancellationRequested)
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
