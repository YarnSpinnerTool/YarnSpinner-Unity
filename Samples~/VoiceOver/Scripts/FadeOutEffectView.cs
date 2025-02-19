/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using UnityEngine.UI;

namespace Yarn.Unity.Samples
{
    public class FadeOutEffectView : MonoBehaviour
    {
        Image Image => GetComponent<Image>();

        protected void Awake()
        {
            Image.color = Color.clear;
        }

        [YarnCommand("set_fade_color")]
        public static YarnTask SetFadeColor(float opacity)
        {
            return Fade(opacity, opacity, 0);
        }

        [YarnCommand("fade_up")]
        public static YarnTask FadeUp(float duration = 1f, bool wait = false)
        {
            var task = Fade(1, 0, duration);
            return wait ? task : YarnTask.CompletedTask;
        }

        [YarnCommand("fade_down")]
        public static YarnTask FadeDown(float duration = 1f, bool wait = false)
        {
            var task = Fade(0, 1, duration);
            return wait ? task : YarnTask.CompletedTask;
        }

        public static async YarnTask Fade(float from, float to, float duration)
        {
            var view = FindAnyObjectByType<FadeOutEffectView>(FindObjectsInactive.Exclude);
            if (view == null)
            {
                Debug.LogError($"Can't fade: no active fade view in the scene!");
                return;
            }

            Color color = Color.black;
            var endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                var t = 1.0f - (endTime - Time.time) / duration;
                color.a = Mathf.Lerp(from, to, t);
                view.Image.color = color;
                await YarnTask.Yield();
            }
            color.a = to;
            view.Image.color = color;
        }
    }
}