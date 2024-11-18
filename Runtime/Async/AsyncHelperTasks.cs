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

#if !USE_UNITASK && !UNITY_2023_1_OR_NEWER
    using YarnTask = System.Threading.Tasks.Task;
    using YarnObjectTask = System.Threading.Tasks.Task<UnityEngine.Object?>;
    using YarnOptionTask = System.Threading.Tasks.Task<DialogueOption?>;
    using System.Threading.Tasks;

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
                return;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YarnTask Forget(this YarnTask task)
        {
            return task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitForCoroutine(this MonoBehaviour mb, IEnumerator coro, CancellationToken cancellationToken = default)
        {
            // Otherwise, we'll need to just start the coroutine directly and
            // wait for it to run to completion
            await mb.WaitForCoroutine(mb.StartCoroutine(coro));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitForCoroutine(this MonoBehaviour mb, Coroutine coro)
        {
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
        }

#if USE_ADDRESSABLES
        // Type aliases don't currently support generics, so in order to support returning
        // a task-like object that yields type T, we need to use gross ifdefs
        // when specifying the return type.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T?> WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken)
        {
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async YarnTask WaitForAsyncOperation(AsyncOperationHandle operationHandle, CancellationToken cancellationToken)
        {
            while (operationHandle.IsDone == false)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await YarnTask.Yield();
            }

            return;
        }

#endif

        public static IEnumerator ToCoroutine(Func<YarnTask> factory)
        {
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
        }
        
        internal static bool IsCompleted(this System.Threading.Tasks.Task task) {
            return task.IsCompleted;
        }
        internal static bool IsCompleted<T>(this System.Threading.Tasks.Task task) {
            return task.IsCompleted;
        }

        internal static bool IsCompletedSuccessfully(this System.Threading.Tasks.Task task) {
            return task.IsCompletedSuccessfully;
        }
        internal static bool IsCompletedSuccessfully<T>(this System.Threading.Tasks.Task task) {
            return task.IsCompletedSuccessfully;
        }

        internal static YarnTask AsYarnTask(this System.Threading.Tasks.Task task)
        {
            return task;
        }

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

        internal static YarnTask Never(CancellationToken cancellationToken)
        {
            return Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
#endif
}
