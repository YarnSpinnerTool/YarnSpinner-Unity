using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> that can present lines and options to the
    /// user, when it receives them from a  <see cref="DialogueRunner"/>.
    /// </summary>
    /// <remarks>
    /// <para>When the Dialogue Runner encounters content that the user should
    /// see - that is, lines or options - it sends that content to all of the
    /// dialogue presenters stored in <see cref="DialogueRunner.dialogueViews"/>. The
    /// Dialogue Runner then waits until all Dialogue Presenters have reported that
    /// they have finished presenting the content.</para>
    /// <para>
    /// To use this class, subclass it, and implement its required methods. Once
    /// you have written your subclass, attach it as a component to a <see
    /// cref="GameObject"/>, and add this game object to the list of Dialogue
    /// presenters in your scene's <see cref="DialogueRunner"/>.
    /// </para>
    /// <para>Dialogue Presenters do not need to handle every kind of content that
    /// the Dialogue Runner might produce. For example, you might have one
    /// Dialogue Presenter that handles Lines, and another that handles Options. The
    /// built-in <see cref="LineView"/> class is an example of this, in that it
    /// only handles Lines and does nothing when it receives Options.</para>
    /// <para>
    /// You may also have multiple Dialogue Presenters that handle the <i>same</i>
    /// kind of content. For example, you may have a Dialogue Presenter that receives
    /// Lines and uses them to play voice-over audio, and a second Dialogue Presenter
    /// that also receives Lines and uses them to display on-screen subtitles.
    /// </para>
    /// </remarks>
    /// <seealso cref="LineProviderBehaviour"/>
    /// <seealso cref="DialogueRunner.dialogueViews"/>
    public abstract class DialoguePresenterBase : MonoBehaviour
    {
        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that a line
        /// should be displayed to the user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this method is called, the Dialogue Presenter should present the
        /// line to the user. The content to present is contained within the
        /// <paramref name="line"/> parameter, which contains information about
        /// the line in the user's current locale.
        /// </para>
        /// <para style="tip">
        /// It's up to the Dialogue Presenter to decide what "presenting" the line
        /// may mean; for example, showing on-screen text, playing voice-over
        /// audio, or updating on-screen portraits to show a picture of the
        /// speaking character.
        /// </para>
        /// <para>
        /// The <see cref="DialogueRunner"/> will wait until the tasks from all
        /// of its dialogue presenters have completed before continuing to the next
        /// piece of content. If your dialogue presenter does not need to handle the
        /// line, it should return immediately.
        /// </para>
        /// <para style="info">The value of the <paramref name="line"/>
        /// parameter is produced by the Dialogue Runner's <see
        /// cref="LineProviderBehaviour"/>.
        /// </para>
        /// <para style="info">
        /// The default implementation of this method takes no action and
        /// returns immediately.
        /// </para>
        /// </remarks>
        /// <param name="line">The line to present.</param>
        /// <param name="token">A <see cref="LineCancellationToken"/> that
        /// represents whether the dialogue presenter should hurry it its
        /// presentation of the line, or stop showing the current line.</param>
        /// <returns>A task that completes when the dialogue presenter has finished
        /// showing the line to the user.</returns>
        /// <seealso cref="RunOptionsAsync(DialogueOption[],
        /// CancellationToken)"/>
        public abstract YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token);


        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that a set of
        /// options should be displayed to the user.
        /// </summary>
        /// <remarks>
        /// <para>This method is called when the Dialogue Runner wants to show a
        /// collection of options that the user should choose from. Each option
        /// is represented by a <see cref="DialogueOption"/> object, which
        /// contains information about the option.</para>
        /// <para>When this method is called, the Dialogue Presenter should display
        /// appropriate user interface elements that let the user choose among
        /// the options.</para>
        /// <para>This method should await until an option is selected, and then
        /// return the selected option. If this view doesn't handle options, or
        /// is otherwise unable to select an option, it should return <see
        /// cref="YarnAsync.NoOptionSelected"/>. The dialogue runner will wait
        /// for all dialogue views to return, so if a dialogue presenter doesn't or
        /// can't handle options, it's good practice to return as soon as
        /// possible. 
        /// </para>
        /// <para>If the <paramref name="cancellationToken"/> becomes cancelled,
        /// this means that the dialogue runner no longer needs this Dialogue
        /// presenter to make a selection, and this method should return as soon as
        /// possible; its return value will not be used.
        /// </para>
        /// <para style="note">
        /// The default implementation of this method returns <see
        /// cref="YarnAsync.NoOptionSelected"/>. 
        /// </para>
        /// </remarks>
        /// <param name="dialogueOptions">The set of options that should be
        /// displayed to the user.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>
        /// that becomes cancelled when the dialogue runner no longer needs this
        /// dialogue presenter to return an option.</param>
        /// <returns>A task that indicates which option was selected, or that this dialogue presenter did not select an option.</returns>
        /// <seealso cref="RunLineAsync(LocalizedLine, LineCancellationToken)"/>
        /// <seealso cref="YarnAsync.NoOptionSelected"/> 
        [System.Obsolete("The LineCancellationToken form of RunOptionsAsync allows for option cancellation and hurrying up and is prefered.")]
        public virtual YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            return YarnTask<DialogueOption?>.FromResult(null);
        }

#pragma warning disable 0618
        public virtual YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            return RunOptionsAsync(dialogueOptions, cancellationToken.NextLineToken);
        }
#pragma warning restore 0618


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
        /// <returns>A task that represents any work done by this dialogue presenter in order to get ready for dialogue to run.</returns>
        public abstract YarnTask OnDialogueStartedAsync();

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
        /// <para style="note">The default implementation of this method does
        /// nothing.</para>
        /// </remarks>
        /// <returns>A task that represents any work done by this dialogue presenter
        /// in order to clean up after running dialogue.</returns>
        public abstract YarnTask OnDialogueCompleteAsync();

        // these are virtual because it's quite likely you don't need them
        // they are also void instead of YarnTask because currently the VM doesn't wait on node enter/exit so we can't either
        public virtual void OnNodeEnter(string nodeName) { }
        public virtual void OnNodeExit(string nodeName) { }

        public virtual IAsyncTypewriter? Typewriter { get; set; }
    }
}

