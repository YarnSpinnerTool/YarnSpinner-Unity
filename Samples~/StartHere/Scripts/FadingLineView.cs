using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.UI;

namespace Yarn.Unity.Example
{
    public class FadingLineView : Yarn.Unity.DialogueViewBase
    {
        [System.Serializable]
        public struct Character {
            public string name;
            public Color color;
        }

        [SerializeField] CanvasGroup contentContainer;
        [SerializeField] Text lineText;

        [SerializeField] float fadeTime = 0.25f;

        bool interrupt = false;

        [SerializeField] GameObject optionButtonTemplate;
        [SerializeField] RectTransform optionContainer;

        List<GameObject> currentOptionButtons = new List<GameObject>();

        [SerializeField] List<Character> characters = new List<Character>();
        [SerializeField] Color optionsColor = Color.white;

        Color GetColorForCharacter(string name) {
            foreach (var character in characters) {
                if (character.name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) {
                    return character.color;
                }
            }
            Debug.LogWarning($"Unknown character {name}");
            return Color.white;
        }

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

            if (characterName != null) {
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

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                MarkLineComplete();
            }
        }
    }
}
