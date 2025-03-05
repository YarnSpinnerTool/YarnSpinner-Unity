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
        [SerializeField] string? characterName;

        [SerializeField] new Renderer? renderer;
        [SerializeField] VoiceOverPresenter? voiceOverView;
        [SerializeField] TMP_Text? debugView;

        private MaterialPropertyBlock? propertyBlock;

        LipSyncTextureGroup? currentFacialExpression;

        [SerializeField] SerializableDictionary<string, LipSyncTextureGroup> facialExpressions = new();
        [SerializeField] LipSyncTextureGroup? defaultFacialExpression;

        [YarnCommand("expression")]
        public void SetFacialExpression(string expression)
        {
            if (!facialExpressions.TryGetValue(expression, out currentFacialExpression))
            {
                Debug.LogError($"Unknown facial expression {expression}; expected {string.Join(", ", facialExpressions.Keys)}", this);
            }
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            return YarnTask.CompletedTask;
        }

        public void Awake()
        {
            propertyBlock = new();

            currentFacialExpression = defaultFacialExpression;

            if (debugView != null)
            {
                debugView.gameObject.SetActive(false);
            }
            SetMouthShape(LipSyncedVoiceLine.MouthShape.X);
        }

        public void SetMouthShape(LipSyncedVoiceLine.MouthShape mouthShape)
        {
            if (renderer == null)
            {
                return;
            }

            if (currentFacialExpression == null)
            {
                Debug.LogWarning($"No facial expression set", this);
                return;
            }

            if (currentFacialExpression.TryGetTexture(mouthShape, out var texture))
            {
                if (propertyBlock == null)
                {
                    propertyBlock = new MaterialPropertyBlock();
                }

                propertyBlock.SetTexture("_Texture", texture);

                renderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                Debug.LogWarning($"No mouth shape {mouthShape}", this);
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
            if (line.CharacterName != characterName)
            {
                // Not our character; nothing to do.
                return;
            }

            if (!(line.Asset is IAssetProvider provider && provider.TryGetAsset<LipSyncedVoiceLine>(out var data)))
            {
                Debug.LogWarning($"No lipsync data for line {line.TextID}", this);
                return;
            }

            if (renderer == null)
            {
                Debug.LogWarning($"No renderer for lipsync view {this.name}", this);
                return;
            }

            var delayBeforeStart = (voiceOverView != null) ? voiceOverView.waitTimeBeforeLineStart : 0f;

            SetMouthShape(LipSyncedVoiceLine.MouthShape.X);

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

                SetMouthShape(frame.mouthShape);

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
            SetMouthShape(LipSyncedVoiceLine.MouthShape.X);

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
