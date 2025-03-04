/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Threading;
using UnityEngine;
using Yarn.Unity;

#nullable enable

namespace Yarn.Unity.Samples
{
    public class TimeoutBar : MonoBehaviour
    {
        public float duration = 1f;
        [SerializeField] RectTransform? bar;

        private float originalSize = 0f;
        public void Start()
        {
            if (bar != null)
            {
                originalSize = bar.sizeDelta.x;
            }
        }

        public async YarnTask Shrink(CancellationToken cancellationToken)
        {
            if (bar == null)
            {
                return;
            }

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
            if (bar != null)
            {
                bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, originalSize);
            }
        }
    }
}
