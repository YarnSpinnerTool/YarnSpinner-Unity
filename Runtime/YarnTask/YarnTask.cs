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
    /// <summary>
    /// Yarn Task is a convenience Awaiter that acts as a wrapper type around the underlying Awaiter that your Unity project is using.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In Unity there are many different possible Awaiter types you could be making use of.
    /// Depending on your Unity set up the Yarn Task will wrap and forward any async behaviour into:
    /// <list type="number">
    /// <item>Cysharp.Threading.Tasks.UniTask</item>
    /// <item>UnityEngine.Awaitable</item>
    /// <item>System.Threading.Tasks.Task</item>
    /// </list>
    /// These are listed in the order of priority for the underlying awaiter, the upshot being UniTask is chosen over Awaitable over Task.
    /// </para>
    /// <para>
    /// In general we recommend that you use the underlying awaiter instead of Yarn Task.
    /// Yarn Task exists primarily so that we don't have to wrap all our code and samples inside ifdefs and usings.
    /// As such it has only the minimum features needed to play the role of forwarding async calls.
    /// There are many quality of life features that the underlying awaiters have that we've not implemented because we don't need them for this job.
    /// </para>
    /// </remarks>
    [AsyncMethodBuilder(typeof(YarnTaskMethodBuilder))]
    public partial struct YarnTask { }

    /// <inheritdoc cref="YarnTask" />
    [AsyncMethodBuilder(typeof(YarnTaskMethodBuilder<>))]
    public partial struct YarnTask<T>
    {
        /// <summary>
        /// Create a new completed task encapsulating a specific value.
        /// </summary>
        /// <param name="value">The returned value from this task</param>
        public static partial YarnTask<T> FromResult(T value);
    }

    /// <summary>
    /// Provides a mechanism to create a Yarn Task and set it as completed or cancelled from a non-async environment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The main use of this class is to be able to provide a Yarn Task that can be awaited like any other, but have it's completion set not by having a method that finishes, but by an external factor.
    /// Essentially this allows non-async code to create a facade of asynchronicity without changing their underlying implementation significantly.
    /// </para>
    /// <para>
    /// Used in many different places one of the main uses of this is to allow the Unity UI and Input systems to use a callback to set an option as selected as they expect.
    /// These callbacks then use this class to make and complete a task that the rest of Yarn Spinner can await like normal.
    /// This allows the Dialogue Presenter to think as everything as being async and allows the UI to think in terms of callbacks, each gets to work how they prefer.
    /// </para>
    /// <para>
    /// This is the Yarn Task form of the Task/Awaitable/UniTask <c>TaskCompletionSource</c> classes.
    /// Similar to <see cref="YarnTask"/> itself we recommend that where possible you use your underlying awaiter infrastructures equivalents of this class in preference to this class.
    /// </para>
    /// </remarks>
    /// <seealso cref="YarnTask"/>
    public partial class YarnTaskCompletionSource
    {
        /// <summary>
        /// Attempt to set the task as complete
        /// </summary>
        /// <returns>True if the task was successfully set as completed, false otherwise.</returns>
        public partial bool TrySetResult();
        /// <summary>
        /// Attempt to set the task as prematurely ended due to an unhandled exception being thrown.
        /// </summary>
        /// <param name="exception">The exception that has caused the task to end prematurely</param>
        /// <returns>True if the task was able to be set as prematurely ended</returns>
        public partial bool TrySetException(System.Exception exception);
        /// <summary>
        /// Attempt to set the task as cancelled
        /// </summary>
        /// <returns>True if the task was able to be cancelled successfully</returns>
        public partial bool TrySetCanceled();
    }

    /// <inheritdoc cref="YarnTaskCompletionSource" />
    public partial class YarnTaskCompletionSource<T>
    {
        /// <summary>
        /// Attempt to set a task as complete with the value
        /// </summary>
        /// <param name="value">The value result, can be retrieved externally from <see cref="YarnTaskCompletionSource{T}.Task"/></param>
        /// <returns></returns>
        public partial bool TrySetResult(T value);
        /// <inheritdoc cref="YarnTaskCompletionSource.TrySetException(Exception)" />
        public partial bool TrySetException(System.Exception exception);
        /// <inheritdoc cref="YarnTaskCompletionSource.TrySetCanceled" />
        public partial bool TrySetCanceled();
    }

    // Static implementation-dependent utility methods
    public partial struct YarnTask
    {
        /// <summary>
        /// Task that waits until the <paramref name="token"/> has been set as cancelled.
        /// </summary>
        /// <param name="token">The token to wait on</param>
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
        
        /// <inheritdoc cref="Delay(TimeSpan, CancellationToken)"/>
        /// <param name="milliseconds">The amount of time to wait.</param>
        public static YarnTask Delay(int milliseconds, CancellationToken token = default) => Delay(TimeSpan.FromMilliseconds(milliseconds), token);
        
        /// <summary>
        /// Task that waits until the <paramref name="predicate"/> evaluates to true.
        /// </summary>
        /// <param name="predicate">The predicate used to control when this task completes. As long as this function evaluates to false the task will not complete.</param>
        /// <param name="token">A token that can be used to cancel the task.</param>
        public static partial YarnTask WaitUntil(System.Func<bool> predicate, System.Threading.CancellationToken token = default);
        
        /// <summary>
        /// Given a specific task converts it into a form that can be run as though it were a Unity coroutine.
        /// </summary>
        /// <param name="factory">A function that creates a Yarn Task</param>
        /// <returns>The Coroutine form of the task</returns>
        public static partial IEnumerator ToCoroutine(Func<YarnTask> factory);

        /// <summary>
        /// Yields for one frame.
        /// </summary>
        /// <remarks>
        /// While the intent is that this will wait until the next frame before continuing, when using System.Threading.Tasks.Task as your underlying awaiter this method may return faster or slower than a single frame.
        /// This is because Task (unlike UniTask or Awaitables) has no understanding of the Unty renderloop.
        /// </remarks>
        public static partial YarnTask Yield();

        /// <summary>
        /// Waits for all tasks to complete before finishing
        /// </summary>
        /// <param name="tasks">The tasks to wait for</param>
        public static partial YarnTask WhenAll(params YarnTask[] tasks);
        
        /// <inheritdoc cref="WhenAll(YarnTask[])"/>
        public static partial YarnTask WhenAll(IEnumerable<YarnTask> tasks);
        /// <inheritdoc cref="WhenAll(YarnTask[])"/>
        public static partial YarnTask<T[]> WhenAll<T>(params YarnTask<T>[] tasks);
        /// <inheritdoc cref="WhenAll(YarnTask[])"/>
        public static partial YarnTask<T[]> WhenAll<T>(IEnumerable<YarnTask<T>> tasks);

        /// <summary>
        /// Awaits the task and captures and suppresses the OperationCanceledException that would otherwise be thrown if the task is cancelled
        /// </summary>
        /// <returns>True if the task did throw, false otherwise</returns>
        public readonly partial YarnTask<bool> SuppressCancellationThrow();

        /// <inheritdoc cref="YarnTask{T}.FromResult(T)"/>
        public static YarnTask<T> FromResult<T>(T value) => YarnTask<T>.FromResult(value);
    }

    // Addressables
#if USE_ADDRESSABLES
    public partial struct YarnTask
    {
        /// <summary>
        /// Waits for the given addressable operation to finish before continuing
        /// </summary>
        /// <param name="operationHandle">The handle into the addressable operation</param>
        /// <param name="cancellationToken">Cancellation token for the tasl</param>
        public static partial YarnTask WaitForAsyncOperation(AsyncOperationHandle operationHandle, CancellationToken cancellationToken);
        /// <inheritdoc cref="WaitForAsyncOperation(AsyncOperationHandle, CancellationToken)"/>
        /// <returns>The result of the addressable operation</returns>
        public static partial YarnTask<T> WaitForAsyncOperation<T>(AsyncOperationHandle<T> operationHandle, CancellationToken cancellationToken);
    }
#endif

    public static class YarnTaskExtensions
    {
        /// <summary>
        /// Waits for the given coroutine to finish before continuing
        /// </summary>
        /// <param name="runner">the host for the coroutine</param>
        /// <param name="coroutine">the coroutine to wait upon</param>
        public static YarnTask WaitForCoroutine(this MonoBehaviour runner, IEnumerator coroutine)
        {
            return WaitForCoroutine(runner, runner.StartCoroutine(coroutine));
        }
        /// <inheritdoc cref="WaitForCoroutine(MonoBehaviour,IEnumerator)" />
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

