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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using UnityEngine;

#if USE_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Yarn.Unity
{
    public interface IYarnTask
    {
        bool IsCompleted();
        bool IsCompletedSuccessfully();
        void Forget();
    }

    [AsyncMethodBuilder(typeof(YarnTaskMethodBuilder))]
    public partial struct YarnTask { }

    [AsyncMethodBuilder(typeof(YarnTaskMethodBuilder<>))]
    public partial struct YarnTask<T>
    {
        public static partial YarnTask<T> FromResult(T value);
    }

    public partial class YarnTaskCompletionSource
    {
        public partial bool TrySetResult();
        public partial bool TrySetException(System.Exception exception);
        public partial bool TrySetCanceled();
    }

    public partial class YarnTaskCompletionSource<T>
    {
        public partial bool TrySetResult(T value);
        public partial bool TrySetException(System.Exception exception);
        public partial bool TrySetCanceled();
    }

    // Static implementation-dependent utility methods
    public partial struct YarnTask
    {
        public static partial YarnTask WaitUntilCanceled(System.Threading.CancellationToken token);

        /// <summary>
        /// Creates a <see cref="YarnTask"/> that delays for the time indicated
        /// by <paramref name="timeSpan"/>, and then returns.
        /// </summary>
        /// <param name="timeSpan">The amount of time to wait.</param>
        /// <param name="token">A token that can be used to cancel the
        /// task.</param>
        /// <returns>A new <see cref="YarnTask"/>.</returns>
        public static partial YarnTask Delay(TimeSpan timeSpan, CancellationToken token = default);
        public static YarnTask Delay(int milliseconds, CancellationToken token = default) => Delay(TimeSpan.FromMilliseconds(milliseconds), token);
        public static partial YarnTask WaitUntil(System.Func<bool> predicate, System.Threading.CancellationToken token = default);
        public static partial IEnumerator ToCoroutine(Func<YarnTask> factory);
        public static partial YarnTask Yield();

        public static partial YarnTask WhenAll(params YarnTask[] tasks);
        public static partial YarnTask WhenAll(IEnumerable<YarnTask> tasks);
        public static partial YarnTask<T[]> WhenAll<T>(params YarnTask<T>[] tasks);
        public static partial YarnTask<T[]> WhenAll<T>(IEnumerable<YarnTask<T>> tasks);

        public readonly partial YarnTask<bool> SuppressCancellationThrow();

        public static YarnTask<T> FromResult<T>(T value) => YarnTask<T>.FromResult(value);
    }

    // Addressables
#if USE_ADDRESSABLES
    public partial struct YarnTask
    {
        public static partial YarnTask WaitForAsyncOperation(AsyncOperationHandle operationHandle, CancellationToken cancellationToken);
        public static partial YarnTask<T> WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken);
    }
#endif

    public static class YarnTaskExtensions
    {
        public static YarnTask WaitForCoroutine(this MonoBehaviour runner, IEnumerator coroutine)
        {
            return WaitForCoroutine(runner, runner.StartCoroutine(coroutine));
        }
        public static YarnTask WaitForCoroutine(this MonoBehaviour runner, Coroutine coroutine)
        {
            var tcs = new YarnTaskCompletionSource();
            IEnumerator InnerCoroutine()
            {
                yield return coroutine;
                tcs.TrySetResult();
            }
            runner.StartCoroutine(InnerCoroutine());

            return tcs.Task;
        }
    }
}

