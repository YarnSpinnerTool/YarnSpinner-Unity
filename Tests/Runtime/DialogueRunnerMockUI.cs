using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace Yarn.Unity.Tests
{
    public class DialogueRunnerMockUI : Yarn.Unity.DialogueViewBase
    {
        // The text of the most recently received line that we've been
        // given
        public string CurrentLine { get; private set; } = default;

        // The text of the most recently received options that we've ben
        // given
        public List<string> CurrentOptions { get; private set; } = new List<string>();

        // The amount of time that this view will take before notifying
        // that the line has been delivered. If zero, lines are delivered
        // immediately.
        public float simulatedLineDeliveryTime = 0;

        // The amount of time that this view will take before notifying
        // that the line has been dismissed. If zero, lines are dismissed
        // immediately.
        public float simulatedLineDismissalTime = 0;

        // If true, the line has been interrupted, and we should notify
        // that delivery is complete immediately.
        private bool isLineInterrupted;

        public override void RunLine(LocalizedLine dialogueLine, Action onLineDeliveryComplete)
        {
            // Store the localised text in our CurrentLine property and
            // signal that we're done "delivering" the line after the
            // correct amount of time
            CurrentLine = dialogueLine.RawText;

            isLineInterrupted = false;

            if (simulatedLineDeliveryTime > 0)
            {
                StartCoroutine(SimulateLineDelivery(onLineDeliveryComplete));
            }
            else
            {
                onLineDeliveryComplete();
            }

        }

        private IEnumerator SimulateLineDelivery(Action onLineDeliveryComplete)
        {
            // Wait for an amount of time before calling the completion
            // handler
            var lineDeliveryTimeRemaining = simulatedLineDeliveryTime;

            while (lineDeliveryTimeRemaining > 0)
            {
                if (isLineInterrupted)
                {
                    break;
                }
                yield return null;
            }

            onLineDeliveryComplete();
        }

        public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            CurrentOptions.Clear();
            foreach (var option in dialogueOptions)
            {
                CurrentOptions.Add(option.TextLocalized);
            }
        }

        public override void DismissLine(Action onDismissalComplete)
        {
            // Signal that we're done "dismissing" the line after the
            // correct amount of time

            if (simulatedLineDeliveryTime > 0)
            {
                StartCoroutine(SimulateLineDismissal(onDismissalComplete));
            }
            else
            {
                onDismissalComplete();
            }
        }

        private IEnumerator SimulateLineDismissal(Action onDismissalComplete)
        {
            yield return new WaitForSeconds(simulatedLineDismissalTime);
            onDismissalComplete();
        }

        public override void OnLineStatusChanged(LocalizedLine dialogueLine)
        {
            switch (dialogueLine.Status)
            {
                case LineStatus.Interrupted:
                    isLineInterrupted = true;
                    break;
                default:
                    // no-op; we don't care about other states in this mock
                    // view
                    break;
            }
        }
    }
}
