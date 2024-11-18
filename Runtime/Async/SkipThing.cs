using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Yarn.Unity;

#if USE_UNITASK
using Cysharp.Threading.Tasks;
using YarnTask = Cysharp.Threading.Tasks.UniTask;
using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.DialogueOption>;
#elif UNITY_2023_1_OR_NEWER
using YarnTask = UnityEngine.Awaitable;
using YarnOptionTask = UnityEngine.Awaitable<Yarn.Unity.DialogueOption>;
#else
using YarnTask = System.Threading.Tasks.Task;
using YarnOptionTask = System.Threading.Tasks.Task<Yarn.Unity.DialogueOption>;
#endif

// this will become the dialogue request interrupt thing
public class SkipThing : AsyncDialogueViewBase
{
    public bool multiSoftSkipIsHardSkip = false;
    public int numberOfMultiSoftSkipsToBecomeHard = 2;

    private int numberOfSkipsThisLine = 0;

    private DialogueRunner runner;
    
    public override YarnTask OnDialogueCompleteAsync()
    {
        return YarnAsync.CompletedTask;
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnAsync.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        numberOfSkipsThisLine = 0;
        return YarnAsync.CompletedTask;
    }

    public override YarnOptionTask RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
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
