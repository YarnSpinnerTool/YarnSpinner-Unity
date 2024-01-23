using UnityEngine;
using System.Threading;
using UnityEngine.Events;


#if USE_UNITASK
using Cysharp.Threading.Tasks;
using YarnTask = Cysharp.Threading.Tasks.UniTask;
using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.DialogueOption>;
#else
using YarnTask = System.Threading.Tasks.Task;
using YarnOptionTask = System.Threading.Tasks.Task<Yarn.Unity.DialogueOption>;
#endif

#nullable enable

namespace Yarn.Unity
{
    public enum ViewBehaviour {
        LineStopsWhenThisViewCompletes,
        LineKeepsRunningWhenThisViewCompletes,
    }

    public abstract class AsyncDialogueViewBase : MonoBehaviour
    {
        public abstract YarnTask RunLineAsync(LocalizedLine line, CancellationToken token);
        public abstract YarnOptionTask RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken);

        public virtual bool EndLineWhenViewFinishes => false;
    }
}

