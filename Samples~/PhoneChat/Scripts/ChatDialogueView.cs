/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Yarn;
using Yarn.Unity;
using System;

#nullable enable

namespace Yarn.Unity.Samples
{
    public class ChatDialogueView : DialoguePresenterBase
    {
        [Header("Prefabs")]
        [SerializeField] SerializableDictionary<string, ChatDialogueViewBubble?> characters = new();

        [Space, SerializeField] ChatDialogueViewBubble? defaultBubblePrefab = null;
        [SerializeField] ChatDialogueViewOptionsButton? optionsButtonPrefab;

        [Header("Containers")]
        [SerializeField] RectTransform? bubbleContainer;

        [SerializeField] RectTransform? optionsContainer;

        [Header("Timing")]
        [SerializeField] float minimumTypingDelay = 0.5f;
        [SerializeField] float maximumTypingDelay = 3f;
        [SerializeField] float typingDelayPerCharacter = 0.05f;
        [SerializeField] float delayAfterLine = 1f;
        [SerializeField] bool showTypingIndicators = true;


        public override YarnTask OnDialogueStartedAsync()
        {
            // Called by the Dialogue Runner to signal that dialogue has just
            // started up.
            //
            // You can use this method to prepare for presenting dialogue, like
            // changing the camera, fading up your on-screen UI, or other tasks.
            //
            // The Dialogue Runner will wait until every Dialogue View returns from
            // this method before delivering any content.
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            // Called by the Dialogue Runner to signal that dialogue has ended.
            //
            // You can use this method to clean up after running dialogue, like
            // changing the camera back, fading away on-screen UI, or other tasks.
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (bubbleContainer == null)
            {
                Debug.LogWarning($"Can't show line '{line.Text.Text}': no bubble container");
                return;
            }

            var prefab = defaultBubblePrefab;

            if (line.CharacterName != null)
            {
                characters.TryGetValue(line.CharacterName, out prefab);
            }

            if (prefab == null)
            {
                Debug.LogWarning($"Can't show line '{line.Text.Text}': no default bubble was set");
                return;
            }


            // Insert the new bubble immediately before the options container (if
            // present), or else at the end of the list.
            int index;

            if (optionsContainer != null)
            {
                index = optionsContainer.GetSiblingIndex();
            }
            else
            {
                index = bubbleContainer.childCount - 1;
            }


            if (showTypingIndicators && prefab.HasIndicator)
            {
                // We create a bubble and then destroy and replace it (rather than
                // changing its size) to avoid a layout pop
                var typingBubble = Instantiate(prefab, bubbleContainer);
                typingBubble.transform.SetSiblingIndex(index);
                typingBubble.SetTyping(true);

                var typingDelay = Mathf.Clamp(line.TextWithoutCharacterName.Text.Length * typingDelayPerCharacter, minimumTypingDelay, maximumTypingDelay);
                await YarnTask.Delay(TimeSpan.FromSeconds(typingDelay), token.HurryUpToken).SuppressCancellationThrow();

                Destroy(typingBubble.gameObject);
            }

            var bubble = Instantiate(prefab, bubbleContainer);
            bubble.transform.SetSiblingIndex(index);
            bubble.SetText(line.TextWithoutCharacterName.Text);

            await YarnTask.Delay(TimeSpan.FromSeconds(delayAfterLine), token.HurryUpToken).SuppressCancellationThrow();
        }

        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
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

            for (int i = 0; i < optionsContainer.childCount; i++)
            {
                Destroy(optionsContainer.GetChild(i).gameObject);
            }


            var completionSource = new YarnTaskCompletionSource<DialogueOption>();

            foreach (var option in dialogueOptions)
            {
                var button = Instantiate(optionsButtonPrefab, optionsContainer);
                button.Text = option.Line.TextWithoutCharacterName.Text;
                button.OnClick = () => completionSource.TrySetResult(option);
            }

            var selectedOption = await completionSource.Task;

            for (int i = 0; i < optionsContainer.childCount; i++)
            {
                Destroy(optionsContainer.GetChild(i).gameObject);
            }

            return selectedOption;
        }
    }
}