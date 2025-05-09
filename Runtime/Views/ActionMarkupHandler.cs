/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using UnityEngine;
using Yarn.Markup;
using System.Threading;

#if USE_TMP
using TMPro;
#else
using TMP_Text = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity
{
    public interface IActionMarkupHandler
    {
        public void OnPrepareForLine(MarkupParseResult line, TMP_Text text);
        public void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text);
        public YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken);
        public void OnLineDisplayComplete();
        public void OnLineWillDismiss();
    }

    /// <summary>
    /// A <see cref="ActionMarkupHandler"/> is an object that reacts to the
    /// delivery of a line of dialogue, and can optionally control the timing of
    /// that delivery.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There are a number of cases where a line's delivery needs to have its
    /// timing controlled. For example, <see cref="PauseEventProcessor"/> adds a
    /// small delay between each character, creating a 'typewriter' effect as
    /// each letter appears over time.
    /// </para>
    /// <para>
    /// Another example of a <see cref="ActionMarkupHandler"/> is an in-line
    /// event or animation, such as causing a character to play an animation
    /// (and waiting for that animation to complete before displaying the rest
    /// of the line).
    /// </para>
    /// </remarks>
    public abstract class ActionMarkupHandler : MonoBehaviour, IActionMarkupHandler
    {
        /// <summary>
        /// Called when the line view receives the line, to prepare for showing
        /// the line.
        /// </summary>
        /// <remarks>
        /// This method is called before any part of the line is visible, and is
        /// an opportunity to set up any part of the <see
        /// cref="ActionMarkupHandler"/>'s display before the user can see it.
        /// </remarks>
        /// <param name="line">The line being presented.</param>
        /// <param name="text">A <see cref="TMP_Text"/> object that the line is
        /// being displayed in.</param>
        public abstract void OnPrepareForLine(MarkupParseResult line, TMP_Text text);

        /// <summary>
        /// Called immediately before the first character in the line is
        /// presented. 
        /// </summary>
        /// <param name="line">The line being presented.</param>
        /// <param name="text">A <see cref="TMP_Text"/> object that the line is
        /// being displayed in.</param>
        public abstract void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text);

        /// <summary>
        /// Called repeatedly for each visible character in the line.
        /// </summary>
        /// <remarks> This method is a <see cref="ActionMarkupHandler"/>
        /// object's main opportunity to take action during line
        /// display.</remarks>
        /// <param name="currentCharacterIndex">The zero-based index of the
        /// character being displayed.</param>
        /// <param name="text">A <see cref="TMP_Text"/> object that the line is
        /// being displayed in.</param>
        /// <param name="cancellationToken">A cancellation token representing
        /// whether the </param>
        /// <returns>A task that completes when the <see
        /// cref="ActionMarkupHandler"/> has completed presenting this
        /// character. Dialogue views will wait until this task is complete
        /// before displaying the remainder of the line.</returns>
        public abstract YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken);

        /// <summary>
        /// Called after the last call to <see cref="PresentCharacter(int,
        /// TMP_Text, CancellationToken)"/>.
        /// </summary>
        /// <remarks>This method is an opportunity for a <see
        /// cref="ActionMarkupHandler"/> to finalise its presentation after
        /// all of the characters in the line have been presented.</remarks>
        public abstract void OnLineDisplayComplete();

        /// <summary>
        /// Called right before the line will dismiss itself.
        /// </summary>
        public abstract void OnLineWillDismiss();
    }
}
