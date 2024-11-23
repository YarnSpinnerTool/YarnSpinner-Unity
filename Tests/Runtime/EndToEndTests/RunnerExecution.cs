/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

namespace Yarn.Unity.Tests
{
    using UnityEngine;

    /// <summary>
    /// Contains helper methods for tests that need to assert the behaviour and
    /// sequence of content in a dialogue session.
    /// </summary>
    static class RunnerExecution
    {
        /// <summary>
        /// Starts dialogue execution on the first available DialogueRunner, and
        /// then runs the task returned by <paramref name="expectation"/>, then
        /// waits for the dialogue to complete.
        /// </summary>
        /// <param name="nodeName">The node to start running.</param>
        /// <param name="expectation">A function that returns a <see
        /// cref="YarnTask"/> that will execute after dialogue has started, and
        /// the dialogue is expected to complete afterward.</param>
        /// <param name="timeoutMilliseconds">The amount of time to wait for the
        /// Dialogue Runner to complete after expectation's task.
        /// completes.</param>
        /// <returns>A <see cref="YarnTask"/> that completes after the dialogue
        /// completes.</returns>
        public async static YarnTask StartDialogue(string nodeName, System.Func<YarnTask> expectation, int timeoutMilliseconds = 2000)
        {
            // Find a dialogue runner and start running it
            var dialogueRunner = GameObject.FindAnyObjectByType<DialogueRunner>();
            dialogueRunner.Should().NotBeNull();
            dialogueRunner.StartDialogue(nodeName);
            dialogueRunner.IsDialogueRunning.Should().BeTrue();

            // Run the set of expectations
            await expectation();

            // Now that we're done, expect the dialogue to finish
            await dialogueRunner.DialogueTask.WithTimeout("Expected dialogue to be finished", timeoutMilliseconds);
        }
    }
}
