using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Yarn.Unity.Example {
    /// <summary>
    /// clones dialogue bubbles for the ChatDialogue example
    /// </summary>
    public class PhoneChatDialogueHelper : MonoBehaviour
    {
        DialogueRunner runner;

        [Tooltip("This is the chat message bubble UI object (what we are cloning for each message!)... NOT the container group for all chat bubbles")]
        public GameObject dialogueBubblePrefab;
        bool isFirstMessage = true;

        // current message bubble styling settings, modified by SetSender
        bool isRightAlignment = true;
        Color currentBGColor = Color.black, currentTextColor = Color.white;

        void Awake() {
            runner = GetComponent<DialogueRunner>();
            runner.AddCommandHandler( "Me", SetSenderMe ); // registers Yarn Command <<Me>>, which sets the current message sender to "Me"
            runner.AddCommandHandler( "Them", SetSenderThem ); // registers Yarn Command <<They>>, which sets the current message sender to "Them" (whoever the player is talking to)
        }

        void Start () {
            dialogueBubblePrefab.SetActive(false);
            UpdateMessageBoxSettings();
        }

        // YarnCommand <<Me>>, but does not use YarnCommand C# attribute, registers in Awake() instead
        public void SetSenderMe() {
            isRightAlignment = true;
            currentBGColor = Color.blue;
            currentTextColor = Color.white;
        }

        // YarnCommand <<Them>> does not use YarnCommand C# attribute, registers in Awake() instead
        public void SetSenderThem() {
            isRightAlignment = false;
            currentBGColor = Color.white;
            currentTextColor = Color.black;
        }

        // when we clone a new message box, re-style the message box based on whether SetSenderMe or SetSenderThem was most recently called
        void UpdateMessageBoxSettings() {
            var bg = dialogueBubblePrefab.GetComponentInChildren<Image>();
            bg.color = currentBGColor;
            var message = dialogueBubblePrefab.GetComponentInChildren<Text>();
            message.text = "";
            message.color = currentTextColor;

            var layoutGroup = dialogueBubblePrefab.GetComponent<HorizontalLayoutGroup>();
            if ( isRightAlignment ) {
                layoutGroup.padding.left = 32;
                layoutGroup.padding.right = 0;
                bg.transform.SetAsLastSibling();
            } else {
                layoutGroup.padding.left = 0;
                layoutGroup.padding.right = 32;
                bg.transform.SetAsFirstSibling();
            }
        }

        public void CloneMessageBoxToHistory() {
            // if this isn't the very first message, then clone current message box and move it up
            if ( isFirstMessage == false ) {
                var oldClone = Instantiate( 
                    dialogueBubblePrefab, 
                    dialogueBubblePrefab.transform.position, 
                    dialogueBubblePrefab.transform.rotation, 
                    dialogueBubblePrefab.transform.parent
                );
                dialogueBubblePrefab.transform.SetAsLastSibling();
            }
            isFirstMessage = false;

            // reset message box and configure based on current settings
            dialogueBubblePrefab.SetActive(true);
            UpdateMessageBoxSettings();
        }


    }

}
