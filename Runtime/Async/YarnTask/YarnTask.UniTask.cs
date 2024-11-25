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

#if YARNTASKS_ARE_UNITASKS
namespace Yarn.Unity
{

    using Cysharp.Threading.Tasks;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

#if USE_ADDRESSABLES
    using UnityEngine.ResourceManagement.AsyncOperations;
#endif

    public partial struct YarnTask
    {
        public UniTask.Awaiter GetAwaiter() => Task.GetAwaiter();

        UniTask Task;
        readonly public bool IsCompleted() => Task.Status != UniTaskStatus.Pending;
        readonly public bool IsCompletedSuccessfully() => Task.Status == UniTaskStatus.Succeeded;

        public static YarnTask CompletedTask => UniTask.CompletedTask;

        public static implicit operator UniTask(YarnTask demoYarnTask)
        {
            return demoYarnTask.Task;
        }

        public static implicit operator YarnTask(UniTask task)
        {
            return new YarnTask { Task = task };
        }

        public static implicit operator YarnTask(System.Threading.Tasks.Task task)
        {
            return new YarnTask { Task = task.AsUniTask() };
        }

        readonly public void Forget() => Task.Forget();

        public static partial YarnTask WaitUntilCanceled(System.Threading.CancellationToken token)
        {
            return UniTask.WaitUntilCanceled(token);
        }

        public static partial YarnTask Delay(TimeSpan timeSpan, CancellationToken token)
        {
            return UniTask.Delay(timeSpan, cancellationToken: token);
        }

        public static partial YarnTask WaitUntil(System.Func<bool> predicate, System.Threading.CancellationToken token)
        {
            return UniTask.WaitUntil(predicate, cancellationToken: token);
        }

        public static partial IEnumerator ToCoroutine(Func<YarnTask> factory)
        {
            return UniTask.ToCoroutine(async () => await factory());
        }

        public static partial async YarnTask Yield() => await UniTask.Yield();

        public static partial YarnTask WhenAll(params YarnTask[] tasks)
        {
            return WhenAll((IEnumerable<YarnTask>)tasks);
        }
        public static partial async YarnTask WhenAll(IEnumerable<YarnTask> tasks)
        {
            // Don't love this allocation here; try and find a better approach
            List<UniTask> taskList = new List<UniTask>();
            foreach (var task in tasks)
            {
                taskList.Add(task);
            }

            await UniTask.WhenAll(taskList.ToArray());

        }

        public static async partial YarnTask<T[]> WhenAll<T>(params YarnTask<T>[] tasks)
        {
            return await UniTask.WhenAll(Array.ConvertAll<YarnTask<T>, UniTask<T>>(tasks, t => t));
        }

        public static async partial YarnTask<T[]> WhenAll<T>(IEnumerable<YarnTask<T>> tasks)
        {
            var uniTasks = new List<UniTask<T>>();
            foreach (var task in tasks)
            {
                uniTasks.Add(task);
            }
            return await UniTask.WhenAll(uniTasks);

        }

#if USE_ADDRESSABLES
        public static partial async YarnTask WaitForAsyncOperation(AsyncOperationHandle operationHandle, CancellationToken cancellationToken)
        {
            await operationHandle.ToUniTask(cancellationToken: cancellationToken);
        }

        public static partial async YarnTask<T> WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken)
        {
            return await operationHandle.ToUniTask(cancellationToken: cancellationToken);
        }
#endif

        public readonly partial async YarnTask<bool> SuppressCancellationThrow()
        {
            try
            {
                await Task;
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
        UniTask<T> Task;
        public UniTask<T>.Awaiter GetAwaiter() => Task.GetAwaiter();

        readonly public bool IsCompleted() => Task.Status != UniTaskStatus.Pending;
        readonly public bool IsCompletedSuccessfully() => Task.Status == UniTaskStatus.Succeeded;

        public static implicit operator UniTask<T>(YarnTask<T> demoYarnTask)
        {
            return demoYarnTask.Task;
        }

        public static implicit operator YarnTask<T>(UniTask<T> task)
        {
            return new YarnTask<T> { Task = task };
        }

        readonly public void Forget() => Task.Forget();

        public static partial YarnTask<T> FromResult(T value)
        {
            return UniTask.FromResult(value);
        }
    }


    public partial class YarnTaskCompletionSource
    {
        private UniTaskCompletionSource awaitableCompletionSource = new();
        public partial bool TrySetResult()
        {
            return awaitableCompletionSource.TrySetResult();
        }

        public partial bool TrySetException(System.Exception exception)
        {
            return awaitableCompletionSource.TrySetException(exception);
        }

        public partial bool TrySetCanceled()
        {
            return awaitableCompletionSource.TrySetCanceled();
        }

        public YarnTask Task => awaitableCompletionSource.Task;
    }

    public partial class YarnTaskCompletionSource<T>
    {
        private UniTaskCompletionSource<T> awaitableCompletionSource = new();
        public partial bool TrySetResult(T value)
        {
            return awaitableCompletionSource.TrySetResult(value);
        }

        public partial bool TrySetException(System.Exception exception)
        {
            return awaitableCompletionSource.TrySetException(exception);
        }

        public partial bool TrySetCanceled()
        {
            return awaitableCompletionSource.TrySetCanceled();
        }
        public YarnTask<T> Task => awaitableCompletionSource.Task;
    }
}
#endif