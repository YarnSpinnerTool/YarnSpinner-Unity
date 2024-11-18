/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

// This file contains helpers that make it easier for the Yarn Spinner code to
// not have to think about which async API it's using.

#nullable enable

namespace Yarn.Unity
{
#if USE_ADDRESSABLES
    using UnityEngine.ResourceManagement.AsyncOperations;
#endif

#if USE_UNITASK
    using Cysharp.Threading.Tasks;
    using YarnTask = Cysharp.Threading.Tasks.UniTask;
    using YarnObjectTask = Cysharp.Threading.Tasks.UniTask<UnityEngine.Object?>;
    using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<DialogueOption?>;

    public static partial class YarnAsync
    {
        public static YarnTask Delay(int milliseconds, CancellationToken token)
        {
            return YarnTask.Delay(milliseconds, token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitUntil(System.Func<bool> predicate, System.Threading.CancellationToken token)
        {
            while (!token.IsCancellationRequested && predicate() == false)
            {
                await YarnTask.Yield();
            }
        }

        public static YarnOptionTask NoOptionSelected
        {
            get
            { 
                return YarnTask.FromResult<DialogueOption?>(null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitForCoroutine(this MonoBehaviour mb, IEnumerator coro, CancellationToken cancellationToken = default)
        {
            // If we have UniTask, we can convert it to a task directly and
            // attach a cancellation token
            await coro.ToUniTask(cancellationToken: cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitForCoroutine(this MonoBehaviour mb, Coroutine coro)
        {
            IEnumerator Wait()
            {
                yield return coro;
            }
            await Wait().ToUniTask(mb);
        }

#if USE_ADDRESSABLES
        // Type aliases don't currently support generics, so in order to support returning
        // a task-like object that yields type T, we need to use gross ifdefs
        // when specifying the return type.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<T?> WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            return await operationHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async YarnTask WaitForAsyncOperation(AsyncOperationHandle operationHandle, CancellationToken cancellationToken)
        {
            await operationHandle.ToUniTask(cancellationToken: cancellationToken);
        }

#endif

        public static IEnumerator ToCoroutine(Func<YarnTask> factory)
        {
            return UniTask.ToCoroutine(factory);
        }

        internal static bool IsCompleted(this UniTask task)
        {
            return task.Status != UniTaskStatus.Pending;
        }
        internal static bool IsCompleted<T>(this UniTask<T> task)
        {
            return task.Status != UniTaskStatus.Pending;
        }
        internal static bool IsCompletedSuccessfully(this UniTask task)
        {
            return task.Status != UniTaskStatus.Succeeded;
        }
        internal static bool IsCompletedSuccessfully<T>(this UniTask<T> task)
        {
            return task.Status != UniTaskStatus.Succeeded;
        }

        internal static YarnTask AsYarnTask(this System.Threading.Tasks.Task task)
        {
            return task.AsUniTask();
        }
        // will also need a awaitables to unitask here

        internal static async YarnTask Wait(this UniTask task, TimeSpan timeout)
        {
            var delay = UniTask.Delay(timeout);

            var winner = await UniTask.WhenAny(task, delay);

            if (winner == 0)
            {
                return;
            }
            else
            {
                throw new TimeoutException();
            }

        }

        internal static YarnTask Never(CancellationToken cancellationToken)
        {
            return UniTask.Never(cancellationToken);
        }
    }
#endif
}
