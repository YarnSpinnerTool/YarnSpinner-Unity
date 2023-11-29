using UnityEngine;
using System.Threading;

#if USE_UNITASK
using Cysharp.Threading.Tasks;
using YarnTask = Cysharp.Threading.Tasks.UniTask;
using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.DialogueOption>;
using YarnLineTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.LocalizedLine>;
#else
using YarnTask = System.Threading.Tasks.Task;
using YarnOptionTask = System.Threading.Tasks.Task<Yarn.Unity.DialogueOption>;
using YarnLineTask = System.Threading.Tasks.Task<Yarn.Unity.LocalizedLine>;
#endif

namespace Yarn.Unity
{
    public abstract class AsyncDialogueViewBase : MonoBehaviour
    {
        public abstract YarnTask RunLineAsync(LocalizedLine line, CancellationToken token);
        public abstract YarnOptionTask RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken);
    }
}
