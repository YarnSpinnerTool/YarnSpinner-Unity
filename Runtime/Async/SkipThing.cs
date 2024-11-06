using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Yarn.Unity;

using YarnTask = System.Threading.Tasks.Task;

// this will become the dialogue request interrupt thing
public class SkipThing : AsyncDialogueViewBase
{
    public bool multiSoftSkipIsHardSkip = false;
    public int numberOfMultiSoftSkipsToBecomeHard = 2;

    private int numberOfSkipsThisLine = 0;

    private DialogueRunner runner;
    
    public override YarnTask OnDialogueCompleteAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        numberOfSkipsThisLine = 0;
        return YarnTask.CompletedTask;
    }

    public override Task<DialogueOption> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
    {
        return YarnAsync.NoOptionSelected;
    }

    public void Update()
    {
        if (runner == null)
        {
            runner = FindObjectOfType<DialogueRunner>();
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            runner.HurryUpCurrentLine();
            numberOfSkipsThisLine += 1;
        }
        
        if (multiSoftSkipIsHardSkip)
        {
            if (numberOfSkipsThisLine >= numberOfMultiSoftSkipsToBecomeHard)
            {
                runner.CancelCurrentLine();
            }
        }
        else
        {
            if (Input.GetKeyUp(KeyCode.Q))
            {
                runner.CancelCurrentLine();
            }
        }

        if (Input.GetKeyUp(KeyCode.T))
        {
            runner.Stop();
        }
        if (Input.GetKeyUp(KeyCode.J))
        {
            runner.StartDialogue("Start");
        }
    }
}
