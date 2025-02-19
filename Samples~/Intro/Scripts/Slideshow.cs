/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity.Samples
{
    public class Slideshow : DialoguePresenterBase
    {
        [SerializeField] TMP_Text? headerText;
        [SerializeField] TMP_Text? bodyText;
        [SerializeField] Image? image;

        [SerializeField] List<GameObject> projectionItems = new();
        [SerializeField] float delayBeforeShowingNewSlide = 0.5f;


        [SerializeField] List<DialoguePresenterBase> overrideDialogueViews = new();

        [SerializeField] bool startOff = true;

        bool isRunningSlideshow = false;

        private AudioSource? AudioSource => GetComponent<AudioSource>();


        protected void Awake()
        {
            ClearSlide();
            if (startOff)
            {
                foreach (var item in projectionItems)
                {
                    item.SetActive(false);
                }
            }
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            ClearSlide();
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            return YarnTask.CompletedTask;
        }

        [YarnCommand("start_slide")]
        public static YarnTask StartSlide()
        {
            var slideshow = FindAnyObjectByType<Slideshow>(FindObjectsInactive.Include);

            if (slideshow == null)
            {
                Debug.LogError($"Can't start building slide: no {typeof(Slideshow)} present in the scene!");
                return YarnTask.CompletedTask;
            }

            if (slideshow.isRunningSlideshow)
            {
                Debug.LogWarning($"start_slide called when a slide was already being built");
            }

            foreach (var overriddenView in slideshow.overrideDialogueViews)
            {
                overriddenView.enabled = false;
            }



            foreach (var element in slideshow.projectionItems)
            {
                element.SetActive(false);
            }

            if (slideshow.AudioSource != null)
            {
                slideshow.AudioSource.Play();
            }

            slideshow.isRunningSlideshow = true;

            return YarnTask.CompletedTask;
        }

        [YarnCommand("end_slide")]
        public static async YarnTask EndSlide()
        {
            var slideshow = FindAnyObjectByType<Slideshow>(FindObjectsInactive.Include);

            if (slideshow == null)
            {
                Debug.LogError($"Can't start building slide: no {typeof(Slideshow)} present in the scene!");
                return;
            }

            if (!slideshow.isRunningSlideshow)
            {
                Debug.LogWarning($"start_slide called when a slide was not being built");
            }

            if (slideshow.delayBeforeShowingNewSlide > 0)
            {
                await YarnTask.Delay(System.TimeSpan.FromSeconds(slideshow.delayBeforeShowingNewSlide));
            }

            foreach (var overriddenView in slideshow.overrideDialogueViews)
            {
                overriddenView.enabled = true;
            }

            foreach (var element in slideshow.projectionItems)
            {
                element.SetActive(true);
            }

            slideshow.isRunningSlideshow = false;
        }

        [YarnCommand("clear_slide")]
        public static void ClearSlide()
        {
            var slideshow = FindAnyObjectByType<Slideshow>(FindObjectsInactive.Include);

            if (slideshow == null)
            {
                Debug.LogError($"Can't clear slideshow: no {typeof(Slideshow)} present in the scene!");
                return;
            }

            slideshow.SetHeader(null);
            slideshow.SetBody(null);
            slideshow.SetImage(null);
        }

        public void SetHeader(string? text)
        {
            if (headerText != null)
            {
                headerText.gameObject.SetActive(text != null);
                headerText.text = text;
            }
        }

        public void SetBody(string? text)
        {
            if (text != null)
            {
                SetImage(null);
            }

            if (bodyText != null)
            {
                bodyText.text = text ?? string.Empty;
                bodyText.gameObject.SetActive(true);
            }
        }

        public void SetImage(Sprite? sprite)
        {
            if (sprite != null)
            {
                SetBody(null);
            }

            if (image != null)
            {
                image.gameObject.SetActive(sprite != null);
                image.sprite = sprite;
            }
        }

        public void AddBullet(string text)
        {
            if (bodyText != null)
            {
                string result = bodyText.text;
                if (text.Length > 0)
                {

                    result = $"{result}\n• {text}";
                }
                else
                {
                    result = $"• {text}";
                }

                SetBody(result);
            }
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (string.IsNullOrEmpty(line.CharacterName) || !isRunningSlideshow)
            {
                return YarnTask.CompletedTask;
            }

            var text = line.TextWithoutCharacterName.Text;

            switch (line.CharacterName)
            {
                case "SlideHeader":
                    SetHeader(text);
                    break;
                case "SlideBullet":
                    AddBullet(text);
                    break;
                case "SlideImage":
                    var image = Resources.Load<Sprite>(text);
                    if (image == null)
                    {
                        Debug.LogWarning($@"Failed to find sprite ""{text}""");
                    }
                    SetImage(image);
                    break;
            }

            return YarnTask.CompletedTask;
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            return YarnTask.FromResult<DialogueOption?>(null);
        }
    }
}