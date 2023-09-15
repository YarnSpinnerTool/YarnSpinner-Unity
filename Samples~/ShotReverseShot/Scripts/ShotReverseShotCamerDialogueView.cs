using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yarn.Unity;

namespace Yarn.Unity.Example
{
    // custom dialogue view that enables and disables different cinemachine virtual cameras associated with characters
    // as each line of dialogue comes in we get the character name from the line and then use the mapping to work out which camera to enable
    // For options we use the playerName variable to work out which camera to use for options as options rarely have a speaker name with them
    public class ShotReverseShotCamerDialogueView : DialogueViewBase
    {
        [SerializeField] private CharacterCamera[] characters;
        [SerializeField] private string playerName;

        void Start()
        {
            // we turn off all cameras by default
            foreach (var c in characters)
            {
                c.virtualCamera.SetActive(false);
            }
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            if (dialogueLine.CharacterName != null)
            {
                var disables = characters.Where(c => c.CharacterName != dialogueLine.CharacterName).Select(c => c.virtualCamera);
                var enable = characters.Where(c => c.CharacterName == dialogueLine.CharacterName).First().virtualCamera;
                if (enable == null)
                {
                    Debug.LogWarning($"Asked to show the camera for {dialogueLine.CharacterName} but there is no camera associated with that character name");
                }
                else
                {
                    enable.SetActive(true);
                    foreach (var c in disables)
                    {
                        c.SetActive(false);
                    }
                }
            }
            onDialogueLineFinished.Invoke();
        }

        public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            if (playerName == null)
            {
                Debug.LogWarning($"Attempting to set the camera for options to look at the player but playerName is null");
                return;
            }
            // we want it so that the options always show the player
            var disables = characters.Where(c => c.CharacterName != playerName).Select(c => c.virtualCamera);
            var enable = characters.Where(c => c.CharacterName == playerName).First().virtualCamera;
            if (enable == null)
            {
                Debug.LogWarning($"Attempted to show the camera for {playerName} options but there is no camera associated with that name");
                return;
            }

            enable.SetActive(true);
            foreach (var c in disables)
            {
                c.SetActive(false);
            }
        }

        public override void DialogueComplete()
        {
            foreach (var c in characters)
            {
                c.virtualCamera.SetActive(false);
            }
        }
    }

    [Serializable]
    public struct CharacterCamera
    {
        public GameObject virtualCamera;
        public string CharacterName;
    }
}
