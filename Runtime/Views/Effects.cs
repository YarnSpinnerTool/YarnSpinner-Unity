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
    public static class Effects
    {
        public static IEnumerator FadeAlpha(CanvasGroup canvas, float from, float to, float duration, CancellationToken token)
        {
            return YarnTask.ToCoroutine(() => FadeAlphaAsync(canvas, from, to, duration, token));
        }

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
