/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

// If we don't already have a directive telling us what to use...
#if !YARNTASKS_ARE_AWAITABLES && !YARNTASKS_ARE_SYSTEMTASKS && !YARNTASKS_ARE_UNITASKS
// ...then figure out what the best option is. We'll try to use UniTask, if
// installed; then Awaitables, if >= Unity 2023.1; then System.Threading.Tasks.
#if USE_UNITASK
#define YARNTASKS_ARE_UNITASKS
#elif UNITY_2023_1_OR_NEWER
#define YARNTASKS_ARE_AWAITABLES
#else
#define YARNTASKS_ARE_SYSTEMTASKS
#endif
#endif

#if YARNTASKS_ARE_AWAITABLES

namespace Yarn.Unity
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using UnityEngine;

#if USE_ADDRESSABLES
    using UnityEngine.ResourceManagement.AsyncOperations;
#endif

    public readonly partial struct YarnTask
    {
        public readonly Awaitable.Awaiter GetAwaiter() => Awaitable.GetAwaiter();

        readonly Awaitable Awaitable;
        readonly public bool IsCompleted() => Awaitable.IsCompleted;
        readonly public bool IsCompletedSuccessfully() => Awaitable.IsCompleted;

        // Thanks to sisus_co on the Unity Discussions forum:
        // https://discussions.unity.com/t/awaitable-equivalent-of-task-completedtask/1546128/4
        static readonly AwaitableCompletionSource completionSource = new();

        public static YarnTask CompletedTask
        {
            get
            {
                completionSource.SetResult();
                var awaitable = completionSource.Awaitable;
                completionSource.Reset();
                return awaitable;
            }
        }

        private YarnTask(Awaitable awaitable)
        {
            Awaitable = awaitable;
        }

        public static implicit operator Awaitable(YarnTask demoYarnTask)
        {
            return demoYarnTask.Awaitable;
        }

        public static implicit operator YarnTask(Awaitable awaitable)
        {
            return new YarnTask(awaitable);
        }

        public static implicit operator YarnTask(System.Threading.Tasks.Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task),"You are attempting to convert a null Task into a YarnTask, did you mean to use YarnTask.CompletedTask?");
            }
            async Awaitable Awaiter()
            {
                await task;
            }
            return new YarnTask(Awaiter());
        }

#if USE_UNITASK
        public static implicit operator YarnTask(Cysharp.Threading.Tasks.UniTask uniTask)
        {
            return new YarnTask { Awaitable = uniTask.AsAwaitable() };
        }
#endif

        readonly public async void Forget()
        {
            try
            {
                await Awaitable;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static partial async YarnTask Yield()
        {
            await Awaitable.NextFrameAsync();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static partial YarnTask WaitUntilCanceled(System.Threading.CancellationToken token)
        {
            while (token.IsCancellationRequested == false)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static partial YarnTask Delay(TimeSpan timeSpan, CancellationToken token)
        {
            try
            {
                return Awaitable.WaitForSecondsAsync((float)timeSpan.TotalSeconds, token);
            }
            catch (OperationCanceledException)
            {
                return YarnTask.CompletedTask;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static partial YarnTask WaitUntil(System.Func<bool> predicate, System.Threading.CancellationToken token)
        {
            while (token.IsCancellationRequested == false && predicate() == false)
            {
                await Awaitable.NextFrameAsync(token);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static partial IEnumerator ToCoroutine(Func<YarnTask> factory)
        {
            return factory().Awaitable;
        }

        public static partial YarnTask WhenAll(params YarnTask[] tasks)
        {
            return WhenAll((IEnumerable<YarnTask>)tasks);
        }
        public static partial async YarnTask WhenAll(IEnumerable<YarnTask> tasks)
        {
            foreach (var awaitable in tasks)
            {
                await awaitable;
            }
        }

        public static partial YarnTask<T[]> WhenAll<T>(params YarnTask<T>[] tasks)
        {
            return WhenAll((IEnumerable<YarnTask<T>>)tasks);
        }
        public static partial async YarnTask<T[]> WhenAll<T>(IEnumerable<YarnTask<T>> tasks)
        {
            List<T> results = new List<T>(tasks is Array taskArray ? taskArray.Length : 4);

            foreach (var awaitable in tasks)
            {
                var result = await awaitable;
                results.Add(result);
            }
            return results.ToArray();
        }

#if USE_ADDRESSABLES

        public static partial async YarnTask WaitForAsyncOperation(AsyncOperationHandle operationHandle, CancellationToken cancellationToken)
        {
            var tcs = new AwaitableCompletionSource();
            operationHandle.Completed += (t) =>
            {
                switch (t.Status)
                {
                    case AsyncOperationStatus.Succeeded:
                        tcs.TrySetResult();
                        break;
                    case AsyncOperationStatus.Failed:
                        tcs.TrySetException(t.OperationException);
                        break;
                }
            };
            await tcs.Awaitable;
        }

        public static partial async YarnTask<T> WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken)
        {
            var tcs = new AwaitableCompletionSource<T>();
            operationHandle.Completed += (t) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    return;
                }
                switch (t.Status)
                {
                    case AsyncOperationStatus.Succeeded:
                        tcs.TrySetResult(t.Result);
                        break;
                    case AsyncOperationStatus.Failed:
                        tcs.TrySetException(t.OperationException);
                        break;
                }
            };
            return await tcs.Awaitable;
        }
#endif

        public readonly partial async YarnTask<bool> SuppressCancellationThrow()
        {
            try
            {
                await Awaitable;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
            return false;
        }
    }

    public partial struct YarnTask<T>
    {
        public readonly Awaitable<T>.Awaiter GetAwaiter() => Awaitable.GetAwaiter();

        Awaitable<T> Awaitable;
        readonly public bool IsCompleted() => Awaitable.GetAwaiter().IsCompleted;
        readonly public bool IsCompletedSuccessfully() => Awaitable.GetAwaiter().IsCompleted;

        public static implicit operator Awaitable<T>(YarnTask<T> demoYarnTask)
        {
            return demoYarnTask.Awaitable;
        }

        public static implicit operator YarnTask<T>(Awaitable<T> awaitable)
        {
            if (awaitable == null)
            {
                throw new ArgumentNullException(nameof(awaitable),$"You are attempting to convert a null Awaitable<{typeof(T).Name}> into a YarnTask, did you mean to use YarnTask<{typeof(T).Name}>.FromResult(null) instead?");
            }
            return new YarnTask<T> { Awaitable = awaitable };
        }

#if USE_UNITASK
        public static implicit operator YarnTask<T>(Cysharp.Threading.Tasks.UniTask<T> uniTask)
        {
            return new YarnTask<T> { Awaitable = uniTask.AsAwaitable() };
        }
#endif

        readonly public async void Forget()
        {
            // Run the task, and if it throws an exception, log it (instead of
            // letting it disappear.)
            try
            {
                await this.Awaitable;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        public static partial YarnTask<T> FromResult(T value)
        {
            // this is based on https://discussions.unity.com/t/awaitable-fromresult/943659/7
            var nullStateMachine = new NullStateMachine();
            var builder = UnityEngine.Awaitable.AwaitableAsyncMethodBuilder<T>.Create();
            builder.Start(ref nullStateMachine);
            builder.SetResult(value);
            return builder.Task;
        }

        private readonly struct NullStateMachine : IAsyncStateMachine
        {
            public void MoveNext() { }
            public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        }

    }


    public partial class YarnTaskCompletionSource
    {
        private AwaitableCompletionSource awaitableCompletionSource = new();

        public partial bool TrySetResult() => awaitableCompletionSource.TrySetResult();
        public partial bool TrySetException(Exception exception) => awaitableCompletionSource.TrySetException(exception);
        public partial bool TrySetCanceled() => awaitableCompletionSource.TrySetCanceled();

        public YarnTask Task => awaitableCompletionSource.Awaitable;
    }

    public partial class YarnTaskCompletionSource<T>
    {
        private AwaitableCompletionSource<T> awaitableCompletionSource = new();

        public partial bool TrySetResult(T value) => awaitableCompletionSource.TrySetResult(value);
        public partial bool TrySetException(Exception exception) => awaitableCompletionSource.TrySetException(exception);
        public partial bool TrySetCanceled() => awaitableCompletionSource.TrySetCanceled();

        public YarnTask<T> Task => awaitableCompletionSource.Awaitable;
    }

    static class AwaitableUtility
    {
#if USE_UNITASK
        public static async Awaitable AsAwaitable(this Cysharp.Threading.Tasks.UniTask awaitable)
        {
            await awaitable;
        }

        public static async Awaitable<T> AsAwaitable<T>(this Cysharp.Threading.Tasks.UniTask<T> awaitable)
        {
            return await awaitable;
        }
#endif
    }

}

#endif
