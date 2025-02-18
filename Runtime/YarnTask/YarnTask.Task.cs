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

#if YARNTASKS_ARE_SYSTEMTASKS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if USE_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Yarn.Unity
{

    public partial struct YarnTask
    {
        public TaskAwaiter GetAwaiter() => Task.GetAwaiter();

        Task Task;
        readonly public bool IsCompleted() => Task.IsCompleted;
        readonly public bool IsCompletedSuccessfully() => Task.IsCompletedSuccessfully;

        public static implicit operator Task(YarnTask YarnTask)
        {
            return YarnTask.Task;
        }

        public static implicit operator YarnTask(Task task)
        {
            return new YarnTask { Task = task };
        }
#if UNITY_2023_1_OR_NEWER
        public static implicit operator YarnTask(Awaitable awaitable)
        {
            return new YarnTask { Task = awaitable.AsTask() };
        }
#endif

#if USE_UNITASK
        public static implicit operator YarnTask(Cysharp.Threading.Tasks.UniTask uniTask)
        {
            return new YarnTask { Task = Cysharp.Threading.Tasks.UniTaskExtensions.AsTask(uniTask) };
        }
#endif

        readonly public async void Forget()
        {
            try
            {
                await Task;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static partial async YarnTask WaitUntilCanceled(System.Threading.CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Yield();
            }
        }

        public static YarnTask CompletedTask => Task.CompletedTask;

        /// <summary>
        /// Creates a <see cref="YarnTask"/> that delays for the time indicated
        /// by <paramref name="timeSpan"/>, and then returns.
        /// </summary>
        /// <param name="timeSpan">The amount of time to wait.</param>
        /// <param name="token">A token that can be used to cancel the
        /// task.</param>
        /// <returns>A new <see cref="YarnTask"/>.</returns>
        public static partial YarnTask Delay(TimeSpan timeSpan, CancellationToken token)
        {
            return Task.Delay(timeSpan, token);
        }

        public static partial async YarnTask WaitUntil(System.Func<bool> predicate, System.Threading.CancellationToken token)
        {
            while (!token.IsCancellationRequested && predicate() == false)
            {
                await Task.Yield();
            }
        }

        public static partial IEnumerator ToCoroutine(Func<YarnTask> factory)
        {
            IEnumerator InnerCoroutine(Task t)
            {
                while (!t.IsCompleted)
                {
                    yield return null;
                }
                if (t.IsFaulted)
                {
                    Debug.LogException(t.Exception);
                }
            }
            return InnerCoroutine(factory());
        }
        public static partial async YarnTask Yield()
        {
            await Task.Yield();
        }

        public static partial YarnTask WhenAll(params YarnTask[] tasks)
        {
            return WhenAll((IEnumerable<YarnTask>)tasks);
        }
        public static partial async YarnTask WhenAll(IEnumerable<YarnTask> tasks)
        {
            // Don't love this allocation here; try and find a better approach
            List<Task> taskList = new List<Task>();
            foreach (var task in tasks)
            {
                taskList.Add(task);
            }

            await Task.WhenAll(taskList.ToArray());

        }

        public static async partial YarnTask<T[]> WhenAll<T>(params YarnTask<T>[] tasks)
        {
            return await Task.WhenAll(Array.ConvertAll<YarnTask<T>, Task<T>>(tasks, t => t));
        }

        public static async partial YarnTask<T[]> WhenAll<T>(IEnumerable<YarnTask<T>> tasks)
        {
            var uniTasks = new List<Task<T>>();
            foreach (var task in tasks)
            {
                uniTasks.Add(task);
            }
            return await Task.WhenAll(uniTasks);

        }

        public readonly async partial YarnTask<bool> SuppressCancellationThrow()
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

#if USE_ADDRESSABLES
        public static partial async YarnTask WaitForAsyncOperation(AsyncOperationHandle operationHandle, CancellationToken cancellationToken)
        {
            await operationHandle.Task;
        }

        public static partial async YarnTask<T> WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken)
        {
            return await operationHandle.Task;
        }
#endif

    }

    public partial struct YarnTask<T>
    {
        Task<T> Task;
        public TaskAwaiter<T> GetAwaiter() => Task.GetAwaiter();

        readonly public bool IsCompleted() => Task.IsCompleted;
        readonly public bool IsCompletedSuccessfully() => Task.IsCompletedSuccessfully;

        public static implicit operator Task<T>(YarnTask<T> YarnTask)
        {
            return YarnTask.Task;
        }

        public static implicit operator YarnTask<T>(Task<T> task)
        {
            return new YarnTask<T> { Task = task };
        }

#if UNITY_2023_1_OR_NEWER
        public static implicit operator YarnTask<T>(Awaitable<T> awaitable)
        {
            return new YarnTask<T> { Task = awaitable.AsTask() };
        }
#endif

#if USE_UNITASK
        public static implicit operator YarnTask<T>(Cysharp.Threading.Tasks.UniTask<T> uniTask)
        {
            return new YarnTask<T> { Task = Cysharp.Threading.Tasks.UniTaskExtensions.AsTask(uniTask) };
        }
#endif

        public static partial YarnTask<T> FromResult(T value)
        {
            return Task<T>.FromResult(value);
        }

        readonly public void Forget() { }
    }

    public partial class YarnTaskCompletionSource
    {
        private TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();

        public partial bool TrySetResult()
        {
            return taskCompletionSource.TrySetResult(1);
        }
        public partial bool TrySetException(System.Exception exception)
        {
            return taskCompletionSource.TrySetException(exception);
        }
        public partial bool TrySetCanceled()
        {
            return taskCompletionSource.TrySetCanceled();
        }

        public YarnTask Task => taskCompletionSource.Task;
    }
    public partial class YarnTaskCompletionSource<T>
    {
        private TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();

        public partial bool TrySetResult(T value)
        {
            return taskCompletionSource.TrySetResult(value);
        }
        public partial bool TrySetException(System.Exception exception)
        {
            return taskCompletionSource.TrySetException(exception);
        }
        public partial bool TrySetCanceled()
        {
            return taskCompletionSource.TrySetCanceled();
        }

        public YarnTask<T> Task => taskCompletionSource.Task;
    }

    static class TaskUtility
    {
#if UNITY_2023_1_OR_NEWER
        public static async Task AsTask(this Awaitable awaitable)
        {
            await awaitable;
        }

        public static async Task<T> AsTask<T>(this Awaitable<T> awaitable)
        {
            return await awaitable;
        }
#endif
    }
}
#endif
