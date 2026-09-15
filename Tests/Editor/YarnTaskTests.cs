/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using NUnit.Framework;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity;

#nullable enable

namespace Yarn.Unity.Tests
{
    public class YarnTaskTests
    {
        [Test]
        public void YarnTaskCanImplicitlyConvert()
        {
            YarnTask plainYarnTask;

            // Task into YarnTask
            System.Threading.Tasks.Task plainTask = System.Threading.Tasks.Task.CompletedTask;
            plainYarnTask = plainTask;

#if USE_UNITASK
            // UniTask into YarnTask
            Cysharp.Threading.Tasks.UniTask plainUniTask = Cysharp.Threading.Tasks.UniTask.CompletedTask;
            plainYarnTask = plainUniTask;
#endif

#if UNITY_2023_1_OR_NEWER
            // Awaitable into YarnTask
            AwaitableCompletionSource completionSource = new();
            completionSource.SetResult();
            Awaitable plainAwaitable = completionSource.Awaitable;
            plainYarnTask = plainAwaitable;
#endif
        }
        [Test]
        private void ContainerYarnTaskCanImplicitlyConvert()
        {
            YarnTask<int> plainYarnTask;

            // Task<T> into YarnTask<T>
            System.Threading.Tasks.Task<int> plainTask = System.Threading.Tasks.Task.FromResult(1);
            plainYarnTask = plainTask;

#if USE_UNITASK
            // UniTask<T> into YarnTask<T>
            Cysharp.Threading.Tasks.UniTask<int> plainUniTask = Cysharp.Threading.Tasks.UniTask.FromResult(1);
            plainYarnTask = plainUniTask;
#endif

#if UNITY_2023_1_OR_NEWER
            // Awaitable<T> into YarnTask<T>
            AwaitableCompletionSource<int> completionSource = new();
            completionSource.SetResult(1);
            Awaitable<int> plainAwaitable = completionSource.Awaitable;
            plainYarnTask = plainAwaitable;
#endif
        }
    }
}
