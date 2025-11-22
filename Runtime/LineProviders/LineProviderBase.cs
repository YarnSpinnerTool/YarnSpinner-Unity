/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Yarn.Markup;

#nullable enable

namespace Yarn.Unity
{
    /// <summary>
    /// Contains methods for retrieving user-facing localized content, given
    /// non-localized line IDs.
    /// </summary>
    public interface ILineProvider
    {
        /// <summary>
        /// The <see cref="YarnProject"/> that contains the localized data for
        /// lines.
        /// </summary>
        /// <remarks>
        /// This property is set at run-time by the object that will be requesting
        /// content (typically a <see cref="DialogueRunner"/>).
        /// </remarks>
        public YarnProject? YarnProject { get; set; }

        /// <summary>
        /// Gets the line provider's current locale identifier, as a BCP-47 code.
        /// </summary>
        public string LocaleCode { get; }

        /// <summary>
        /// Prepares and returns a <see cref="LocalizedLine"/> from the specified
        /// <see cref="Yarn.Line"/>.
        /// </summary>
        /// <param name="line">The <see cref="Yarn.Line"/> to produce the <see
        /// cref="LocalizedLine"/> from.</param>
        /// <param name="cancellationToken">A cancellation token that indicates
        /// whether the process of fetching the localised version of <paramref
        /// name="line"/> should be cancelled.</param>
        /// <returns>A localized line, ready to be presented to the
        /// player.</returns>
        public YarnTask<LocalizedLine> GetLocalizedLineAsync(Line line, CancellationToken cancellationToken);

        /// <summary>
        /// Signals to the line provider that lines with the provided line IDs may
        /// be presented shortly.        
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method allows implementing classes a chance to prepare any
        /// neccessary resources needed to present these lines, like pre-loading
        /// voice-over audio. The default implementation does nothing.
        /// </para>
        /// <para style="info">
        /// Not every line may run; this method serves as a way to give the line
        /// provider advance notice that a line <i>may</i> run, not <i>will</i> run.
        /// </para>
        /// </remarks>
        /// <param name="lineIDs">A collection of line IDs that the line provider
        /// should prepare for.</param>
        /// <param name="cancellationToken">A cancellation token that indicates
        /// whether the operation should be cancelled.</param>
        public YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a new marker processor to the line provider.
        /// </summary>
        /// <param name="attributeName">The name of the markers to use <paramref
        /// name="markerProcessor"/> for.</param>
        /// <param name="markerProcessor">The marker processor to add.</param>
        public void RegisterMarkerProcessor(string attributeName, Yarn.Markup.IAttributeMarkerProcessor markerProcessor);

        /// <summary>
        /// Removes all marker processors that handle markers named <paramref
        /// name="attributeName"/>.
        /// </summary>
        /// <param name="attributeName">The name of the marker to remove processors
        /// for.</param>
        public void DeregisterMarkerProcessor(string attributeName);
    }

    /// <summary>
    /// A <see cref="MonoBehaviour"/> that produces <see
    /// cref="LocalizedLine"/>s, for use in Dialogue Presenters.
    /// </summary>
    /// <remarks>
    /// <see cref="DialogueRunner"/>s use a <see cref="LineProviderBehaviour"/>
    /// to get <see cref="LocalizedLine"/>s, which contain the localized
    /// information that is presented to the player through dialogue presenters.
    /// </remarks>
    public abstract class LineProviderBehaviour : MonoBehaviour, ILineProvider
    {
        /// <inheritdoc/>
        public abstract YarnTask<LocalizedLine> GetLocalizedLineAsync(Line line, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public YarnProject? YarnProject { get; set; }

        /// <inheritdoc/>
        public virtual YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            // No-op by default.
            return YarnTask.CompletedTask;
        }

        /// <inheritdoc/>
        public abstract string LocaleCode { get; set; }

        /// <summary>
        /// Called by Unity when the <see cref="LineProviderBehaviour"/> has
        /// first appeared in the scene.
        /// </summary>
        /// <remarks>
        /// This method is <see langword="public"/> <see langword="virtual"/> to
        /// allow subclasses to override it.
        /// </remarks>
        public virtual void Start()
        {
        }

        /// <inheritdoc/>
        public abstract void RegisterMarkerProcessor(string attributeName, IAttributeMarkerProcessor markerProcessor);

        /// <inheritdoc/>
        public abstract void DeregisterMarkerProcessor(string attributeName);
    }

}
