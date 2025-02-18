using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using Yarn.Unity;

#nullable enable

namespace Yarn.Unity.Samples
{
    public class TimeoutBar : MonoBehaviour
    {
        public RectTransform bar;
        public CancellationToken cancellationToken;
        public float duration = 1f;

        private float originalSize = 0f;
        public void Start()
        {
            originalSize = bar.sizeDelta.x;
        }

        public async YarnTask Shrink()
        {
            float accumulator = 0;
            var currentSize = bar.sizeDelta.x;

            while (accumulator < duration && !cancellationToken.IsCancellationRequested)
            {
                accumulator += Time.deltaTime;
                var newSize = Mathf.Lerp(currentSize, 0, accumulator / duration);
                bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize);
                await YarnTask.Yield();
            }
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
        }
        public void ResetBar()
        {
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalSize);
        }
    }
}
