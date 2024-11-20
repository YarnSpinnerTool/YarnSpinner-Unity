/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Tests
{
    using System;
    using UnityEngine;

    using static EndToEndUtility;

    /// <summary>
    /// RunnerExecution is a class that represents a dialogue session. When the
    /// object is created, it begins running a given node on the first
    /// DialogueRunner in the scene. When the object is disposed, it waits for
    /// the DialogueRunner to not be running, and throws an exception on
    /// timeout. It's intended to be used inside <see cref="EndToEndTests"/>,
    /// with the <see langword="using"/> syntax.
    /// </summary>
    class RunnerExecution : IAsyncDisposable
    {
        /// <summary>
        /// Starts dialogue execution on the first available DialogueRunner, and
        /// returns a RunnerExecution object that represents the run. When the
        /// object is disposed, the dialogue runner is expected to finish
        /// execution within a timeout.
        /// </summary>
        /// <param name="nodeName"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public static RunnerExecution StartDialogue(string nodeName, int timeoutMilliseconds = 2000)
        {
            return new RunnerExecution(nodeName, timeoutMilliseconds);
        }

        DialogueRunner dialogueRunner;
        int timeoutMilliseconds;

        YarnTaskCompletionSource dialogueCompletionsource = new();

        private RunnerExecution(string nodeName, int timeoutMilliseconds)
        {
            this.timeoutMilliseconds = timeoutMilliseconds;
            dialogueRunner = GameObject.FindAnyObjectByType<DialogueRunner>();
            dialogueRunner.StartDialogue(nodeName);

            async YarnTask CompleteTaskWhenDialogueDone()
            {
                await YarnTask.WaitUntil(() => dialogueRunner);
                dialogueCompletionsource.TrySetResult();
            }

            CompleteTaskWhenDialogueDone().Forget();
        }

        private YarnTask CompletionTask => dialogueCompletionsource.Task;

        public async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            await WaitForTaskAsync(CompletionTask, "Expected dialogue to be finished", timeoutMilliseconds);
        }
    }
}
