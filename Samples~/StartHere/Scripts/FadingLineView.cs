using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.UI;

namespace Yarn.Unity.Example
{
    /// <summary>
    /// A dialogue view that presents lines in a <see cref="Text"/>
    /// component, and options in a vertical list. The text fades in from
    /// transparency when delivering a line, and fades out when the line is
    /// complete. Each character can have a custom colour to use. In
    /// addition, pressing the Spacebar key will skip the line.
    /// </summary>
    public class FadingLineView : Yarn.Unity.DialogueViewBase
    {
        /// <summary>
        /// Stores information about what color to use for a character's
        /// lines.
        /// </summary>
        /// <seealso cref="characters"/>
        /// <seealso cref="GetColorForCharacter(string)"/>
        [Serializable]
        public struct Character
        {
            /// <summary>
            /// The name of the character.
            /// </summary>
            public string name;

            /// <summary>
            /// The colour to use when presenting lines from this
            /// character.
            /// </summary>
            public Color color;
        }

        [SerializeField] CanvasGroup contentContainer = null;
        [SerializeField] Text lineText = null;

        [SerializeField] GameObject optionButtonTemplate = null;
        [SerializeField] RectTransform optionContainer = null;

        [SerializeField] float fadeTime = 0.25f;

        [SerializeField] List<Character> characters = new List<Character>();
        [SerializeField] Color optionsColor = Color.white;
        [SerializeField] Color defaultColor = Color.white;

        private bool interrupt = false;
        private List<GameObject> currentOptionButtons = new List<GameObject>();

        /// <summary>
        /// Returns a Color to use for a line, based on the name of the
        /// character. 
        /// </summary>
        /// <remarks>
        /// The color is looked up from the list specified in <see
        /// cref="characters"/>. If <paramref name="name"/> does not refer
        /// to a known character, <see cref="defaultColor"/> is returned.
        /// </remarks>
        /// <param name="name">The name of the character to look up a
        /// character for.</param>
        /// <returns>The color to use.</returns>
        public Color GetColorForCharacter(string name)
        {
            foreach (var character in characters)
            {
                if (character.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return character.color;
                }
            }

            // We don't know what colour to use, so go with the default
            // color
            return defaultColor;
        }

        /// <summary>
        /// A coroutine that fades the <see cref="contentContainer"/>
        /// object's opacity from <paramref name="from"/> to <paramref
        /// name="to"/> over the course of <see cref="fadeTime"/> seconds,
        /// and then invokes onComplete.
        /// </summary>
        /// <param name="from">The opacity value to start fading
        /// from.</param>
        /// <param name="to">The opacity value to end fading at.</param>
        /// <param name="onComplete">A delegate to invoke after fading is
        /// complete.</param>
        IEnumerator FadeContent(float from, float to, Action onComplete = null)
        {
            contentContainer.alpha = from;

            var timeElapsed = 0f;

            while (timeElapsed < fadeTime && interrupt == false)
            {
                var fraction = timeElapsed / fadeTime;
                timeElapsed += Time.deltaTime;

                float a = Mathf.Lerp(from, to, fraction);

                contentContainer.alpha = a;
                yield return null;
            }

            contentContainer.alpha = to;

            onComplete?.Invoke();
        }

        public override void DismissLine(Action onDismissalComplete)
        {
            contentContainer.alpha = 0;
            lineText.gameObject.SetActive(false);
            onDismissalComplete();
        }

        public override void OnLineStatusChanged(LocalizedLine dialogueLine)
        {
            switch (dialogueLine.Status)
            {
                case LineStatus.Running:
                    break;
                case LineStatus.Interrupted:
                    interrupt = true;
                    break;
                case LineStatus.Delivered:
                    break;
                case LineStatus.Ended:
                    break;
            }
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            lineText.gameObject.SetActive(true);
            contentContainer.gameObject.SetActive(true);

            interrupt = false;

            lineText.text = dialogueLine.TextWithoutCharacterName.Text;

            string characterName = dialogueLine.CharacterName;

            if (characterName != null)
            {
                lineText.color = GetColorForCharacter(characterName);
            }

            StartCoroutine(FadeContent(0, 1, onDialogueLineFinished));
        }

        public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            currentOptionButtons.Clear();
            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var option = dialogueOptions[i];

                var newOption = Instantiate(optionButtonTemplate);
                newOption.SetActive(true);
                newOption.transform.SetParent(optionContainer, false);

                var button = newOption.GetComponentInChildren<Button>();
                button.onClick.AddListener(() => OptionButtonSelected(option.DialogueOptionID, onOptionSelected));

                var text = newOption.GetComponentInChildren<Text>();
                text.text = option.Line.TextWithoutCharacterName.Text;

                text.color = optionsColor;

                currentOptionButtons.Add(newOption);
            }

            StartCoroutine(FadeContent(0, 1));
        }

        public void OptionButtonSelected(int optionID, Action<int> optionSelectedCallback)
        {
            foreach (var button in currentOptionButtons)
            {
                Destroy(button);
            }

            currentOptionButtons.Clear();

            optionSelectedCallback(optionID);
        }

        private void Start()
        {
            contentContainer.gameObject.SetActive(false);

            lineText.gameObject.SetActive(false);
            optionButtonTemplate.SetActive(false);
        }

        void Update()
        {
            // If the user presses the spacebar, signal that we want to
            // interrupt the line.
            if (Input.GetKeyDown(KeyCode.Space))
            {
                MarkLineComplete();
            }
        }
    }
}
