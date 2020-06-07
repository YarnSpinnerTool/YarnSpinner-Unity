using System;
using System.Collections;
using Yarn.Unity;

public class DialogueRunnerMockUI : Yarn.Unity.DialogueViewBase
{
    // The text of the most recently received line that we've been given
    public string CurrentLine { get; private set; } = default;

    public override void RunLine(LocalizedLine dialogueLine, Action onLineDeliveryComplete)
    {
        // Store the localised text in our CurrentLine property and
        // immediately signal that we're done "delivering" the line
        CurrentLine = dialogueLine.TextLocalized;
        onLineDeliveryComplete();
    }

    public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
    {
        // Do nothing in response to options becoming available
    }

    public override void DismissLine(Action onDismissalComplete) {
        // Immediately indicate that we're done 'dismissing' the line.
        onDismissalComplete();
    }

    public override void OnLineStatusChanged(LocalizedLine dialogueLine, LineStatus previousStatus, LineStatus newStatus)
    {
        // Do nothing in response to lines changing status
    }
}
