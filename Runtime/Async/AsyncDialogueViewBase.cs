using UnityEngine;
using System.Threading;
using UnityEngine.Events;


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

#nullable enable

namespace Yarn.Unity
{

    public abstract class AsyncDialogueViewBase : MonoBehaviour
    {
        public abstract YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token);
        public abstract YarnOptionTask RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken);
        public abstract YarnTask OnDialogueStartedAsync();
        public abstract YarnTask OnDialogueCompleteAsync();

        public virtual bool EndLineWhenViewFinishes => false;
    }
}

