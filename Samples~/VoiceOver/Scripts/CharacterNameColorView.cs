using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Yarn;
using Yarn.Unity;

#nullable enable

public class CharacterNameColorView : DialoguePresenterBase
{
    [SerializeField] SerializableDictionary<string, Color> characterColors = new();
    [SerializeField] Color defaultColor = Color.white;

    [SerializeField] List<TMPro.TMP_Text> texts = new();

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {

        Color color;

        if (string.IsNullOrEmpty(line.CharacterName) || !characterColors.TryGetValue(line.CharacterName, out color))
        {
            color = defaultColor;
        }

        foreach (var text in texts)
        {
            text.color = color;
        }

        return YarnTask.CompletedTask;
    }

    public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
    {
        return YarnTask.FromResult<DialogueOption?>(null);
    }
}
