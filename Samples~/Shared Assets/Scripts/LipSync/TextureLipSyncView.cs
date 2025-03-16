/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using Yarn;
using Yarn.Unity;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity.Samples
{
    public class TextureLipSyncView : DialoguePresenterBase
    {

        [SerializeField] VoiceOverPresenter? voiceOverView;
        [SerializeField] TMP_Text? debugView;


        private readonly Dictionary<string, MouthView?> _cachedMouthViews = new();

        public override YarnTask OnDialogueStartedAsync()
        {
            var keys = new List<string>(_cachedMouthViews.Keys);

            foreach (var k in keys)
            {
                var v = _cachedMouthViews[k];
                if (v == null)
                {
                    _cachedMouthViews.Remove(k);
                }
            }

            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            return YarnTask.CompletedTask;
        }

        public void Awake()
        {
            if (debugView != null)
            {
                debugView.gameObject.SetActive(false);
            }

        }

        public MouthView? FindMouthView(string characterName)
        {
            if (_cachedMouthViews.TryGetValue(characterName, out var mouthView))
            {
                if (mouthView != null)
                {
                    return mouthView;
                }
                else
                {
                    _cachedMouthViews.Remove(characterName);
                    return null;
                }
            }
            else
            {
                var mouthViews = FindObjectsByType<MouthView>(FindObjectsSortMode.None);
                foreach (var mv in mouthViews)
                {
                    var mvName = string.IsNullOrEmpty(mv.CharacterName) ? mv.name : mv.CharacterName;
                    if (mvName.Equals(characterName, System.StringComparison.InvariantCulture))
                    {
                        _cachedMouthViews[characterName] = mv;
                        return mv;
                    }
                }
                return null;
            }
        }

        public static string MouthShapeToPrestonBlair(LipSyncedVoiceLine.MouthShape mouthShape)
        {
            return mouthShape switch
            {
                LipSyncedVoiceLine.MouthShape.A => "MBP",
                LipSyncedVoiceLine.MouthShape.B => "SNTK",
                LipSyncedVoiceLine.MouthShape.C => "E",
                LipSyncedVoiceLine.MouthShape.D => "AI",
                LipSyncedVoiceLine.MouthShape.E => "O",
                LipSyncedVoiceLine.MouthShape.F => "U",
                LipSyncedVoiceLine.MouthShape.G => "FV",
                LipSyncedVoiceLine.MouthShape.H => "L",
                LipSyncedVoiceLine.MouthShape.X => "rest",
                _ => "[?]",
            };
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (!(line.Asset is IAssetProvider provider && provider.TryGetAsset<LipSyncedVoiceLine>(out var data)))
            {
                Debug.LogWarning($"No lipsync data for line {line.TextID}", this);
                return;
            }

            if (string.IsNullOrEmpty(line.CharacterName))
            {
                // Line has no character name, so we don't know which mouth view
                // to use.
                return;
            }

            var targetMouthView = FindMouthView(line.CharacterName);

            if (targetMouthView == null)
            {
                // No known mouth view for this line's character
                return;
            }

            var delayBeforeStart = (voiceOverView != null) ? voiceOverView.waitTimeBeforeLineStart : 0f;

            targetMouthView.SetMouthShape(LipSyncedVoiceLine.MouthShape.X);

            if (delayBeforeStart > 0)
            {
                await YarnTask
                    .Delay(System.TimeSpan.FromSeconds(delayBeforeStart), token.HurryUpToken)
                    .SuppressCancellationThrow();
            }

            var startTime = Time.time;
            var endTime = startTime + data.Duration;

            while (Time.time < endTime && !token.IsHurryUpRequested)
            {
                var elapsed = Time.time - startTime;
                var frame = data.Evaluate(elapsed);

                targetMouthView.SetMouthShape(frame.mouthShape);

                if (debugView != null)
                {
                    debugView.gameObject.SetActive(true);
                    string debugText;
                    if (!string.IsNullOrEmpty(frame.comment))
                    {
                        debugText = $"{frame.mouthShape} ({frame.comment})";
                    }
                    else
                    {
                        debugText = $"{frame.mouthShape}";
                    }
                    debugView.text = debugText;
                }

                await YarnTask.Yield();
            }

            // Reset to the rest shape
            targetMouthView.SetMouthShape(LipSyncedVoiceLine.MouthShape.X);

            if (debugView != null)
            {
                debugView.gameObject.SetActive(false);
            }
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            // The lip sync view doesn't handle options.
            return YarnTask.FromResult<DialogueOption?>(null);
        }
    }
}
