/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = Yarn.Unity.TMPShim;
#endif

using System.Threading;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// Provides common effects used by various <see cref="DialoguePresenterBase"/> implementations.
    /// </summary>
    public static class Effects
    {
        /// <inheritdoc cref="FadeAlphaAsync(CanvasGroup, float, float, float, CancellationToken)">
        public static IEnumerator FadeAlpha(CanvasGroup canvas, float from, float to, float duration, CancellationToken token = default)
        {
            return YarnTask.ToCoroutine(() => FadeAlphaAsync(canvas, from, to, duration, token));
        }

        /// <summary>
        /// Fades a canvas group alpha value.
        /// Generally used to fade up and down the main dialogue canvas when dialogue starts and ends.
        /// </summary>
        /// <param name="canvas">The canvas group to fade</param>
        /// <param name="from">The starting value of the fade</param>
        /// <param name="to">The end value of the fade</param>
        /// <param name="duration">How long the fade should take</param>
        /// <param name="token">Cancellation token for this task</param>
        /// <returns></returns>
        public static async YarnTask FadeAlphaAsync(CanvasGroup canvas, float from, float to, float duration, CancellationToken token)
        {
            if (duration == 0)
            {
                canvas.alpha = to;
                return;
            }

            canvas.alpha = from;

            float accumulator = 0;
            while (!token.IsCancellationRequested && accumulator < duration)
            {
                accumulator += Time.deltaTime;
                canvas.alpha = Mathf.Lerp(from, to, accumulator / duration);
                await YarnTask.Yield();
            }

            canvas.alpha = to;
        }
    }
}
