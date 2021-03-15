/*

The MIT License (MIT)

Copyright (c) 2015-2017 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

*/

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

namespace Yarn.Unity
{
    /// <summary>
    /// Displays dialogue lines to the player, and sends user choices back
    /// to the dialogue system.
    /// </summary>
    /// <remarks>
    /// The DialogueUI component works closely with the <see
    /// cref="DialogueRunner"/> class. It receives <see cref="Line"/>s,
    /// <see cref="OptionSet"/>s and <see cref="Command"/>s from the
    /// DialogueRunner, and conveys them to the rest of the game. It is
    /// also responsible for relaying input from the user to the
    /// DialogueRunner, such as option selection or the signal to proceed
    /// to the next line.
    /// </remarks>
    /// <seealso cref="DialogueRunner"/>
    [HelpURL("https://yarnspinner.dev/docs/unity/components/dialogue-ui/")]
    public class DialogueUI : DialogueViewBase
    {
        /// <summary>
        /// The object that contains the dialogue and the options.
        /// </summary>
        /// <remarks>
        /// This object will be enabled when conversation starts, and
        /// disabled when it ends.
        /// </remarks>
        public GameObject dialogueContainer;

        /// <summary>
        /// How quickly to show the text, in seconds per character
        /// </summary>
        [Tooltip("How quickly to show the text, in seconds per character")]
        public float textSpeed = 0.025f;

        /// <summary>
        /// The buttons that let the user choose an option.
        /// </summary>
        /// <remarks>
        /// The <see cref="Button"/> objects in this list will be enabled
        /// and disabled by the <see cref="DialogueUI"/>. Each button
        /// should have as a child object a <see cref="Text"/> or a <see
        /// cref="TMPro.TextMeshProUGUI"/> as a label; the text of this
        /// child object will be updated by the DialogueUI as necessary.
        ///
        /// You do not need to configure the On Click action of any of
        /// these buttons. The <see cref="DialogueUI"/> will configure them
        /// for you.
        /// </remarks>
        public List<Button> optionButtons;

        /// <summary>
        /// Indicates whether lines should display character names, if
        /// present.
        /// </summary>
        /// <remarks>
        /// When this value is <see langword="false"/>, this class will
        /// check to see if the line contains any text with the `character`
        /// attribute. If such text exists, it will be removed from the
        /// text that is passed to the <see cref="onLineUpdate"/> event.
        /// </remarks>
        public bool showCharacterName = true;

        /// <summary>
        /// Indicates whether options whose line conditions have evaluated
        /// to false should be shown (but be unselectable). If this value
        /// is false, these options are not shown at all.
        /// </summary>
        public bool showUnavailableOptions = false;

        /// <summary>
        /// When true, the Runner has signaled to finish the current line
        /// asap.
        /// </summary>
        protected bool finishCurrentLine = false;

        // The method that we should call when the user has chosen an
        // option. Externally provided by the DialogueRunner.
        protected System.Action<int> currentOptionSelectionHandler;

        // When true, the DialogueRunner is waiting for the user to press
        // one of the option buttons.
        protected bool waitingForOptionSelection = false;

        /// <summary>
        /// A <see cref="UnityEngine.Events.UnityEvent"/> that is called
        /// when the dialogue starts.
        /// </summary>
        /// <remarks>
        /// Use this event to enable any dialogue-related UI and gameplay
        /// elements, and disable any non-dialogue UI and gameplay
        /// elements.
        /// </remarks>
        public UnityEngine.Events.UnityEvent onDialogueStart;

        /// <summary>
        /// A <see cref="UnityEngine.Events.UnityEvent"/> that is called
        /// when the dialogue ends.
        /// </summary>
        /// <remarks>
        /// Use this event to disable any dialogue-related UI and gameplay
        /// elements, and enable any non-dialogue UI and gameplay elements.
        /// </remarks>
        public UnityEngine.Events.UnityEvent onDialogueEnd;

        /// <summary>
        /// A <see cref="UnityEngine.Events.UnityEvent"/> that is called
        /// when a <see cref="Line"/> has been delivered.
        /// </summary>
        /// <remarks>
        /// This method is called before <see cref="onLineUpdate"/> is
        /// called. Use this event to prepare the scene to deliver a line.
        /// </remarks>
        public UnityEngine.Events.UnityEvent onLineStart;

        /// <summary>
        /// A <see cref="UnityEngine.Events.UnityEvent"/> that is called
        /// when the line has finished being displayed by this view.
        /// </summary>
        /// <remarks>
        /// This method is called after <see cref="onLineUpdate"/>. Use
        /// this method to display UI elements like a "continue" button. 
        ///
        /// If there are multiple views displaying this line, this method
        /// may be called some time before <see
        /// cref="onLineFinishDisplaying"/> is called.
        /// </remarks>
        /// <seealso cref="onLineUpdate"/>
        /// <seealso cref="onLineFinishDisplaying"/>
        /// <seealso cref="MarkLineComplete"/>
        public UnityEngine.Events.UnityEvent onTextFinishDisplaying;

        /// <summary>
        /// A <see cref="UnityEngine.Events.UnityEvent"/> that is called
        /// when a line has finished being delivered on all views.
        /// </summary>
        /// <remarks>
        /// Use this method to display UI elements like a "continue" button
        /// in sync with other <see cref="DialogueViewBase"/> objects, like
        /// voice over playback.
        /// </remarks>
        /// <seealso cref="onLineUpdate"/>
        /// <seealso cref="onTextFinishDisplaying"/>
        /// <seealso cref="MarkLineComplete"/>        
        public UnityEngine.Events.UnityEvent onLineFinishDisplaying;

        /// <summary>
        /// A <see cref="DialogueRunner.StringUnityEvent"/> that is called
        /// when the visible part of the line's localised text changes.
        /// </summary>
        /// <remarks>
        /// The <see cref="string"/> parameter that this event receives is
        /// the text that should be displayed to the user. Use this method
        /// to display line text to the user.
        ///
        /// The <see cref="DialogueUI"/> class gradually reveals the
        /// localised text of the <see cref="Line"/>, at a rate of <see
        /// cref="textSpeed"/> seconds per character. <see
        /// cref="onLineUpdate"/> will be called multiple times, each time
        /// with more text; the final call to <see cref="onLineUpdate"/>
        /// will have the entire text of the line.
        ///
        /// If the line's Status becomes <see
        /// cref="LineStatus.Interrupted"/>, which indicates that the user
        /// has requested that the Dialogue UI skip to the end of the line,
        /// <see cref="onLineUpdate"/> will be called once more, to display
        /// the entire text.
        ///
        /// If <see cref="textSpeed"/> is `0`, <see cref="onLineUpdate"/>
        /// will be called just once, to display the entire text all at
        /// once.
        ///
        /// After the final call to <see cref="onLineUpdate"/>, <see
        /// cref="onTextFinishDisplaying"/> will be called to indicate that
        /// the line has finished appearing, followed by <see
        /// cref="onLineFinishDisplaying"/>.
        /// </remarks>
        /// <seealso cref="textSpeed"/>
        /// <seealso cref="onTextFinishDisplaying"/>
        /// <seealso cref="onLineFinishDisplaying"/>
        public DialogueRunner.StringUnityEvent onLineUpdate;

        /// <summary>
        /// A <see cref="UnityEngine.Events.UnityEvent"/> that is called
        /// when a line has finished displaying, and should be removed from
        /// the screen.
        /// </summary>
        /// <remarks>
        /// This method is called after the line's <see
        /// cref="LocalizedLine.Status"/> has changed to <see
        /// cref="LineStatus.Ended"/>. Use this method to dismiss the
        /// line's UI elements.
        ///
        /// After this method is called, the next piece of dialogue content
        /// will be presented, or the dialogue will end.
        /// </remarks>
        public UnityEngine.Events.UnityEvent onLineEnd;

        /// <summary>
        /// A <see cref="UnityEngine.Events.UnityEvent"/> that is called
        /// when an <see cref="OptionSet"/> has been displayed to the user.
        /// </summary>
        /// <remarks>
        /// Before this method is called, the <see cref="Button"/>s in <see
        /// cref="optionButtons"/> are enabled or disabled (depending on
        /// how many options there are), and the <see cref="Text"/> or <see
        /// cref="TMPro.TextMeshProUGUI"/> is updated with the correct
        /// text.
        ///
        /// Use this method to ensure that the active <see
        /// cref="optionButtons"/>s are visible, such as by enabling the
        /// object that they're contained in.
        /// </remarks>
        public UnityEngine.Events.UnityEvent onOptionsStart;

        /// <summary>
        /// A <see cref="UnityEngine.Events.UnityEvent"/> that is called
        /// when an option has been selected, and the <see
        /// cref="optionButtons"/> should be hidden.
        /// </summary>
        /// <remarks>
        /// This method is called after one of the <see
        /// cref="optionButtons"/> has been clicked, or the <see
        /// cref="SelectOption(int)"/> method has been called.
        ///
        /// Use this method to hide all of the <see cref="optionButtons"/>,
        /// such as by disabling the object they're contained in. (The
        /// DialogueUI won't hide them for you individually.)
        /// </remarks>
        public UnityEngine.Events.UnityEvent onOptionsEnd;

        internal void Awake()
        {
            // Start by hiding the container
            if (dialogueContainer)
                dialogueContainer.SetActive(false);

            foreach (var button in optionButtons)
            {
                button.gameObject.SetActive(false);
            }
        }

        /// <inheritdoc/>
        public override void RunLine(LocalizedLine dialogueLine, System.Action onDialogueLineFinished)
        {
            StartCoroutine(DoRunLine(dialogueLine, onDialogueLineFinished));
        }

        /// <summary>
        /// Shows a line of dialogue, gradually.
        /// </summary>
        /// <param name="dialogueLine">The line to deliver.</param>
        /// <param name="onDialogueLineFinished">A callback to invoke when
        /// the text has finished appearing.</param>
        /// <returns></returns>
        protected IEnumerator DoRunLine(LocalizedLine dialogueLine, System.Action onDialogueLineFinished)
        {
            onLineStart?.Invoke();

            finishCurrentLine = false;

            // The final text we'll be showing for this line.
            string text;

            // Are we hiding the character name?
            if (showCharacterName == false)
            {

                // First, check to see if we have it
                var hasCharacterAttribute = dialogueLine.Text.TryGetAttributeWithName("character", out var characterAttribute);

                // If we do, remove it from the markup, and use the
                // resulting text
                if (hasCharacterAttribute)
                {
                    text = dialogueLine.Text.DeleteRange(characterAttribute).Text;
                }
                else
                {
                    // This line doesn't have a [character] attribute, so
                    // there's nothing to remove. We'll use the entire
                    // text.
                    text = dialogueLine.Text.Text;
                }
            }
            else
            {
                text = dialogueLine.Text.Text;
            }

            if (textSpeed > 0.0f)
            {
                // Display the line one character at a time
                var stringBuilder = new StringBuilder();

                foreach (char c in text)
                {
                    stringBuilder.Append(c);
                    onLineUpdate?.Invoke(stringBuilder.ToString());
                    if (finishCurrentLine)
                    {
                        // We've requested a skip of the entire line.
                        // Display all of the text immediately.
                        onLineUpdate?.Invoke(text);
                        break;
                    }
                    yield return new WaitForSeconds(textSpeed);
                }
            }
            else
            {
                // Display the entire line immediately if textSpeed <= 0
                onLineUpdate?.Invoke(text);
            }


            // Indicate to the rest of the game that the text has finished
            // being delivered
            onTextFinishDisplaying?.Invoke();

            // Indicate to the dialogue runner that we're done delivering
            // the line here
            onDialogueLineFinished();
        }

        public override void OnLineStatusChanged(LocalizedLine dialogueLine)
        {

            switch (dialogueLine.Status)
            {
                case LineStatus.Running:
                    // No-op; this line is running
                    break;
                case LineStatus.Interrupted:
                    // The line is now interrupted, and we need to hurry up
                    // in our delivery
                    finishCurrentLine = true;
                    break;
                case LineStatus.Delivered:
                    // The line has now finished its delivery across all
                    // views, so we can signal call our UnityEvent for it
                    onLineFinishDisplaying?.Invoke();
                    break;
                case LineStatus.Ended:
                    // The line has now Ended. DismissLine will be called
                    // shortly.
                    onLineEnd?.Invoke();
                    break;
            }
        }

        public override void DismissLine(System.Action onDismissalComplete)
        {
            // This view doesn't need any extra time to dismiss its view,
            // so it can just call onDismissalComplete immediately.
            onDismissalComplete();
        }

        /// Runs a set of options.
        /// <inheritdoc/>
        public override void RunOptions(DialogueOption[] dialogueOptions, System.Action<int> onOptionSelected)
        {
            StartCoroutine(DoRunOptions(dialogueOptions, onOptionSelected));
        }

        /// Show a list of options, and wait for the player to make a
        /// selection.
        protected IEnumerator DoRunOptions(DialogueOption[] dialogueOptions, System.Action<int> selectOption)
        {
            // Do a little bit of safety checking
            if (dialogueOptions.Length > optionButtons.Count)
            {
                Debug.LogWarning("There are more options to present than there are" +
                                 "buttons to present them in. Only the first " +
                                 $"{optionButtons.Count} options will be shown.");
            }

            // Display each option in a button, and make it visible
            int i = 0;

            waitingForOptionSelection = true;

            currentOptionSelectionHandler = selectOption;

            foreach (var dialogueOption in dialogueOptions)
            {

                bool allowOptionSelection = true;

                if (dialogueOption.IsAvailable == false)
                {
                    if (showUnavailableOptions)
                    {
                        // Flag that we want to make this button not
                        // selectable
                        allowOptionSelection = false;
                    }
                    else
                    {
                        // Completely ignore this option - don't show it at
                        // all
                        continue;
                    }
                }

                optionButtons[i].gameObject.SetActive(true);

                // When the button is selected, tell the dialogue about it
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => SelectOption(dialogueOption.DialogueOptionID));

                var optionText = dialogueOption.Line.Text.Text;

                if (optionText == null)
                {
                    Debug.LogWarning($"Option {dialogueOption.TextID} doesn't have any localised text");
                    optionText = dialogueOption.TextID;
                }

                var unityText = optionButtons[i].GetComponentInChildren<Text>();
                if (unityText != null)
                {
                    unityText.text = optionText;
                }

                var textMeshProText = optionButtons[i].GetComponentInChildren<TMPro.TMP_Text>();
                if (textMeshProText != null)
                {
                    textMeshProText.text = optionText;
                }

                // Make this button enabled if it's an available option
                optionButtons[i].interactable = allowOptionSelection;

                i++;
            }

            // hide all remaining unused buttons
            for (; i < optionButtons.Count; i++)
            {
                optionButtons[i].gameObject.SetActive(false);
            }

            onOptionsStart?.Invoke();

            // Wait until the chooser has been used and then removed 
            while (waitingForOptionSelection)
            {
                yield return null;
            }

            // Hide all the buttons
            foreach (var button in optionButtons)
            {
                button.gameObject.SetActive(false);
            }

            onOptionsEnd?.Invoke();
        }

        /// Called when the dialogue system has started running.
        /// <inheritdoc/>
        public override void DialogueStarted()
        {
            // Enable the dialogue controls.
            if (dialogueContainer)
                dialogueContainer.SetActive(true);

            onDialogueStart?.Invoke();
        }

        /// Called when the dialogue system has finished running.
        /// <inheritdoc/>
        public override void DialogueComplete()
        {
            onDialogueEnd?.Invoke();

            // Hide the dialogue interface.
            if (dialogueContainer)
                dialogueContainer.SetActive(false);
        }

        /// <summary>
        /// Signals that the user has selected an option.
        /// </summary>
        /// <remarks>
        /// This method is called by the <see cref="Button"/>s in the <see
        /// cref="optionButtons"/> list when clicked.
        ///
        /// If you prefer, you can also call this method directly.
        /// </remarks>
        /// <param name="optionID">The <see cref="OptionSet.Option.ID"/> of
        /// the <see cref="OptionSet.Option"/> that was selected.</param>
        public void SelectOption(int optionID)
        {
            if (!waitingForOptionSelection)
            {
                Debug.LogWarning("An option was selected, but the dialogue UI was not expecting it.");
                return;
            }
            waitingForOptionSelection = false;
            currentOptionSelectionHandler?.Invoke(optionID);
        }


    }
}
