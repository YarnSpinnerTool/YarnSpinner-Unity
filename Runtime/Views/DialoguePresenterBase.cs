using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> that is informed of important events occurring in your dialogue, when it receives them from a <see cref="DialogueRunner"/> and are used for a variety of different tasks.
    /// Primarily these are how you show lines and options to the player.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the Dialogue Runner encounters content that the user should see - that is, lines or options - it sends that content to all of the dialogue presenters stored in <see cref="DialogueRunner.dialoguePresenters"/>.
    /// The Dialogue Runner then waits until all Dialogue Presenters have reported that they have finished presenting the content.
    /// To use this class, subclass it, and implement its required methods. Once you have written your subclass, attach it as a component to a <see cref="GameObject"/>, and add this game object to the list of Dialogue presenters in your scene's <see cref="DialogueRunner"/>.
    /// </para>
    /// <para>
    /// Dialogue Presenters do not need to handle every kind of content that the Dialogue Runner might produce. For example, you might have one Dialogue Presenter that handles Lines, and another that handles Options.
    /// The built-in <see cref="LinePresenter"/> class is an example of this, in that it only handles Lines and does nothing when it receives Options.
    /// </para>
    /// <para>
    /// You may also have multiple Dialogue Presenters that handle the <i>same</i> kind of content.
    /// For example, you may have a Dialogue Presenter that receives Lines and uses them to play voice-over audio, and a second Dialogue Presenter that also receives Lines and uses them to display on-screen subtitles.
    /// </para>
    /// <para>
    /// While the main focus of Dialogue Presenters is on the <i>presentation</i> of content to the user they also are informed of a much wider gamut of important dialogue events.
    /// There is no restriction or limitation on what you do with your Dialogue Presenters other than with the exception of Options you cannot use them to <b>influence</b> how the dialogue works.
    /// Think of Dialogue Presenters as a read-only responder to your story.
    /// </para>
    /// </remarks>
    /// <seealso cref="LineProviderBehaviour"/>
    /// <seealso cref="DialogueRunner.dialoguePresenters"/>
    public abstract class DialoguePresenterBase : MonoBehaviour
    {
        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that a line should be displayed to the user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this method is called, the Dialogue Presenter should present the line to the user. 
        /// The content to present is contained within the <paramref name="line"/> parameter, which contains information about the line in the user's current locale.
        /// </para>
        /// <para style="tip">
        /// It's up to the Dialogue Presenter to decide what "presenting" the line may mean; for example, showing on-screen text, playing voice-over audio, or updating on-screen portraits to show a picture of the speaking character.
        /// </para>
        /// <para>
        /// The <see cref="DialogueRunner"/> will wait until the tasks from all of its dialogue presenters have completed before continuing to the next piece of content.
        /// If your dialogue presenter does not need to handle the line, it should return immediately.
        /// </para>
        /// <para style="info">The value of the <paramref name="line"/> parameter is produced by the Dialogue Runner's <see cref="LineProviderBehaviour"/>.
        /// </para>
        /// <para style="info">
        /// The default implementation of this method takes no action and returns immediately.
        /// </para>
        /// </remarks>
        /// <param name="line">The line to present.</param>
        /// <param name="token">A <see cref="LineCancellationToken"/> that represents whether the dialogue presenter should hurry it its presentation of the line, or stop showing the current line.</param>
        /// <returns>A task that completes when the dialogue presenter has finished showing the line to the user.</returns>
        /// <seealso cref="RunOptionsAsync(DialogueOption[],
        /// CancellationToken)"/>
        public abstract YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token);

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that a set of options should be displayed to the user.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called when the Dialogue Runner wants to show a collection of options that the user should choose from.
        /// Each option is represented by a <see cref="DialogueOption"/> object, which contains information about the option.
        /// </para>
        /// <para>
        /// When this method is called, the Dialogue Presenter should display appropriate user interface elements that let the user choose among the options.
        /// </para>
        /// <para>
        /// This method should await until an option is selected, and then return the selected option.
        /// If this view doesn't handle options, or is otherwise unable to select an option, it should return <see cref="DialogueRunner.NoOptionSelected"/>.
        /// The dialogue runner will wait for all dialogue views to return, so if a dialogue presenter doesn't or can't handle options, it's good practice to return as soon as possible.
        /// </para>
        /// <para>
        /// If the <paramref name="cancellationToken"/> becomes cancelled, this means that the dialogue runner no longer needs this Dialogue presenter to make a selection, and this method should return as soon as possible; its return value will not be used.
        /// </para>
        /// <para style="note">
        /// The default implementation of this method returns <see cref="DialogueRunner.NoOptionSelected"/>. 
        /// </para>
        /// </remarks>
        /// <param name="dialogueOptions">The set of options that should be displayed to the user.</param>
        /// <param name="cancellationToken">A <see cref="LineCancellationToken"/> that becomes cancelled when the dialogue runner no longer needs this dialogue presenter to return an option.</param>
        /// <returns>A task that indicates which option was selected, or that this dialogue presenter did not select an option.</returns>
        /// <seealso cref="RunLineAsync(LocalizedLine, LineCancellationToken)"/>
        /// <seealso cref="YarnAsync.NoOptionSelected"/> 
        public virtual YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            return YarnTask<DialogueOption?>.FromResult(null);
        }

        /// <summary>
        /// Called by the dialogue runner whenever a command is encountered in the dialogue.
        /// </summary>
        /// <remarks>
        /// This is called on all presenters <b>before</b> the normal command dispatch flow occurs.
        /// You cannot prevent the normal command dispatch happening from within your presenter.
        /// But if this will be called regardless of if there are any handlers for the command registered on the dialogue runner.
        /// <para>
        /// Because the nature of command parsing has more edge-cases than it might first seem if you do want to perform your own processing here, the <see cref="DialogueRunner.SplitCommandText(string)" /> method handles splitting apart the command into it's parameters for you.
        /// While a command is just a series of individual words the way it ends up being broken up and how escape characters work changes on if strings are used within the command or not.
        /// The first parameter of the command will always be name of the command.
        /// </para>
        /// </remarks>
        /// <param name="command">The command. It's values come entirely from the Yarn</param>
        /// <param name="cancellationToken">The cancellation token for this task, if cancelled this method should finish as soon as possible.</param>
        public virtual YarnTask RunCommand(Command command, LineCancellationToken cancellationToken)
        {
            return YarnTask.CompletedTask;
        }

        /// <summary>Called by the <see cref="DialogueRunner"/> to signal that dialogue has started.</summary>
        /// <remarks>
        /// <para>
        /// This method is called before any content (that is, lines, options or commands) are delivered.
        /// </para>
        /// <para>
        /// This method is a good place to perform tasks like preparing on-screen dialogue UI (for example, turning on a letterboxing effect, or making dialogue UI elements visible.)
        /// </para>
        /// <para style="note">
        /// The default implementation of this method does nothing.</para>
        /// </remarks>
        /// <param name="token">The cancellation token for this task, if cancelled this method should finish as soon as possible.</param>
        /// <returns>A task that represents any work done by this dialogue presenter in order to get ready for dialogue to run.</returns>
        public abstract YarnTask OnDialogueStartedAsync(CancellationToken token);

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> to signal that the dialogue has ended, and no more content will be delivered.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is called after the last piece of content (that is, lines, options or commands) finished running.
        /// </para>
        /// <para>
        /// This method is a good place to perform tasks like dismissing on-screen dialogue UI (for example, turning off a letterboxing effect, or hiding dialogue UI elements.)
        /// </para>
        /// <para style="note">
        /// The default implementation of this method does nothing.
        /// </para>
        /// </remarks>
        /// <param name="token">The cancellation token for this task, if cancelled this method should finish as soon as possible.</param>
        /// <returns>A task that represents any work done by this dialogue presenter in order to clean up after running dialogue.</returns>
        public abstract YarnTask OnDialogueCompleteAsync(CancellationToken token);

        /// <summary>
        /// Called by the <see cref="DialogueRunner"/> when a node is entered.
        /// </summary>
        /// <remarks>
        /// This will be called before any content from that node has been presented, but other content may have already been shown from other nodes.
        /// <para style="note">
        /// The default implementation of this method does nothing.
        /// </para>
        /// </remarks>
        /// <param name="nodeName">the node being entered</param>
        /// <param name="token">The cancellation token for this task, if cancelled this method should finish as soon as possible.</param>
        public virtual YarnTask OnNodeEnter(string nodeName, CancellationToken token = default) { return YarnTask.CompletedTask; }

         /// <summary>
        /// Called by the <see cref="DialogueRunner"/> when a node is exited.
        /// </summary>
        /// <remarks>
        /// This will be called before after all content from that node has been presented, but does not necessarily signal the end of dialogue.
        /// <para style="note">
        /// The default implementation of this method does nothing.
        /// </para>
        /// </remarks>
        /// <param name="nodeName">the node being exited</param>
        /// <param name="token">The cancellation token for this task, if cancelled this method should finish as soon as possible.</param>
        public virtual YarnTask OnNodeExit(string nodeName, CancellationToken token = default) { return YarnTask.CompletedTask; }

        /// <summary>
        /// The typewriter is the object that is responsible for controlling the timing of visual presentation content.
        /// As such not all Dialogue Presenters have or need one.
        /// </summary>
        /// <remarks>
        /// The typewriter by itself won't do anything and needs to be told when to do things by the presenter however because the style of typewriter can change this property exists so that there is a common point to assign the typewriter.
        /// </remarks>
        /// <seealso cref="LinePresenter"/>
        /// <seealso cref="LetterTypewriter"/>
        public virtual IAsyncTypewriter? Typewriter { get; set; }
    }
}

