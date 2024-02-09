/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

// This file contains helpers that make it easier for the Yarn Spinner code to
// not have to think about which async API it's using.

#nullable enable

namespace Yarn.Unity
{
    using System;
    using System.Collections;
    using System.Threading;
    using System.Runtime.CompilerServices;
    
    using UnityEngine;
    using UnityEngine.ResourceManagement.AsyncOperations;

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
        WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken)
        {
#if USE_UNITASK
        // TODO: use cancellationToken
        return await operationHandle;
#else

            while (operationHandle.IsDone == false)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return default;
                }

                await YarnTask.Yield();
            }

            if (operationHandle.Status == AsyncOperationStatus.Failed) {
                throw operationHandle.OperationException;
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

        public static IEnumerator ToCoroutine(Func<YarnTask> factory)
        {
#if USE_UNITASK
            return UniTask.ToCoroutine(task);
#else
            var task = factory();
            // Yield until the task is complete, successfully or otherwise
            while (task.IsCompleted == false)
            {
                yield return null;
            }

            if (task.Exception != null)
            {
                // The task ended because it threw an exception. Rethrow it.
                throw task.Exception;
            }
#endif
        }

        internal static async YarnTask WaitForSeconds(float timeInSeconds)
        {
#if USE_UNITASK
            throw new NotImplementedException();
#else
            var now = Time.time;
            while (Time.time < now + timeInSeconds) {
                await YarnTask.Yield();
            }
#endif
        }
    }

}
