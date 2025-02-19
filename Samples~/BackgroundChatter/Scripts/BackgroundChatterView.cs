/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.Samples
{

    using System.Threading;
    using System.Collections.Generic;
    using UnityEngine;
    using Yarn;
    using Yarn.Unity;
    using System;

    #if USE_TMP
    using TMPro;
    #else
    using TMP_Text = Yarn.Unity.TMPShim;
    #endif


    public class BackgroundChatterView : DialoguePresenterBase
    {
        [SerializeField] TMP_Text? text;

        [Header("Timing")]
        [SerializeField] int millisecondsPerCharacter = 75;
        [SerializeField] float minDuration = 1.5f;

        [SerializeField] float delayAfterLines = 0.5f;

        Canvas? canvas;

        Transform? attachmentTarget;


        // Update is called once per frame
        void LateUpdate()
        {
            if (canvas == null || attachmentTarget == null)
            {
                return;
            }

            Camera? camera = canvas.renderMode switch
            {
                RenderMode.ScreenSpaceCamera => canvas.worldCamera,
                RenderMode.WorldSpace => canvas.worldCamera,
                _ => null,
            };

            var screenPoint = RectTransformUtility.WorldToScreenPoint(
                Camera.main, attachmentTarget.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent.GetComponent<RectTransform>(), screenPoint, null, out var localPoint);

            transform.localPosition = localPoint;
        }

        public void Awake()
        {
            canvas = GetComponentInParent<Canvas>();

            if (text != null)
            {
                text.enabled = false;
            }
        }


        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (text == null)
            {
                return;
            }

            var characterName = line.CharacterName;

            if (characterName == null)
            {
                Debug.LogWarning($"Line {line.TextID} (\"{line.Text.Text}\") has no character name", this);
                return;
            }

            var lineText = line.TextWithoutCharacterName.Text;

            var npc = ChatterNPC.FindByName(characterName);

            if (npc == null)
            {
                Debug.LogWarning($"Failed to find an NPC for {line.TextID} (\"{line.Text.Text}\")", this);
                return;
            }

            attachmentTarget = npc.BackgroundChatterPoint;

            text.text = lineText;

            var durationMilliseconds = Mathf.Max(lineText.Length * millisecondsPerCharacter, this.minDuration * 1000);

            try
            {
                text.enabled = true;
                await YarnTask.Delay(TimeSpan.FromMilliseconds(durationMilliseconds), token.NextLineToken);
                text.enabled = false;
                await YarnTask.Delay(TimeSpan.FromMilliseconds(this.delayAfterLines), token.NextLineToken);
            }
            catch (System.OperationCanceledException)
            {
                // Line cancelled, nothing to do
            }
            finally
            {
                text.enabled = false;
                attachmentTarget = null;
            }
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException($"Background chatter does not support options");
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            // No action required when background dialogue starts
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            // Ensure that our on-screen text isn't visible (the line-runner should have already done this, but just in case)
            if (text != null)
            {
                text.enabled = false;
            }

            attachmentTarget = null;

            return YarnTask.CompletedTask;
        }
    }
}
