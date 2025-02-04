/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Yarn.Unity.Tests
{
    public static class EndToEndUtility
    {
        private const int DelayBetweenInteractions = 10;

        private static GameObject GetObject(string objectName)
        {
            var allObjects = GameObject.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name == objectName)
                {
                    return obj;
                }
            }

            throw new System.NullReferenceException($"Failed to find a game object named {objectName}");
        }

        public static async YarnTask MoveToMarkerAsync(string moverName, string markerName, float speed = 20f, float distance = 2f)
        {
            var mover = GetObject(moverName);
            var target = GetObject(markerName);

            while ((mover.transform.position - target.transform.position).magnitude > distance)
            {
                var newPosition = Vector3.MoveTowards(mover.transform.position, target.transform.position, speed * Time.deltaTime);
                mover.transform.position = newPosition;
                await YarnTask.Yield();
            }
        }

        [YarnCommand("move_to")]
        public static async YarnTask MoveToMarker(string moverName,
            string markerName, float speed = 20f, float distance = 2f)
        {
            await MoveToMarkerAsync(moverName, markerName, speed, distance);
        }

        public static async YarnTask RunDialogueAsync(string nodeName)
        {
            var dialogueRunner = GameObject.FindAnyObjectByType<DialogueRunner>();
            dialogueRunner.StartDialogue(nodeName);
            dialogueRunner.IsDialogueRunning.Should().BeTrue();

            await dialogueRunner.DialogueTask;
        }

        public static async YarnTask WaitForLineAsync(string lineText, int timeoutMilliseconds = 3000)
        {
            async YarnTask LineWait(CancellationToken token)
            {
                LineView? view = GameObject.FindAnyObjectByType<LineView>();
                if (view == null)
                {
                    throw new NullReferenceException("Line view not found");
                }
                while (token.IsCancellationRequested == false)
                {
                    if (view.lineText.text == lineText && view.continueButton.activeInHierarchy)
                    {
                        // Continue to the next piece of content
                        view.OnContinueClicked();
                        Debug.Log($"Successfully saw line " + lineText);
                        await YarnTask.Delay(DelayBetweenInteractions);
                        return;
                    }
                    await YarnTask.Yield();
                }
            }
            var cts = new CancellationTokenSource();

            await LineWait(cts.Token)
                .WithTimeout(
                    failureMessage: $"Failed to see line \"{lineText}\" within {timeoutMilliseconds}ms",
                    timeoutMilliseconds
                );

            cts.Cancel();
        }

        public static async YarnTask WaitForOptionAndSelectAsync(string optionText, int timeoutMilliseconds = 2000)
        {
            async YarnTask OptionsWait(CancellationToken token)
            {
                OptionsListView? view = GameObject.FindAnyObjectByType<OptionsListView>();
                if (view == null)
                {
                    throw new NullReferenceException("Options view not found");
                }
                while (token.IsCancellationRequested == false)
                {
                    var optionsListViews = view.GetComponentsInChildren<OptionView>(includeInactive: false);
                    foreach (var v in optionsListViews)
                    {
                        // The buttons may not be interactable yet, so don't try
                        // to use it if it's not
                        if (!v.IsInteractable())
                        {
                            continue;
                        }

                        var text = v.GetComponentInChildren<TMPro.TMP_Text>();
                        if (text.text == optionText)
                        {
                            // This is the option!
                            v.InvokeOptionSelected();
                            Debug.Log($"Successfully selected option " + optionText);
                            await YarnTask.Delay(DelayBetweenInteractions);
                            return;
                        }
                    }

                    await YarnTask.Yield();
                }
            }

            var cts = new CancellationTokenSource();

            await OptionsWait(cts.Token)
                .WithTimeout(
                    failureMessage: $"Failed to see option \"{optionText}\" within ${timeoutMilliseconds}ms",
                    timeoutMilliseconds
                );

            cts.Cancel();
        }
    }

    static class YarnTaskExtensions
    {
        public static async YarnTask WithTimeout(this YarnTask task, string? failureMessage = null, int timeoutMilliseconds = 2000)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // If a debugger is attached, don't do timeouts, because the act
                // of debugging will likely make us take longer than the timeout
                // and we'd (potentially spuriously) fail.
                await task;
                return;
            }

            // A completion source that represents whether the task timed out
            // (true), or didn't (false).
            YarnTaskCompletionSource<bool> taskCompletionSource = new();

            // A cancellation source used to cancel the timeout if the task
            // completes in time.
            CancellationTokenSource cts = new();

            try
            {
                async YarnTask RunTask()
                {
                    // Run the task, and then attempt to report that we did not
                    // time out (and we should stop waiting to see if we did).
                    await task;

                    taskCompletionSource.TrySetResult(false);
                    cts.Cancel();
                }

                async YarnTask Timeout()
                {
                    // Wait the allotted time, and then attempt to report that
                    // we timed out; stop the timer early if the task completes
                    // before this wait does.
                    try
                    {
                        await YarnTask.Delay(timeoutMilliseconds, cts.Token);

                        taskCompletionSource.TrySetResult(true);
                    }
                    catch (OperationCanceledException)
                    {
                        // Our delay was cancelled because the main task
                        // finished. Nothing to do.
                    }
                }

                RunTask().Forget();
                Timeout().Forget();

                bool timedOut = await taskCompletionSource.Task;
                if (timedOut == true)
                {
                    // We timed out before completing the task. Throw the
                    // timeout exception.
                    throw new TimeoutException(failureMessage);
                }
            }
            catch (Exception)
            {
                // Rethrow non-timeout exceptions to our main context.
                throw;
            }
        }
    }
}
