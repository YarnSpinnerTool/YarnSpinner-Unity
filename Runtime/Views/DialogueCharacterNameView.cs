using System;
using UnityEngine.Events;

namespace Yarn.Unity
{
    /// <summary>
    /// A subclass of <see cref="DialogueViewBase"/> that displays
    /// character names.
    /// </summary>
    /// <remarks>
    /// This class uses the `character` attribute on lines that it receives
    /// to determine its content. When the view's <see cref="RunLine"/>
    /// method is called with a line whose <see cref="LocalizedLine.Text"/>
    /// contains a `character` attribute, the <see cref="onNameUpdate"/>
    /// event is fired. If the line does not contain such an attribute, the
    /// <see cref="onNameNotPresent"/> event is fired instead.
    ///
    /// This view does not present any options or handle commands. It's
    /// intended to be used alongside other subclasses of DialogueViewBase.
    /// </remarks>
    /// <seealso cref="DialogueUI"/>
    public class DialogueCharacterNameView : Yarn.Unity.DialogueViewBase
    {
        /// <summary>
        /// Invoked when a line is received that contains a character name.
        /// The name is given as the parameter.
        /// </summary>
        /// <seealso cref="onNameNotPresent"/>
        public DialogueRunner.StringUnityEvent onNameUpdate;

        /// <summary>
        /// Invoked when the dialogue is started.
        /// </summary>
        public UnityEvent onDialogueStarted;

        /// <summary>
        /// Invoked when a line is received that doesn't contain a
        /// character name.
        /// </summary>
        /// <remarks>
        /// Games can use this event to hide the name UI.
        /// </remarks>
        /// <seealso cref="onNameUpdate"/>
        public UnityEvent onNameNotPresent;

        public override void DialogueStarted()
        {
            onDialogueStarted?.Invoke();
        }

        public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            // Try and get the character name from the line
            string characterName = dialogueLine.CharacterName;

            // Did we find one?
            if (!string.IsNullOrEmpty(characterName))
            {
                // Then notify the rest of the scene about it. This
                // generally involves updating a text view and making it
                // visible.
                onNameUpdate?.Invoke(characterName);
            }
            else
            {
                // Otherwise, notify the scene about not finding it. This
                // generally involves making the name text view not
                // visible.
                onNameNotPresent?.Invoke();
            }

            // Immediately mark this view as having finished its work
            onDialogueLineFinished();
        }
    }
}
