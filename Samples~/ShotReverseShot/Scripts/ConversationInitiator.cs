using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

namespace Yarn.Unity.Example
{
    // Super basic class to start the conversation
    // Reason being we don't want to start the conversation straight away
    // because that will mean you don't get to see the first camera transition
    public class ConversationInitiator : MonoBehaviour
    {
        // a reference to the "press e to start" label
        [SerializeField] GameObject helpOverlay;

        // Update is called once per frame
        void Update()
        {
            var runner = FindObjectOfType<DialogueRunner>();
            if (runner != null)
            {
                if (Input.GetKeyUp(KeyCode.E))
                {
                    if (!runner.IsDialogueRunning)
                    {
                        helpOverlay.SetActive(false);
                        runner.StartDialogue("RedLizardFriend");
                    }
                }
            }
        }
    }
}
