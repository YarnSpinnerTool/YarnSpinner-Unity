using System;
using System.Collections.Generic;
using UnityEngine;
#if USE_TMP
using TMPro;
#else
using TextMeshProUGUI = TMPShim;
#endif

namespace Yarn.Unity
{
    public class CharacterColorView : Yarn.Unity.DialogueViewBase
    {
        [Serializable]
        public class CharacterColorData
        {
            public string characterName;
            public Color displayColor = Color.white;
        }

        [SerializeField] Color defaultColor = Color.white;

        [SerializeField] CharacterColorData[] colorData;

        [SerializeField] List<TextMeshProUGUI> lineTexts = new List<TextMeshProUGUI>();

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            var characterName = dialogueLine.CharacterName;

            Color colorToUse = defaultColor;

            if (string.IsNullOrEmpty(characterName) == false) {
                foreach (var color in colorData) {
                    if (color.characterName.Equals(characterName, StringComparison.InvariantCultureIgnoreCase)) {
                        colorToUse = color.displayColor;
                        break;
                    }
                }
            }

            foreach (var text in lineTexts) {
                text.color = colorToUse;
            }

            onDialogueLineFinished();
        }
    }
}
