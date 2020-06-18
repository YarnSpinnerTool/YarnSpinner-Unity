using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;

namespace Yarn.Unity
{
    public class DialogueCharacterNameView : Yarn.Unity.DialogueViewBase
    {
        public DialogueRunner.StringUnityEvent onNameUpdate;
        public UnityEvent onDialogueStarted;
        public UnityEvent onNamePresent;
        public UnityEvent onNameNotPresent;

        public override void DialogueStarted() {
            onDialogueStarted?.Invoke();
        }

        public override void DismissLine(Action onDismissalComplete)
        {
            // Dismissing a line takes no time, so call the completion handler
            // immediately
            onDismissalComplete();
        }

        public override void OnLineStatusChanged(LocalizedLine dialogueLine)
        {
            // We don't need to do anything when the line status changes for
            // this view
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            // Try and get the character name from the line
            var hasCharacterName = dialogueLine.Text.TryGetAttributeWithName("character", out var characterAttribute);

            // Did we find one?
            if (hasCharacterName)
            {
                // Then notify the rest of the scene about it. This generally
                // involves updating a text view and making it visible.
                onNameUpdate?.Invoke(characterAttribute.Properties["name"].StringValue);
                onNamePresent?.Invoke();
            }
            else
            {
                // Otherwise, notify the scene about not finding it. This
                // generally involves making the name text view not visible.
                onNameNotPresent?.Invoke();
            }

            // Immediately mark this view as having finished its work
            onDialogueLineFinished();
        }

        public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            // This view doesn't present options, so do nothing here
        }
    }
}
