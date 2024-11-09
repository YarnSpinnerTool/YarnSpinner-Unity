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
#if USE_ADDRESSABLES
    using UnityEngine.ResourceManagement.AsyncOperations;
#endif

#if USE_UNITASK
    using Cysharp.Threading.Tasks;
    using YarnTask = Cysharp.Threading.Tasks.UniTask;
    using YarnObjectTask = Cysharp.Threading.Tasks.UniTask<UnityEngine.Object?>;
    using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<DialogueOption?>;
#else
    using YarnTask = System.Threading.Tasks.Task;
    using YarnObjectTask = System.Threading.Tasks.Task<UnityEngine.Object?>;
    using YarnOptionTask = System.Threading.Tasks.Task<DialogueOption?>;
    using System.Threading.Tasks;
#endif

    public static partial class YarnAsync
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitUntilCanceled(System.Threading.CancellationToken token)
        {
#if USE_UNITASK
            await YarnTask.WaitUntilCanceled(token);
#else
            while (token.IsCancellationRequested == false)
            {
                await YarnTask.Yield();
            }
#endif
        }

        public async static YarnTask Delay(int milliseconds, CancellationToken token)
        {
            try
            {
                await YarnTask.Delay(milliseconds, token);
            }
            catch (TaskCanceledException)
            {
                // we don't want to throw an exception for this because this is valid behaviour
                // why did you do it this way c# ?
                // WHY?
            }
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

#if !USE_UNITASK
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YarnTask Forget(this YarnTask task)
        {
            return task;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitForCoroutine(this MonoBehaviour mb, IEnumerator coro, CancellationToken cancellationToken = default)
        {
#if USE_UNITASK
            // If we have UniTask, we can convert it to a task directly and
            // attach a cancellation token
            await coro.ToUniTask(cancellationToken: cancellationToken);
#else
            // Otherwise, we'll need to just start the coroutine directly and
            // wait for it to run to completion
            await mb.WaitForCoroutine(mb.StartCoroutine(coro));
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitForCoroutine(this MonoBehaviour mb, Coroutine coro)
        {
#if USE_UNITASK
            IEnumerator Wait()
            {
                yield return coro;
            }
            await Wait().ToUniTask(mb);
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
                await YarnTask.Yield();
            }
#endif
        }

#if USE_ADDRESSABLES
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
        await operationHandle.ToUniTask(cancellationToken: cancellationToken);
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

#endif

        public static IEnumerator ToCoroutine(Func<YarnTask> factory)
        {
#if USE_UNITASK
            return UniTask.ToCoroutine(factory);
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

#if USE_UNITASK
        internal static bool IsCompleted(this UniTask task) {
            return task.Status != UniTaskStatus.Pending;
        }
        internal static bool IsCompleted<T>(this UniTask<T> task) {
            return task.Status != UniTaskStatus.Pending;
        }
        internal static bool IsCompletedSuccessfully(this UniTask task) {
            return task.Status != UniTaskStatus.Succeeded;
        }
        internal static bool IsCompletedSuccessfully<T>(this UniTask<T> task) {
            return task.Status != UniTaskStatus.Succeeded;
        }
#endif

        internal static bool IsCompleted(this System.Threading.Tasks.Task task)
        {
            return task.IsCompleted;
        }
        internal static bool IsCompleted<T>(this System.Threading.Tasks.Task task)
        {
            return task.IsCompleted;
        }

        internal static bool IsCompletedSuccessfully(this System.Threading.Tasks.Task task)
        {
            return task.IsCompletedSuccessfully;
        }
        internal static bool IsCompletedSuccessfully<T>(this System.Threading.Tasks.Task task)
        {
            return task.IsCompletedSuccessfully;
        }

#if USE_UNITASK
        internal static YarnTask AsYarnTask(this System.Threading.Tasks.Task task)
        {
            return task.AsUniTask();
        }
#else
        internal static YarnTask AsYarnTask(this System.Threading.Tasks.Task task)
        {
            return task;
        }
#endif


#if USE_UNITASK
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
#else
        internal static async YarnTask Wait(this System.Threading.Tasks.Task task, TimeSpan timeout)
        {
            var delay = System.Threading.Tasks.Task.Delay(timeout);

            var winner = await System.Threading.Tasks.Task.WhenAny(task, delay);

            if (winner == task)
            {
                return;
            }
            else
            {
                throw new TimeoutException();
            }
        }
#endif

        internal static YarnTask Never(CancellationToken cancellationToken)
        {
#if USE_UNITASK
            return UniTask.Never(cancellationToken);
#else
            return Task.Delay(Timeout.Infinite, cancellationToken);
#endif
        }

    }

}
