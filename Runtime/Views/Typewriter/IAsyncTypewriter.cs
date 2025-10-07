#nullable enable

namespace Yarn.Unity
{
    using System.Threading;
    using System.Collections.Generic;

    /// <summary>
    /// An object that can handle delivery of a line's text over time.
    /// </summary>
    public interface IAsyncTypewriter
    {
        /// <summary>
        /// Displays the contents of a line over time.
        /// </summary>
        /// <remarks>
        /// <para>This method is called when a dialogue presenter wants to
        /// deliver a line's text. The typewriter should present the text to the
        /// user; it may take as long as it needs to do so. </para>
        ///
        /// <para>If <paramref name="cancellationToken"/>'s <see
        /// cref="CancellationToken.IsCancellationRequested"/> becomes true, the
        /// typewriter effect should end early and present the entire contents
        /// of <paramref name="line"/>.</para>
        /// </remarks>
        /// <param name="line">The line to display.</param>
        /// <param name="cancellationToken">A token that indicates that the
        /// typewriter effect should be cancelled.</param>
        /// <returns>A task that completes when the typewriter effect has
        /// finished.</returns>
        public YarnTask RunTypewriter(Markup.MarkupParseResult line, CancellationToken cancellationToken);

        /// <summary>
        /// Called by the presenter before content has been shown.
        /// This gives the typewriter it's chance to do any setup before the content is visibly shown.
        /// </summary>
        /// <param name="line">The content of the line or option that is about to be shown</param>
        public void PrepareForContent(Markup.MarkupParseResult line);

        /// <summary>
        /// Called right before the content will be visibly hidden
        /// </summary>
        public void ContentWillDismiss();

        /// <summary>
        /// The list of action markup handlers that this typewriter should call out to while typewriting.
        /// </summary>
        public List<IActionMarkupHandler> ActionMarkupHandlers { get; }
    }
}
