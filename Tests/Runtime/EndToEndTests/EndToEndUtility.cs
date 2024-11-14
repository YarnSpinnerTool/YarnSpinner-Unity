/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using System;
using System.Threading;

#nullable enable
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#if USE_UNITASK
using Cysharp.Threading.Tasks;
using YarnTask = Cysharp.Threading.Tasks.UniTask;
using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.DialogueOption>;
using YarnLineTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.LocalizedLine>;
#else
using YarnTask = System.Threading.Tasks.Task;
using YarnOptionTask = System.Threading.Tasks.Task<Yarn.Unity.DialogueOption>;
using YarnLineTask = System.Threading.Tasks.Task<Yarn.Unity.LocalizedLine>;
#endif

namespace Yarn.Unity.Tests
{
    public static class EndToEndUtility
    {
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

            while (dialogueRunner.IsDialogueRunning)
            {
                await YarnTask.Yield();
            }
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
                    if (view.lineText.text == lineText)
                    {
                        Debug.Log($"Successfully saw line " + lineText);
                        await YarnTask.Delay(500);
                        // Continue to the next piece of content
                        view.OnContinueClicked();
                        return;
                    }
                    await YarnTask.Yield();
                }
            }
            var cts = new CancellationTokenSource();

            await WaitForTaskAsync(
                LineWait(cts.Token),
                failureMessage: $"Failed to see line \"{lineText}\" within ${timeoutMilliseconds}ms",
                timeoutMilliseconds);

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
                        var text = v.GetComponentInChildren<TMPro.TMP_Text>();
                        if (text.text == optionText)
                        {
                            await YarnTask.Delay(500);
                            // This is the option!
                            v.InvokeOptionSelected();
                            return;
                        }
                    }

                    await YarnTask.Yield();
                }
            }

            var cts = new CancellationTokenSource();

            await WaitForTaskAsync(
                OptionsWait(cts.Token),
                failureMessage: $"Failed to see option \"{optionText}\" within ${timeoutMilliseconds}ms",
                timeoutMilliseconds);

            cts.Cancel();
        }

        public static async YarnTask WaitForTaskAsync(YarnTask task, string? failureMessage = null, int timeoutMilliseconds = 2000)
        {
            try
            {
                await YarnAsync.Wait(task, TimeSpan.FromMilliseconds(timeoutMilliseconds));
            }
            catch (TimeoutException timeout)
            {
                if (failureMessage == null)
                {
                    throw;
                }
                else
                {
                    throw new TimeoutException(failureMessage, timeout);
                }
            }
            catch (Exception)
            {
                // Rethrow non-timeout exceptions to our main context
                throw;
            }
        }
    }

}
