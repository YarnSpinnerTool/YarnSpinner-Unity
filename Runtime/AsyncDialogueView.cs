using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Unity;

#if USE_UNITASK
using YarnTask = Cysharp.Threading.Tasks.UniTask;
using YarnOptionTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.DialogueOption>;
using YarnIntTask = Cysharp.Threading.Tasks.UniTask<int>;
#else
using YarnTask = System.Threading.Tasks.Task;
using YarnOptionTask = System.Threading.Tasks.Task<Yarn.Unity.DialogueOption>;
using YarnIntTask = System.Threading.Tasks.Task<int>;
#endif

#nullable enable

namespace Yarn.Unity
{
    public class AsyncDialogueView : AsyncDialogueViewBase
    {
        [SerializeField] bool autoContinue;
        [SerializeField] bool showUnavailableOptions;

        [Header("Debug")]
        [SerializeField] float debugDeliveryDuration = 0.25f;
        [SerializeField] bool debugHandlesOptions = true;

        [SerializeField] AudioSource audioSource;

        [SerializeField] TMP_Text lineText;

        public override async YarnTask RunLineAsync(LocalizedLine line, CancellationToken token)
        {
            var text = line.Text.Text;
            Debug.Log("Line: " + text);

            lineText.text = line.Text.Text;

            var asset = line.Asset;
            if (asset is AudioClip clip)
            {
                audioSource.PlayOneShot(clip);

                // Wait for playback to start.
                while (!audioSource.isPlaying)
                {
                    if (token.IsCancellationRequested)
                    {
                        // The line was cancelled. Stop waiting.
                        break;
                    }
                    await YarnTask.Yield();
                }

                // Wait for playback to end.
                while (audioSource.isPlaying)
                {
                    if (token.IsCancellationRequested)
                    {
                        // The line was cancelled. Stop playback.
                        audioSource.Stop();
                    }
                    await YarnTask.Yield();
                }
            }

            if (token.IsCancellationRequested == false)
            {
                try
                {
                    await YarnTask.Delay((int)(debugDeliveryDuration * 1000), cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    // We were cancelled during the delay. We can ignore this
                    // exception.
                }
            }

            if (!autoContinue)
            {
                // We're not automatically continuing.
                // Wait until the line is cancelled.
                await YarnAsync.WaitUntilCanceled(token);
            }

            lineText.text = "";
        }

        public override async YarnOptionTask RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            if (debugHandlesOptions == false)
            {
                return null;
            }

            var sb = new StringBuilder();

            var displayedOptions = new List<DialogueOption>();
            foreach (var opt in dialogueOptions)
            {
                if (!opt.IsAvailable && !showUnavailableOptions)
                {
                    // The option isn't available, and we're not showing unavailable
                    // options. Don't show this option.
                    continue;
                }
                displayedOptions.Add(opt);
                sb.AppendLine($"{displayedOptions.Count}: {opt.Line.Text.Text}");
                Debug.Log($"Option {displayedOptions.Count}: {opt.Line.Text.Text}");
            }

            lineText.text = sb.ToString();

            do
            {
                // Wait for the user to hit a key
                var inputNumber = await GetKeyboardNumberAsync(cancellationToken) - 1;

                if (cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (inputNumber >= displayedOptions.Count)
                {
                    // Not a valid option.
                    continue;
                }

                var opt = displayedOptions[inputNumber];

                if (!opt.IsAvailable)
                {
                    // The option isn't available. Don't allow selecting it.
                    continue;
                }

                lineText.text = "";
                return opt;
            } while (true);
        }

        private async YarnIntTask GetKeyboardNumberAsync(CancellationToken cancellationToken)
        {
            var dict = new Dictionary<KeyCode, int> {
            { KeyCode.Alpha0, 0 },
            { KeyCode.Alpha1, 1 },
            { KeyCode.Alpha2, 2 },
            { KeyCode.Alpha3, 3 },
            { KeyCode.Alpha4, 4 },
            { KeyCode.Alpha5, 5 },
            { KeyCode.Alpha6, 6 },
            { KeyCode.Alpha7, 7 },
            { KeyCode.Alpha8, 8 },
            { KeyCode.Alpha9, 9 },
        };
            while (cancellationToken.IsCancellationRequested == false)
            {
                foreach (var kv in dict)
                {
                    if (Input.GetKeyDown(kv.Key))
                    {
                        return kv.Value;
                    }
                }
                await YarnTask.Yield();
            }
            return default;
        }
    }
}
