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

    public partial struct YarnTask
    {


        public Awaitable.Awaiter GetAwaiter() => Awaitable.GetAwaiter();

        Awaitable Awaitable;
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

        public static implicit operator Awaitable(YarnTask demoYarnTask)
        {
            return demoYarnTask.Awaitable;
        }

        public static implicit operator YarnTask(Awaitable awaitable)
        {
            return new YarnTask { Awaitable = awaitable };
        }

        public static implicit operator YarnTask(System.Threading.Tasks.Task task)
        {
            async Awaitable Awaiter()
            {
                await task;
            }
            return new YarnTask { Awaitable = Awaiter() };
        }

        readonly public void Forget() { /* no-op */ }

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
            return Awaitable.WaitForSecondsAsync((float)timeSpan.TotalSeconds, token);
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

        public static partial async YarnTask<T[]> WhenAll<T>(params YarnTask<T>[] tasks)
        {
            List<T> results = new List<T>(tasks.Length);

            foreach (var awaitable in tasks)
            {
                // Unlike WhenAll() (i.e. not returning a value), if an
                // operation is cancelled, we can't provide a value for it, so
                // we can no longer provide the complete collection of results,
                // so we'll let OperationCanceledExceptions propagate out of us
                // and we won't catch it

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
        public Awaitable<T>.Awaiter GetAwaiter() => Awaitable.GetAwaiter();

        Awaitable<T> Awaitable;
        readonly public bool IsCompleted() => Awaitable.GetAwaiter().IsCompleted;
        readonly public bool IsCompletedSuccessfully() => Awaitable.GetAwaiter().IsCompleted;

        public static implicit operator Awaitable<T>(YarnTask<T> demoYarnTask)
        {
            return demoYarnTask.Awaitable;
        }

        public static implicit operator YarnTask<T>(Awaitable<T> awaitable)
        {
            return new YarnTask<T> { Awaitable = awaitable };
        }

        readonly public void Forget() { /* no-op */ }

        static readonly AwaitableCompletionSource<T> completionSource = new();

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

}

#endif
