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

namespace Yarn.Unity
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Security;
    using UnityEngine;

#if YARNTASKS_ARE_AWAITABLES
    public partial struct YarnTaskMethodBuilder
    {
        private Awaitable.AwaitableAsyncMethodBuilder methodBuilder;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private Awaitable.AwaitableAsyncMethodBuilder GetBuilder() => Awaitable.AwaitableAsyncMethodBuilder.Create();
    }
    public partial struct YarnTaskMethodBuilder<T>
    {
        private Awaitable.AwaitableAsyncMethodBuilder<T> methodBuilder;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private Awaitable.AwaitableAsyncMethodBuilder<T> GetBuilder() => Awaitable.AwaitableAsyncMethodBuilder<T>.Create();
    }
#elif YARNTASKS_ARE_UNITASKS

    using Cysharp.Threading.Tasks;
    using Cysharp.Threading.Tasks.CompilerServices;

    public partial struct YarnTaskMethodBuilder
    {
        private AsyncUniTaskMethodBuilder methodBuilder;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private AsyncUniTaskMethodBuilder GetBuilder() => AsyncUniTaskMethodBuilder.Create();
    }
    public partial struct YarnTaskMethodBuilder<T>
    {
        private AsyncUniTaskMethodBuilder<T> methodBuilder;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private AsyncUniTaskMethodBuilder<T> GetBuilder() => AsyncUniTaskMethodBuilder<T>.Create();
    }

#elif YARNTASKS_ARE_SYSTEMTASKS
    public partial struct YarnTaskMethodBuilder
    {
        private AsyncTaskMethodBuilder methodBuilder;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private AsyncTaskMethodBuilder GetBuilder() => AsyncTaskMethodBuilder.Create();
    }
    public partial struct YarnTaskMethodBuilder<T>
    {
        private AsyncTaskMethodBuilder<T> methodBuilder;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private AsyncTaskMethodBuilder<T> GetBuilder() => AsyncTaskMethodBuilder<T>.Create();
    }
#endif

    public partial struct YarnTaskMethodBuilder
    {
        // 1. Static Create method.
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YarnTaskMethodBuilder Create()
        {
            YarnTaskMethodBuilder result = default;
            result.methodBuilder = GetBuilder();
            return result;
        }
        // 2. TaskLike Task property.
        public YarnTask Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => methodBuilder.Task;
        }
        // 3. SetException
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception) => methodBuilder.SetException(exception);

        // 4. SetResult
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult() => methodBuilder.SetResult();

        // 5. AwaitOnCompleted
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine => methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        // 6. AwaitUnsafeOnCompleted
        [DebuggerHidden]
        [SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine => methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        // 7. Start
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => methodBuilder.Start(ref stateMachine);

        // 8. SetStateMachine
        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            methodBuilder.SetStateMachine(stateMachine);
        }
    }

    public partial struct YarnTaskMethodBuilder<T>
    {
        // 1. Static Create method.
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YarnTaskMethodBuilder<T> Create()
        {
            YarnTaskMethodBuilder<T> result = default;
            result.methodBuilder = GetBuilder();
            return result;
        }

        // 2. TaskLike Task property.
        public YarnTask<T> Task
        {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => methodBuilder.Task;
        }

        // 3. SetException
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
            methodBuilder.SetException(exception);
        }

        // 4. SetResult
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result) => methodBuilder.SetResult(result);

        // 5. AwaitOnCompleted
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine => methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        // 6. AwaitUnsafeOnCompleted
        [DebuggerHidden]
        [SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine => methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        // 7. Start
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine => methodBuilder.Start(ref stateMachine);

        // 8. SetStateMachine
        [DebuggerHidden]
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            methodBuilder.SetStateMachine(stateMachine);
        }
    }
}
