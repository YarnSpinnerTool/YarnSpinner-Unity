using System;
using UnityEngine;

namespace Yarn.Unity
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> that can present the data of a
    /// dialogue executed by a <see cref="DialogueRunner"/> to the user.
    /// The <see cref="DialogueRunner"/> uses subclasses of this type to
    /// relay information to and from the user, and to pause and resume the
    /// execution of the <see cref="YarnScript"/>.
    /// </summary>
    /// <remarks>
    /// The term "view" is meant in the broadest sense, e.g. a view on the
    /// dialogue (MVVM pattern). Therefore, this abstract class only
    /// defines how a specific view on the dialogue should communicate with
    /// the <see cref="DialogueRunner"/> (e.g. display text or trigger a
    /// voice over clip). How to present the content to the user will be
    /// the responsibility of all classes inheriting from this class.
    ///
    /// The inheriting classes will receive a <see cref="LocalizedLine"/>
    /// and can be in one of the stages defined in <see
    /// cref="DialogueLineStatus"/> while presenting it.
    /// </remarks>
    /// <seealso cref="DialogueRunner.dialogueViews"/>
    /// <seealso cref="DialogueUI"/>
    public abstract class DialogueViewBase : MonoBehaviour
    {
        /// <summary>
        /// Represents the method that should be called when this view
        /// wants the line to be interrupted or to proceed to the next
        /// line.
        /// </summary>
        internal System.Action onUserWantsLineContinuation;

        /// <summary>Signals that a conversation has started.</summary>
        public virtual void DialogueStarted()
        {
            // Default implementation does nothing.
        }

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that a
        /// line should be displayed to the user.
        /// </summary>
        /// <param name="dialogueLine">The content of the line that should
        /// be presented to the user.</param>
        /// <param name="onDialogueLineFinished">The method that should be
        /// called after the line has been finished.</param>
        /// FIXME: If this method is expected to be called only from the
        /// DialogueRunner then this should be converted into a coroutine
        /// and merged with RunLineWithCallback();
        public virtual void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            // The default implementation does nothing, and immediately
            // calls onDialogueLineFinished.
            onDialogueLineFinished?.Invoke();
        }

        /// <summary>
        /// Called by the DialogueRunner to indicate that the line that
        /// this view is delivering has changed state.
        /// </summary>
        /// <remarks>
        /// Subclasses of <see cref="DialogueViewBase"/> should override
        /// this method to be notified when a line has become interrupted,
        /// and when the line has finished being delivered by all views.
        ///
        /// The default implementation does nothing.
        /// </remarks>
        /// <param name="dialogueLine">The <see cref="LocalizedLine"/> that
        /// has changed state.</param>
        /// <seealso cref="LineStatus"/>
        public virtual void OnLineStatusChanged(LocalizedLine dialogueLine)
        {
            // Default implementation is a no-op.
        }

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that the
        /// view should dismiss its current line from display, and clean
        /// up.
        /// </summary>
        /// <param name="onDismissalComplete">The method that should be
        /// called when the view has finished dismissing the line.</param>
        public virtual void DismissLine(Action onDismissalComplete)
        {
            // The default implementation does nothing, and immediately
            // calls onDialogueLineFinished.
            onDismissalComplete?.Invoke();
        }

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that a set
        /// of options should be displayed to the user.
        /// </summary>
        /// <remarks>
        /// <para>When this method is called, the <see
        /// cref="DialogueRunner"/> will pause execution until the
        /// `onOptionSelected` method is called.</para>
        ///
        /// <para>If your scene includes multiple dialogue views that
        /// override this method, they will all receive a call each time
        /// the dialogue system presents options to the player. You must
        /// ensure that only one of them calls the <paramref
        /// name="onOptionSelected"/> method.</para>
        /// </remarks>
        /// <param name="dialogueOptions">The set of options that should be
        /// displayed to the user.</param>
        /// <param name="onOptionSelected">A method that should be called
        /// when the user has made a selection.</param>
        public virtual void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            // The default implementation does nothing.
        }

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that the
        /// end of a node has been reached.
        /// </summary>
        /// <remarks>
        /// This method may be called multiple times before <see
        /// cref="DialogueComplete"/> is called. If this method returns
        /// <see cref="Dialogue.HandlerExecutionType.ContinueExecution"/>,
        /// do not call the <paramref name="onComplete"/> method.
        ///
        /// The default implementation does nothing.
        /// </remarks>
        /// <param name="nextNode">The name of the next node that is being
        /// entered.</param>
        /// <param name="onComplete">A method that should be called to
        /// /// indicate that the DialogueRunner should continue
        /// executing.</param>
        /// <inheritdoc cref="RunLine(Line, ILineLocalisationProvider,
        /// Action)"/> FIXME: This doesn't seem to be called anymore ...?
        public virtual void NodeComplete(string nextNode, Action onComplete)
        {
            // The default implementation does nothing.            
        }

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that the
        /// dialogue has ended, and no more lines will be delivered.
        /// </summary>
        /// <remarks>
        /// The default implementation does nothing.
        /// </remarks>
        public virtual void DialogueComplete()
        {
            // Default implementation does nothing.
        }

        /// <summary>
        /// Signals that the user wants to go to the next line.
        /// </summary>
        /// <remarks>
        /// This method is generally called by a "continue" button, and
        /// causes the DialogueUI to signal the <see
        /// cref="DialogueRunner"/> to proceed to the next piece of
        /// content.
        ///
        /// If this method is called before the line has finished appearing
        /// (that is, before the line's status changes to <see
        /// cref="LineStatus.FinishedPresenting"/>), the line's status will change
        /// to <see cref="LineStatus.Interrupted"/>, and <see
        /// cref="OnLineStatusChanged"/> will be called to notify the view.
        /// </remarks>
        public void ReadyForNextLine()
        {
            // Call the continuation callback, if we have it.
            onUserWantsLineContinuation?.Invoke();
        }
    }
}
