/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

// This file contains helpers that make it easier for the Yarn Spinner code to
// not have to think about which async API it's using.

#nullable enable

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Yarn.Unity
{
#if !USE_UNITASK && UNITY_2023_1_OR_NEWER
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Runtime.CompilerServices;

    using UnityEngine;
#if USE_ADDRESSABLES
    using UnityEngine.ResourceManagement.AsyncOperations;
#endif

    using YarnTask = UnityEngine.Awaitable;
    using YarnOptionTask = UnityEngine.Awaitable<DialogueOption?>;

    public static partial class YarnAsync
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitUntilCanceled(System.Threading.CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        // this is based on https://discussions.unity.com/t/awaitable-fromresult/943659/7
        public static Awaitable<TResult> FromResult<TResult>(TResult result)
        {
            var nullStateMachine = new NullStateMachine();
            var builder = Awaitable.AwaitableAsyncMethodBuilder<TResult>.Create();
            builder.Start(ref nullStateMachine);
            builder.SetResult(result);
            return builder.Task;
        }
        private readonly struct NullStateMachine : IAsyncStateMachine
        {
            public void MoveNext() { }
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }

        // this is based on https://discussions.unity.com/t/awaitable-equivalent-of-task-completedtask/1546128/4
        private static readonly AwaitableCompletionSource completedTaskCompletionSource = new();
        public static Awaitable CompletedTask
        {
            get
            {
                completedTaskCompletionSource.SetResult();
                var awaitable = completedTaskCompletionSource.Awaitable;
                completedTaskCompletionSource.Reset();
                return awaitable;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask Yield()
        {
            await Awaitable.EndOfFrameAsync();
        }

        public async static YarnTask Delay(int milliseconds, CancellationToken token)
        {
            try
            {
                float seconds = milliseconds / 1000f;
                await YarnTask.WaitForSecondsAsync(seconds, token);
            }
            catch (OperationCanceledException)
            {
                // we don't want to throw an exception for this because this is valid behaviour
                // why did you do it this way c# ?
                // WHY?
                return;
            }
        }
        private async static YarnTask Delay(float seconds, CancellationToken token)
        {
            try
            {
                await YarnTask.WaitForSecondsAsync(seconds, token);
            }
            catch (OperationCanceledException)
            {
                // we don't want to throw an exception for this because this is valid behaviour
                // why did you do it this way c# ?
                // WHY?
                return;
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
            await mb.WaitForCoroutine(mb.StartCoroutine(coro), cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitForCoroutine(this MonoBehaviour mb, Coroutine coro, CancellationToken cancellationToken = default)
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
                if (cancellationToken.IsCancellationRequested)
                {
                    mb.StopCoroutine(waitingCoroutine);
                    return;
                }
                await YarnAsync.Yield();
            }
        }

#if USE_ADDRESSABLES
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Awaitable<T> WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken)
        {
            return await operationHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async YarnTask WaitForAsyncOperation(AsyncOperationHandle operationHandle, CancellationToken cancellationToken)
        {
            return await operationHandle;
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
        }

        internal static bool IsCompletedSuccessfully(this Awaitable task)
        {
            return task.IsCompleted;
        }

        internal static YarnTask Never(CancellationToken cancellationToken)
        {
            return Delay(Mathf.Infinity, cancellationToken);
        }

        public static async Awaitable WhenAll(IEnumerable<Awaitable> tasks)
        {
            foreach (var awaitable in tasks)
            {
                try
                {
                    await awaitable;
                }
                catch (OperationCanceledException)
                {
                    continue;
                }
            }
        }

        internal static Awaitable AsTask(this AwaitableCompletionSource source)
        {
            return source.Awaitable;
        }
        internal static Awaitable AsAwaitable(this AwaitableCompletionSource source)
        {
            return source.Awaitable;
        }
        internal static Awaitable<T> AsTask<T>(this AwaitableCompletionSource<T> source)
        {
            return source.Awaitable;
        }
        internal static Awaitable<T> AsAwaitable<T>(this AwaitableCompletionSource<T> source)
        {
            return source.Awaitable;
        }
        internal static async YarnTask AsYarnTask(this System.Threading.Tasks.Task task)
        {
            await task;
        }

        internal static async YarnOptionTask CancellationOptionTask(CancellationToken token)
        {
            try
            {
                await Never(token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            return null;
        }
    }
#endif
}
