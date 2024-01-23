// This file contains helpers that make it easier for the Yarn Spinner code to
// not have to think about which async API it's using.

#nullable enable

namespace Yarn.Unity
{
    using UnityEngine;
    using System.Collections;
    using System.Threading;
    
    using UnityEngine.ResourceManagement.AsyncOperations;
    using System.Runtime.CompilerServices;

#if USE_UNITASK
    using Cysharp.Threading.Tasks;
    using YarnTask = Cysharp.Threading.Tasks.UniTask;
    using YarnObjectTask = Cysharp.Threading.Tasks.UniTask<UnityEngine.Object?>;
#else
    using YarnTask = System.Threading.Tasks.Task;
    using YarnObjectTask = System.Threading.Tasks.Task<UnityEngine.Object?>;
    using System.Threading.Tasks;
#endif

    public static partial class YarnAsync
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitUntilCanceled(System.Threading.CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                await YarnTask.Yield();
            }
        }

#if !USE_UNITASK
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YarnTask Forget(this YarnTask task)
        {
            return task;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitForCoroutine(this MonoBehaviour mb, Coroutine coro, CancellationToken cancellationToken = default)
        {
#if USE_UNITASK
        IEnumerator Wait()
        {
            yield return coro;
        }
        await Wait()
            .ToUniTask(cancellationToken: cancellationToken)
            .SuppressCancellationThrow();
#else
            bool complete = false;
            IEnumerator WaitForCompletion()
            {
                yield return coro;
                complete = true;
            }

            var waitingCoroutine = mb.StartCoroutine(WaitForCompletion());

            while (!complete)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // Our task was cancelled. Stop the coroutine and return
                    // immediately.
                    mb.StopCoroutine(waitingCoroutine);
                    return;
                }
                await YarnTask.Yield();
            }
#endif
        }

        // Type aliases don't currently support generics, so in order to support returning
        // a task-like object that yields type T, we need to use gross ifdefs
        // when specifying the return type.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async 
        #if USE_UNITASK
            UniTask<T?>
        #else
            Task<T?>
        #endif
        WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
#if USE_UNITASK
        // TODO: use cancellationToken
        return await operationHandle;
#else

            while (operationHandle.IsDone == false)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                await YarnTask.Yield();
            }

            return operationHandle.Result;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async YarnTask WaitForAsyncOperation(AsyncOperationHandle operationHandle, CancellationToken cancellationToken)
        {
#if USE_UNITASK
        // TODO: use cancellationToken
        return await operationHandle;
#else
            while (operationHandle.IsDone == false)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await YarnTask.Yield();
            }

            return;
#endif
        }
    }

}
