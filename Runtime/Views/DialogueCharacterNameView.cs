/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Threading;
using UnityEngine.Events;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// A subclass of <see cref="DialogueViewBase"/> that displays character
    /// names.
    /// </summary>
    /// <remarks>
    /// <para>This class uses the `character` attribute on lines that it
    /// receives to determine its content. When the view's <see
    /// cref="RunLineAsync"/> method is called with a line whose <see
    /// cref="LocalizedLine.Text"/> contains a `character` attribute, the <see
    /// cref="onNameUpdate"/> event is fired. If the line does not contain such
    /// an attribute, the <see cref="onNameNotPresent"/> event is fired
    /// instead.</para>
    ///
    /// <para>This view does not present any options or handle commands. It's
    /// intended to be used alongside other subclasses of <see
    /// cref="AsyncDialogueViewBase"/>.</para>
    /// </remarks>
    public class DialogueCharacterNameView : AsyncDialogueViewBase
    {
        /// <summary>
        /// Invoked when a line is received that contains a character name.
        /// The name is given as the parameter.
        /// </summary>
        /// <seealso cref="onNameNotPresent"/>
        public UnityEventString onNameUpdate;

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

        /// <inheritdoc/>
        public override YarnTask OnDialogueCompleteAsync()
        {
            return YarnTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override YarnTask OnDialogueStartedAsync()
        {
            onDialogueStarted?.Invoke();
            return YarnTask.CompletedTask;
        }

        /// <summary>
        /// Invokes the <see cref="onNameUpdate"/> or <see
        /// cref="onNameNotPresent"/> events, depending on the contents of
        /// <paramref name="line"/>.
        /// </summary>
        /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync" path="/param" />
        /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync" path="/returns" />
        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            // Try and get the character name from the line
            string? characterName = line.CharacterName;

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

            return YarnTask.CompletedTask;
        }

        /// <summary>Takes no action; this dialogue view does not handle
        /// options.</summary>
        /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync" path="/param"
        /// />
        /// <inheritdoc cref="AsyncDialogueViewBase.RunLineAsync"
        /// path="/returns" />
        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            return DialogueRunner.NoOptionSelected;
        }
    }
}
