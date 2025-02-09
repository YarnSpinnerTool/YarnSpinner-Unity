#nullable enable

namespace Yarn.Unity.Samples
{
    using UnityEngine;
    using System.Threading;

    internal static class Tweening
    {
        public static async YarnTask TweenValue(float from, float to, float duration, System.Func<float, float> easingFunction, System.Action<float> apply, CancellationToken cancellationToken)
        {
            if (duration <= 0)
            {
                apply(to);
                return;
            }

            apply(from);
            var startTime = Time.time;
            while (Time.time < (startTime + duration))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                var t = Mathf.Clamp01((Time.time - startTime) / duration);
                var easedT = easingFunction(t);

                var value = Mathf.Lerp(from, to, easedT);

                apply(value);
                await YarnTask.Yield();
            }
            apply(to);
        }
    }

    internal static class EasingFunctions
    {
        public static float InOutQuad(float t)
        {
            return t < 0.5 ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        }

        public static float Linear(float t)
        {
            return t;
        }
    }

}