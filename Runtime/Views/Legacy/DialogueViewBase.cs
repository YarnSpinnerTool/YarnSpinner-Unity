/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System;
using System.Threading;
using UnityEngine;

#nullable enable

namespace Yarn.Unity.Legacy
{
    /// <summary>
    /// Implements the Yarn Spinner 2 callback-based API for dialogue views
    /// using Yarn Spinner 3.
    /// </summary>
    /// <remarks>
    /// You should not use this class in new code. It exists to provide a
    /// compatibility layer for existing Yarn Spinner dialogue views. New
    /// dialogue views should subclass <see cref="DialoguePresenterBase"/>
    /// directly.
    /// </remarks>
    [Obsolete("Use " + nameof(DialoguePresenterBase))]
    public abstract class DialogueViewBase : DialoguePresenterBase
    {
        /// <summary>
        /// Represents the method that should be called when this view wants the
        /// line to be interrupted.
        /// </summary>
        /// <remarks>
        /// <para style="info">This value is set by the <see
        /// cref="DialogueRunner"/> class during initial setup. Do not modify
        /// this value yourself.
        /// </para>
        /// <para>
        /// When this method is called, the Dialogue Runner that has this
        /// Dialogue View in its <see cref="DialogueRunner.dialogueViews"/> list
        /// will call <see cref="InterruptLine(LocalizedLine, Action)"/> on any
        /// view that has not yet finished presenting its line.
        /// </para>
        /// <para>
        /// A Dialogue View can call this method to signal to the Dialogue
        /// Runner that the current line should be interrupted. This is usually
        /// done when it receives some input that the user wants to skip to the
        /// next line of dialogue.
        /// </para>
        /// </remarks>
        public Action? requestInterrupt;

        /// <summary>Called by the <see cref="DialogueRunner"/> to signal that
        /// dialogue has started.</summary>
        /// <remarks>
        /// <para>This method is called before any content (that is, lines,
        /// options or commands) are delivered.</para>
        /// <para>This method is a good place to perform tasks like preparing
        /// on-screen dialogue UI (for example, turning on a letterboxing
        /// effect, or making dialogue UI elements visible.)
        /// </para>
        /// <para style="note">The default implementation of this method does
        /// nothing.</para>
        /// </remarks>
        public virtual void DialogueStarted()
        {
            // Default implementation does nothing.
        }

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that a line
        /// should be displayed to the user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this method is called, the Dialogue View should present the
        /// line to the user. The content to present is contained within the
        /// <paramref name="dialogueLine"/> parameter, which contains
        /// information about the line in the user's current locale.
        /// </para>
        /// <para style="info">The value of the <paramref name="dialogueLine"/>
        /// parameter is produced by the Dialogue Runner's <see
        /// cref="LineProviderBehaviour"/>.
        /// </para>
        /// <para>
        /// It's up to the Dialogue View to decide what "presenting" the line
        /// may mean; for example, showing on-screen text, or playing voice-over
        /// audio.
        /// </para>
        /// <para>When the line has finished being presented, this method calls
        /// the <paramref name="onDialogueLineFinished"/> method, which signals
        /// to the Dialogue Runner that this Dialogue View has finished
        /// presenting the line. When all Dialogue Views have finished
        /// presenting the line, the Dialogue Runner calls <see
        /// cref="DismissLine(Action)"/> to signal that the views should get rid
        /// of the line.</para>
        /// <para>
        /// If you want to create a Dialogue View that waits for user input
        /// before continuing, either wait for that input before calling
        /// <paramref name="onDialogueLineFinished"/>, or don't call it at all
        /// and instead call <see cref="requestInterrupt"/> to tell the Dialogue
        /// Runner to interrupt the line.
        /// </para>
        /// <para style="danger">
        /// The <paramref name="onDialogueLineFinished"/> method should only be
        /// called when <see cref="RunLine"/> finishes its presentation
        /// normally. If <see cref="InterruptLine"/> has been called, you must
        /// call the completion handler that it receives, and not the completion
        /// handler that <see cref="RunLine"/> has received.
        /// </para>
        /// <para style="note">
        /// The default implementation of this method immediately calls the
        /// <paramref name="onDialogueLineFinished"/> method (that is, it
        /// reports that it has finished presenting the line the moment that it
        /// receives it), and otherwise does nothing.
        /// </para>
        /// </remarks>
        /// <param name="dialogueLine">The content of the line that should be
        /// presented to the user.</param>
        /// <param name="onDialogueLineFinished">The method that should be
        /// called after the line has finished being presented.</param>
        /// <seealso cref="InterruptLine(LocalizedLine, Action)"/>
        /// <seealso cref="DismissLine(Action)"/>
        /// <seealso cref="RunOptions(DialogueOption[], Action{int})"/>
        public virtual void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            // The default implementation does nothing, and immediately calls
            // onDialogueLineFinished.
            onDialogueLineFinished?.Invoke();
        }

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that a line has
        /// been interrupted, and that the Dialogue View should finish
        /// presenting its line as quickly as possible.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called when Dialogue Runner wants to interrupt the
        /// presentation of the current line, in order to proceed to the next
        /// piece of content.
        /// </para>
        /// <para>
        /// When this method is called, the Dialogue View must finish presenting
        /// their line as quickly as it can. Depending on how this Dialogue View
        /// presents lines, this can mean different things: for example, a view
        /// that plays voice-over audio might stop playback immediately, or fade
        /// out playback over a short period of time; a view that displays text
        /// a letter at a time might display all of the text at once.
        /// </para>
        /// <para>
        /// The process of finishing the presentation can take time to complete,
        /// but should happen as quickly as possible, because this method is
        /// generally called when the user wants to skip the current line.
        /// </para>
        /// <para>
        /// When the line has finished presenting, the <paramref
        /// name="onDialogueLineFinished"/> method must be called, which
        /// indicates to the Dialogue Runner that this line is ready to be
        /// dismissed.
        /// </para>
        /// <para style="danger">
        /// When <see cref="InterruptLine"/> is called, you must not call the
        /// completion handler that <see cref="RunLine"/> has previously
        /// received - this completion handler is no longer valid. Call this method's <paramref name="onDialogueLineFinished"/> instead.
        /// </para>
        /// <para style="note">
        /// The default implementation of this method immediately calls the
        /// <paramref name="onDialogueLineFinished"/> method (that is, it
        /// reports that it has finished presenting the line the moment that it
        /// receives it), and otherwise does nothing.
        /// </para>
        /// </remarks>
        /// <param name="dialogueLine">The current line that is being
        /// presented.</param>
        /// <param name="onDialogueLineFinished">The method that should be
        /// called after the line has finished being presented.</param>
        /// <seealso cref="RunLine(LocalizedLine, Action)"/>
        /// <seealso cref="DismissLine(Action)"/>
        public virtual void InterruptLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
        {
            // the default implementation does nothing
            onDialogueLineFinished?.Invoke();
        }

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that the view
        /// should dismiss its current line from display, and clean up.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called when all Dialogue Views attached to a Dialogue
        /// Runner report that they have finished presenting this line. When
        /// this occurs, the Dialogue Runner calls <see cref="DismissLine"/> on
        /// all Dialogue Views to tell them to clear their current line from
        /// display.</para>
        /// <para>Depending on how the Dialogue View presents lines,
        /// "dismissing" a line may mean different things. For example, a
        /// Dialogue View that presents on-screen text might fade the text away,
        /// or a Dialogue View that presents voice-over dialogue may not need to
        /// do anything at all (because audio finished playing when the line
        /// finished presenting.)
        /// </para>
        /// <para style="hint">
        /// Dismissing the line can take time, but should ideally be as fast as
        /// possible, because the user will be waiting for the next piece of
        /// content to appear. </para>
        /// <para>
        /// When the line has finished dismissing, this method calls
        /// onDismissalComplete to indicate that the dismissal is complete. When
        /// all Dialogue Views on a Dialogue Runner have finished dismissing,
        /// the Dialogue Runner moves on to the next piece of content.
        /// </para>
        /// <para style="note">
        /// The default implementation of this method immediately calls the
        /// <paramref name="onDismissalComplete"/> method (that is, it reports
        /// that it has finished dismissing the line the moment that it receives
        /// it), and otherwise does nothing.
        /// </para>
        /// </remarks>
        /// <param name="onDismissalComplete">The method that should be called
        /// when the view has finished dismissing the line.</param>
        public virtual void DismissLine(Action onDismissalComplete)
        {
            // The default implementation does nothing, and immediately calls
            // onDialogueLineFinished.
            onDismissalComplete?.Invoke();
        }

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that a set of
        /// options should be displayed to the user.
        /// </summary>
        /// <remarks>
        /// <para>This method is called when the Dialogue Runner wants to show a
        /// collection of options that the user should choose from. Each option
        /// is represented by a <see cref="DialogueOption"/> object, which
        /// contains information about the option.</para>
        /// <para>When this method is called, the Dialogue View should display
        /// appropriate user interface elements that let the user choose among
        /// the options.</para>
        /// <para>After this method is called, the <see cref="DialogueRunner"/>
        /// will wait until the <paramref name="onOptionSelected"/> method is
        /// called.</para>
        /// <para>After calling the <paramref name="onOptionSelected"/> method, the
        /// Dialogue View should dismiss whatever options UI it presented. The
        /// Dialogue Runner will immediately deliver the next piece of content.
        /// </para>
        ///
        /// <para style="warning">When the Dialogue Runner delivers Options to
        /// its Dialogue Views, it expects precisely one of its views to call
        /// the <paramref name="onOptionSelected"/> method.
        /// <list type="bullet">
        /// <item>
        /// If your scene includes <b>no</b> dialogue views that override <see
        /// cref="RunOptions"/>, the Dialogue Runner will never be told which
        /// option the user selected, and will therefore wait forever.
        /// </item>
        /// <item>
        /// If your scene includes <b>multiple</b> dialogue views that override
        /// <see cref="RunOptions"/>, they will all receive a call each time the
        /// dialogue system presents options to the player. You must ensure that
        /// only one of them calls the <paramref name="onOptionSelected"/>
        /// method.
        /// </item>
        /// </list>
        /// </para>
        /// <para style="note">
        /// The default implementation of this method does nothing, and does not
        /// call the <paramref name="onOptionSelected"/> method (that is, it
        /// ignores any Options it receives.)
        /// </para>
        /// </remarks>
        /// <param name="dialogueOptions">The set of options that should be
        /// displayed to the user.</param>
        /// <param name="onOptionSelected">A method that should be called when
        /// the user has made a selection.</param>
        public virtual void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
        {
            // The default implementation does nothing.
        }

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that the
        /// dialogue has ended, and no more lines will be delivered.
        /// </summary>
        /// <remarks>
        /// <para>This method is called after the last piece of content (that
        /// is, lines, options or commands) finished running.</para>
        /// <para>This method is a good place to perform tasks like dismissing
        /// on-screen dialogue UI (for example, turning off a letterboxing
        /// effect, or hiding dialogue UI elements.)
        /// </para>
        /// <para>
        /// If <see cref="DialogueRunner.Stop()" /> is called, this method is how your custom views are informed of this.
        /// This allows you to skip over the normal flow of dialogue, so please use this method to clean up your views.
        /// </para>
        /// <para style="note">The default implementation of this method does
        /// nothing.</para>
        /// </remarks>
        public virtual void DialogueComplete()
        {
            // Default implementation does nothing.
        }

        /// <summary>
        /// Called by <see cref="DialogueAdvanceInput"/> to signal that the user
        /// has requested that the dialogue advance.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this method is called, the Dialogue View should advance the
        /// dialogue. Advancing the dialogue can mean different things,
        /// depending on the nature of the dialogue view, and its current state.
        /// </para>
        /// <para>
        /// In many situations, if the Dialogue View hasn't yet finished
        /// presenting its line (that is, the <see cref="RunLine(LocalizedLine,
        /// Action)"/> method has been called, but it hasn't yet called its
        /// completion handler), it's sufficient to call the <see
        /// cref="requestInterrupt"/> method, which tells the Dialogue Runner to
        /// interrupt the current line.
        /// </para>
        /// <para>
        /// 'Advancing' the dialogue may not always mean <em>finishing</em> the
        /// line's presentation.
        /// </para>
        /// <para>
        /// For example, in the <em>Legend of Zelda</em> series of games, lines
        /// of dialogue are displayed one character at a time in a text box,
        /// until the line has finished appearing. At this point, the text box
        /// displays a button to continue; when the user presses the primary
        /// input button (typically the <c>A</c> button), the line is dismissed.
        /// However, if this button is pressed <em>while the line is still
        /// appearing</em>, the rest of the line appears all at once, and the
        /// button appears. Finally, if a secondary input button (typically the
        /// <c>B</c> button) is pressed at any point, the line is
        /// <em>interrupted</em>, and the dialogue proceeds to the next line
        /// immediately.</para>
        /// <para>
        /// <see cref="UserRequestedViewAdvancement"/> is designed to give your
        /// Dialogue View an opportunity to decide whether it wants to interrupt
        /// the entire line for all views, or simply speed up the delivery of
        /// <em>this</em> view.
        /// </para>
        /// <para style="note">The default implementation of this method does
        /// nothing.</para>
        /// </remarks>
        public virtual void UserRequestedViewAdvancement()
        {
            // default implementation does nothing
        }

        /// <inheritdoc/>
        public override YarnTask OnDialogueStartedAsync()
        {
            // Invoke the synchronous version of 'dialogue started'
            this.DialogueStarted();
            return YarnTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override YarnTask OnDialogueCompleteAsync()
        {
            // Invoke the synchronous version of 'dialogue started'
            this.DialogueComplete();
            return YarnTask.CompletedTask;
        }

        // This method implements the v3 async pattern for dialogue views on top
        // of the v2 API.
        /// <inheritdoc/>
        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            // phaseComplete is a flag that represents whether the current
            // 'phase' of a v2-style dialogue view (Run, Interrupt, Dismiss) is
            // complete or not.
            bool phaseComplete = false;
            void PhaseComplete() => phaseComplete = true;

            // Run the line, and make phaseComplete become true when it's done.
            this.RunLine(line, PhaseComplete);

            // Wait for one of the following things to happen:
            // 1. RunLine completes successfully and calls PhaseComplete.
            // 2. The line is cancelled.
            while (phaseComplete == false
                   && token.IsNextLineRequested == false
                   && Application.exitCancellationToken.IsCancellationRequested == false)
            {
                await YarnTask.Yield();
            }

            // If the line was cancelled, tell the view that the line was
            // 'interrupted' and should finish presenting quickly. Wait for the
            // phase to complete.
            if (token.IsNextLineRequested)
            {
                phaseComplete = false;
                this.InterruptLine(line, PhaseComplete);
                while (phaseComplete == false
                  && Application.exitCancellationToken.IsCancellationRequested == false)
                {
                    await YarnTask.Yield();
                }
            }

            // Finally, signal that the line should be dismissed, and wait for
            // the dismissal to complete.
            phaseComplete = false;
            this.DismissLine(PhaseComplete);

            while (phaseComplete == false
                  && Application.exitCancellationToken.IsCancellationRequested == false)
            {
                await YarnTask.Yield();
            }
        }

        // This method implements the v3 async pattern for dialogue views on top
        // of the v2 API.
        /// <inheritdoc/>
        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            int selectedOptionID = -1;

            // Run the options, and wait for either a selection to be made, or
            // for this view to be cancelled.
            this.RunOptions(dialogueOptions, (selectedID) =>
            {
                selectedOptionID = selectedID;
            });

            while (selectedOptionID == -1 && cancellationToken.IsCancellationRequested == false && Application.exitCancellationToken.IsCancellationRequested == false)
            {
                await YarnTask.Yield();
            }

            if (cancellationToken.IsCancellationRequested || Application.exitCancellationToken.IsCancellationRequested)
            {
                // We were cancelled or are exiting the game. Return null.
                return null;
            }

            // Find the option that has the same ID as the one that was
            // selected, and return that.
            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                if (dialogueOptions[i].DialogueOptionID == selectedOptionID)
                {
                    return dialogueOptions[i];
                }
            }

            // If we got here, we weren't cancelled, but we also didn't select
            // an option that was valid. Throw an error.
            throw new InvalidOperationException($"Option view selected an invalid option ID ({selectedOptionID})");
        }
    }
}
