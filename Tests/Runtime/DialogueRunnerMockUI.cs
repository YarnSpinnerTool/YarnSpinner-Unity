using System;
using System.Collections;
using System.Collections.Generic;
using Yarn.Unity;

public class DialogueRunnerMockUI : Yarn.Unity.DialogueViewBase
{
    // The text of the most recently received line that we've been given
    public string CurrentLine { get; private set; } = default;

    // The text of the most recently received options that we've ben given
    public List<string> CurrentOptions {get;private set;} = new List<string>();

    public override void RunLine(LocalizedLine dialogueLine, Action onLineDeliveryComplete)
    {
        // Store the localised text in our CurrentLine property and
        // immediately signal that we're done "delivering" the line
        CurrentLine = dialogueLine.TextLocalized;
        onLineDeliveryComplete();
    }

    public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
    {
        CurrentOptions.Clear();
        foreach (var option in dialogueOptions) {
            CurrentOptions.Add(option.TextLocalized);
        }
    }

    public override void DismissLine(Action onDismissalComplete) {
        // Immediately indicate that we're done 'dismissing' the line.
        onDismissalComplete();
    }

    public override void OnLineStatusChanged(LocalizedLine dialogueLine)
    {
        // Do nothing in response to lines changing status
    }
}
