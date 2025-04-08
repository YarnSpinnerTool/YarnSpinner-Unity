/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Threading;
using UnityEngine;
using System;

#nullable enable

namespace Yarn.Unity.Samples
{
    /// <summary>
    /// A dialogue presenter that shows lines as chat bubbles in a scrolling
    /// container.
    /// </summary>
    public class ChatDialogueView : DialoguePresenterBase
    {
        /// <summary>
        /// A mapping between character names and the bubble prefabs used for
        /// their messages.
        /// </summary>
        [Header("Prefabs")]
        [SerializeField] SerializableDictionary<string, ChatDialogueViewBubble?> characters = new();

        /// <summary>
        /// The bubble prefab to use when no specific prefab is specified.
        /// </summary>
        [Space, SerializeField] ChatDialogueViewBubble? defaultBubblePrefab = null;

        /// <summary>
        /// The prefab to use for buttons in the options list.
        /// </summary>
        [SerializeField] ChatDialogueViewOptionsButton? optionsButtonPrefab;

        /// <summary>
        /// The container that message bubbles will be stored in.
        /// </summary>
        [Header("Containers")]
        [SerializeField] RectTransform? bubbleContainer;

        /// <summary>
        /// The container that options buttons will be stored in.
        /// </summary>
        [SerializeField] RectTransform? optionsContainer;

        /// <summary>
        /// The amount of time to pause after showing a line, before showing the
        /// next piece of content.
        /// </summary>
        [Header("Timing")]
        [SerializeField] float delayAfterLine = 1f;

        /// <summary>
        /// The minimum amount of time that a typing indicator will be on screen
        /// for.
        /// </summary>
        [SerializeField] float minimumTypingDelay = 0.5f;
        /// <summary>
        /// The maximum amount fo time that a typing indicator will be on screen
        /// for.
        /// </summary>
        [SerializeField] float maximumTypingDelay = 3f;

        /// <summary>
        /// The amount of time per message character that the typing indicator
        /// will be on screen for.
        /// </summary>
        [SerializeField] float typingDelayPerCharacter = 0.05f;

        /// <summary>
        /// If true, messages will appear with a 'user is typing' animation
        /// before their text is shown.
        /// </summary>
        [SerializeField] bool showTypingIndicators = true;

        /// <summary>
        /// Called by the dialogue runner when dialogue starts.
        /// </summary>
        /// <returns>A task that completes when the dialogue presenter has
        /// finished preparing for dialogue.</returns>
        public override YarnTask OnDialogueStartedAsync()
        {
            // This method doesn't have any work to do, so it immediately
            // returns a completed task.
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Called by the dialogue runner when dialogue ends.
        /// </summary>
        /// <returns>A task that completes when the dialogue presenter has
        /// finished wrapping up dialogue.</returns>
        public override YarnTask OnDialogueCompleteAsync()
        {
            // This method doesn't have any work to do, so it immediately
            // returns a completed task.
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Called by the dialogue runner when a line needs to be presented.
        /// </summary>
        /// <param name="line">The line that should be presented.</param>
        /// <param name="token">A cancellation token that represents whether the
        /// presentation should end early.</param>
        /// <returns>A task that completes when the line has finished presenting
        /// and the user should see the next piece of content.</returns>
        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            // Early out if we don't have anywhere to put our bubble
            if (bubbleContainer == null)
            {
                Debug.LogWarning($"Can't show line '{line.Text.Text}': no bubble container");
                return;
            }

            // Next, we figure out what prefab to use.

            // We'll start with our default bubble. If we know about a specific
            // bubble that the character speaking the line should use, we'll use
            // that instead.
            var prefab = defaultBubblePrefab;

            if (line.CharacterName != null && characters.TryGetValue(line.CharacterName, out var characterBubble))
            {
                prefab = characterBubble;
            }

            // If we don't have a bubble prefab at this point, we didn't have a
            // default prefab, and we didn't find a prefab for the specific
            // character. We can't show the line.
            if (prefab == null)
            {
                Debug.LogWarning($"Can't show line '{line.Text.Text}': no default bubble was set");
                return;
            }

            // Next, we need to show the bubble. If the options container is
            // present, insert it immediately before the container (so that the
            // options are always at the bottom of the list.) If we don't have
            // an options container, just insert it at the bottom of the list.

            int index;

            if (optionsContainer != null)
            {
                index = optionsContainer.GetSiblingIndex();
            }
            else
            {
                index = bubbleContainer.childCount - 1;
            }

            // If we're configured to show a typing indicator in the bubbles,
            // and the bubble prefab we have actually HAS a typing indicator,
            // we'll create a bubble for showing it, and wait for the
            // appropriate time before replacing it with the text.

            if (showTypingIndicators && prefab.HasIndicator)
            {
                // We create a bubble and then destroy and replace it (rather
                // than changing its size) to avoid a layout pop
                var typingBubble = Instantiate(prefab, bubbleContainer);
                typingBubble.transform.SetSiblingIndex(index);
                typingBubble.ShowTyping();

                // Calculate how long the typing indicator should appear for
                var typingDelay = Mathf.Clamp(
                    line.TextWithoutCharacterName.Text.Length * typingDelayPerCharacter,
                    minimumTypingDelay,
                    maximumTypingDelay);

                // Wait for the required time. If our token gets cancelled in
                // the meantime, stop waiting.
                await YarnTask.Delay(TimeSpan.FromSeconds(typingDelay), token.HurryUpToken).SuppressCancellationThrow();

                // Remove the typing bubble. We'll replace it with the text
                // bubble in a moment.
                Destroy(typingBubble.gameObject);
            }

            // Create the bubble containing the text.
            var bubble = Instantiate(prefab, bubbleContainer);
            bubble.transform.SetSiblingIndex(index);
            bubble.ShowText(line.TextWithoutCharacterName.Text);

            // Now that the line is on screen, wait for the appropriate delay,
            // and then return. We'll leave the speech bubble we added, so that
            // it stay on screen.
            await YarnTask.Delay(TimeSpan.FromSeconds(delayAfterLine), token.HurryUpToken).SuppressCancellationThrow();
        }

        /// <summary>
        /// Called by the dialogue runner when options need to be presented.
        /// </summary>
        /// <param name="dialogueOptions">The collection of options to
        /// present.</param>
        /// <param name="cancellationToken">A token that becomes cancelled if
        /// the dialogue runner no longer needs this dialogue presenter to
        /// return an option.</param>
        /// <returns>A task that completes with the option that the user
        /// selected, or <see langword="null"/> if no option was
        /// selected.</returns>
        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            // First things first: check to see if we have everything we need to show options.
            if (optionsContainer == null)
            {
                Debug.LogWarning($"Can't show options: no bubble container");
                return null;
            }

            if (optionsButtonPrefab == null)
            {
                Debug.LogWarning($"Can't show options: no bubble prefab");
                return null;
            }

            // Clear any previous options that might still be present.
            for (int i = 0; i < optionsContainer.childCount; i++)
            {
                Destroy(optionsContainer.GetChild(i).gameObject);
            }

            // Create a completion source, which allows the buttons to indicate
            // that an option has been selected.
            var completionSource = new YarnTaskCompletionSource<DialogueOption>();

            // Show a button for each of the options.
            foreach (var option in dialogueOptions)
            {
                // Create the button, and show the text.
                var button = Instantiate(optionsButtonPrefab, optionsContainer);
                button.Text = option.Line.TextWithoutCharacterName.Text;

                // When the button is clicked, complete the task with the
                // appropriate option.
                button.OnClick = () => completionSource.TrySetResult(option);
            }

            // Wait until an option has been selected.
            var selectedOption = await completionSource.Task;

            // Clean up by destroying all of the buttons.
            for (int i = 0; i < optionsContainer.childCount; i++)
            {
                Destroy(optionsContainer.GetChild(i).gameObject);
            }

            // Return the selected option.
            return selectedOption;
        }
    }
}