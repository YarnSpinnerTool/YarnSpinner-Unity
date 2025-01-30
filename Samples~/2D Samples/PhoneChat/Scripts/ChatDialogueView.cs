using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Yarn;
using Yarn.Unity;
using System;

#nullable enable

// public class ChatBubbleDictionary : SerializableDictionary<string, ChatDialogueViewBubble?> {}

public class ChatDialogueView : AsyncDialogueViewBase
{
    // [System.Serializable]
    // public struct BubbleCharacter
    // {
    //     public ChatDialogueViewBubble? bubblePrefab;
    //     public string? name;
    // }

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


    public override async YarnTask OnDialogueStartedAsync()
    {
        // Called by the Dialogue Runner to signal that dialogue has just
        // started up.
        //
        // You can use this method to prepare for presenting dialogue, like
        // changing the camera, fading up your on-screen UI, or other tasks.
        //
        // The Dialogue Runner will wait until every Dialogue View returns from
        // this method before delivering any content.
    }

    public override async YarnTask OnDialogueCompleteAsync()
    {
        // Called by the Dialogue Runner to signal that dialogue has ended.
        //
        // You can use this method to clean up after running dialogue, like
        // changing the camera back, fading away on-screen UI, or other tasks.
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


        // Called by the Dialogue Runner to signal that a line of dialogue
        // should be shown to the player.
        //
        // If your dialogue views handles lines, it should take the 'line'
        // parameter and use the information inside it to present the content to
        // the player, in whatever way makes sense.
        //
        // Some useful information:
        // - The 'Text' property in 'line' contains the parsed, localised text
        //   of the line, including attributes and text.
        // - The 'TextWithoutCharacterName' property contains all of the text
        //   after the character name in the line (if present), and the
        //   'CharacterName' contains the character name (if present).
        // - The 'Asset' property contains whatever object was associated with
        //   this line, as provided by your Dialogue Runner's Line Provider.
        //
        // The LineCancellationToken contains information on whether the
        // Dialogue Runner wants this Dialogue View to hurry up its
        // presentation, or to advance to the next line. 
        //
        // - If 'token.IsHurryUpRequested' is true, that's a hint that your view
        //   should speed up its delivery of the line, if possible (for example,
        //   by displaying text faster). 
        // - If 'token.IsNextLineRequested' is true, that's an instruction that
        //   your view must end its presentation of the line as fast as possible
        //   (even if that means ending the delivery early.)
        //
        // The Dialogue Runner will wait for all Dialogue Views to return from
        // this method before delivering new content.
        //
        // If your Dialogue View doesn't need to handle lines, simply return
        // from this method immediately.
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

        // Called by the Dialogue Runner to signal that options should be shown
        // to the player.
        //
        // If your Dialogue View handles options, it should present them to the
        // player and await a selection. Once a choice has been made, it should
        // return the appropriate element from dialogueOptions.
        //
        // The CancellationToken can be used to check to see if the Dialogue
        // Runner no longer needs this Dialogue View to make a choice. This
        // happens if a different Dialogue View made a selection, or if dialogue
        // has been cancelled. If the token is cancelled, it means that the
        // returned value from this method will not be used, and this method
        // should return null as soon as possible.
        //
        // The Dialogue Runner will wait for all Dialogue Views to return from
        // this method before delivering new content.
        //
        // If your Dialogue View doesn't need to handle options, simply return
        // null from this method to indicate that this Dialogue View didn't make
        // a selection.

        return null;
    }
}
