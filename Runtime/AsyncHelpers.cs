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
#elif UNITY_2023_1_OR_NEWER
    using YarnTask = UnityEngine.Awaitable;
    using YarnOptionTask = UnityEngine.Awaitable<DialogueOption?>;
#else
    using YarnTask = System.Threading.Tasks.Task;
    using YarnObjectTask = System.Threading.Tasks.Task<UnityEngine.Object?>;
    using YarnOptionTask = System.Threading.Tasks.Task<DialogueOption?>;
    using System.Threading.Tasks;
#endif

    public static partial class YarnAsync
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static YarnTask WaitUntil(System.Func<bool> predicate, System.Threading.CancellationToken token)
        {
            while (!token.IsCancellationRequested && predicate() == false)
            {
                await YarnAsync.Yield();
            }
        }

        public static YarnOptionTask NoOptionSelected
        {
            get
            { 
                return YarnAsync.FromResult<DialogueOption?>(null);
            }
        }

#if USE_UNITASK
        internal static bool IsCompleted(this UniTask task)
        {
            return task.Status != UniTaskStatus.Pending;
        }
        internal static bool IsCompleted<T>(this UniTask<T> task)
        {
            return task.Status != UniTaskStatus.Pending;
        }
        internal static bool IsCompletedSuccessfully(this UniTask task)
        {
            return task.Status != UniTaskStatus.Succeeded;
        }
        internal static bool IsCompletedSuccessfully<T>(this UniTask<T> task)
        {
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
    }
}
