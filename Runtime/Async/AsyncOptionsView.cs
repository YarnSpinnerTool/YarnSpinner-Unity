using System.Collections.Generic;
using UnityEngine;

#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = Yarn.Unity.TMPShim;
#endif

#if USE_UNITASK
using Cysharp.Threading.Tasks;
using YarnTask = Cysharp.Threading.Tasks.UniTask;
using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.DialogueOption>;
using YarnOptionCompletionSource = Cysharp.Threading.Tasks.UniTaskCompletionSource<Yarn.Unity.DialogueOption>;
#else
using System.Threading;
using YarnTask = System.Threading.Tasks.Task;
using YarnOptionTask = System.Threading.Tasks.Task<Yarn.Unity.DialogueOption>;
using YarnOptionCompletionSource = System.Threading.Tasks.TaskCompletionSource<Yarn.Unity.DialogueOption>;
#endif

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// Receives options from a <see cref="DialogueRunner"/>, and displays and
    /// manages a collection of <see cref="AsyncOptionItem"/> views for the user
    /// to choose from.
    /// </summary>
    [HelpURL("https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-view/options-list-view")]
    public class AsyncOptionsView : AsyncDialogueViewBase
    {
        [SerializeField] CanvasGroup? canvasGroup;

        [MustNotBeNull]
        [SerializeField] AsyncOptionItem optionViewPrefab;

        // A cached pool of OptionView objects so that we can reuse them
        List<AsyncOptionItem> optionViews = new List<AsyncOptionItem>();

        [Space]
        [SerializeField] bool showsLastLine;

        [ShowIf(nameof(showsLastLine))]
        [Indent]
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


        LocalizedLine lastSeenLine;

        /// <summary>
        /// Controls whether or not to display options whose <see
        /// cref="OptionSet.Option.IsAvailable"/> value is <see
        /// langword="false"/>.
        /// </summary>
        [Space]
        public bool showUnavailableOptions = false;

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
        /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync"
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
        /// <inheritdoc cref="AsyncDialogueViewBase.RunOptionsAsync"
        /// path="/param"/>
        /// <inheritdoc cref="AsyncDialogueViewBase.RunOptionsAsync"
        /// path="/returns"/>
        public override async YarnOptionTask RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            // If we don't already have enough option views, create more
            while (dialogueOptions.Length > optionViews.Count)
            {
                var optionView = CreateNewOptionView();
                optionViews.Add(optionView);
            }

            // the tasks we are making to give to the options for their selection event
            List<YarnOptionTask> tasks = new List<YarnOptionTask>();

            // adding in a cancellation task
            // this exists to let us bail out early if the dialogue is cancelled
            // in later version of dotnet there exists a way to bind a cancellation token to a WhenAny task, but not in our version
            // alas
            var cancellationTask = new YarnOptionTask(() => { return null; }, cancellationToken);
            tasks.Add(cancellationTask);

            // tracks the options views created so we can use it to configure the interaction correctly
            int optionViewsCreated = 0;
            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var optionView = optionViews[i];
                var option = dialogueOptions[i];

                if (option.IsAvailable == false && showUnavailableOptions == false)
                {
                    Debug.Log("option is unavailable, skipping it");
                    continue;
                }
                optionView.gameObject.SetActive(true);
                optionView.Option = option;

                YarnOptionCompletionSource tcs = new YarnOptionCompletionSource();
                tasks.Add(tcs.Task);
                optionView.onOptionSelected = tcs;

                // The first available option is selected by default
                if (optionViewsCreated == 0)
                {
                    optionView.Select();
                }
                optionViewsCreated += 1;
            }

            // Update the last line, if one is configured
            if (lastLineContainer != null && lastLineText != null)
            {
                if (lastSeenLine != null && showsLastLine)
                {
                    // if we have a last line character name container
                    // and the last line has a character then we show the nameplate
                    // otherwise we turn off the nameplate
                    var line = lastSeenLine.Text;
                    if (lastLineCharacterNameContainer != null && lastLineCharacterNameText != null)
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
                    else
                    {
                        line = lastSeenLine.TextWithoutCharacterName;
                    }

                    lastLineText.text = line.Text;
                    lastLineContainer.SetActive(true);
                }
                else
                {
                    lastLineContainer.SetActive(false);
                }
            }

            // fade up the UI now
            await Effects.FadeAlpha(canvasGroup, 0, 1, 1, cancellationToken);

            // allow interactivity and wait for an option to be selected
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            var completedTask = await YarnTask.WhenAny(tasks);

            // now one of the option items has been selected so we do cleanup
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // fade down
            await Effects.FadeAlpha(canvasGroup, 1, 0, 1, cancellationToken);

            // disabling ALL the options views now
            foreach (var optionView in optionViews)
            {
                optionView.gameObject.SetActive(false);
            }
            await YarnTask.Yield();

            // if we are cancelled we still need to return but we don't want to have a selection, so we return no selected option
            if (cancellationToken.IsCancellationRequested)
            {
                return YarnAsync.NoOptionSelected.Result;
            }

            // finally we return the selected option
            return completedTask.Result;
        }

        private AsyncOptionItem CreateNewOptionView()
        {
            var optionView = Instantiate(optionViewPrefab);

            var targetTransform = canvasGroup != null ? canvasGroup.transform : this.transform;

            optionView.transform.SetParent(targetTransform.transform, false);
            optionView.transform.SetAsLastSibling();
            optionView.gameObject.SetActive(false);

            return optionView;
        }
    }
}
