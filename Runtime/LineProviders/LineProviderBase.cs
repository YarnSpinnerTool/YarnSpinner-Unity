/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using UnityEngine;


using System.Threading;
using Yarn.Unity;
using Yarn;
using Yarn.Markup;


#if USE_UNITASK
    using Cysharp.Threading.Tasks;
    using YarnTask = Cysharp.Threading.Tasks.UniTask;
    using YarnIntTask = Cysharp.Threading.Tasks.UniTask<int>;
    using YarnLineTask = Cysharp.Threading.Tasks.UniTask<Yarn.Unity.LocalizedLine>;
#else
using YarnTask = System.Threading.Tasks.Task;
    using YarnLineTask = System.Threading.Tasks.Task<Yarn.Unity.LocalizedLine>;
#endif

#nullable enable

public interface ILineProvider
{
    public YarnProject? YarnProject { get; set; }
    public string LocaleCode { get; }
    public YarnLineTask GetLocalizedLineAsync(Line line, CancellationToken cancellationToken);
    public YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken);

    public void RegisterMarkerProcessor(string attributeName, Yarn.Markup.IAttributeMarkerProcessor markerProcessor);
    public void DeregisterMarkerProcessor(string attributeName);
}


namespace Yarn.Unity
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> that produces <see
    /// cref="LocalizedLine"/>s, for use in Dialogue Views.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="DialogueRunner"/>s use a <see
    /// cref="LineProviderBehaviour"/> to get <see cref="LocalizedLine"/>s,
    /// which contain the localized information that <see
    /// cref="DialogueViewBase"/> classes use to present content to the
    /// player. 
    /// </para>
    /// <para>
    /// Subclasses of this abstract class may return subclasses of <see
    /// cref="LocalizedLine"/>. For example, <see
    /// cref="BuiltinLocalisedLineProvider"/> returns an <see
    /// cref="AudioLocalizedLine"/>, which includes <see
    /// cref="AudioClip"/>; views that make use of audio can then access
    /// this additional data.
    /// </para>
    /// </remarks>
    /// <seealso cref="DialogueViewBase"/>
    public abstract class LineProviderBehaviour : MonoBehaviour, ILineProvider
    {
        /// <summary>
        /// Prepares and returns a <see cref="LocalizedLine"/> from the
        /// specified <see cref="Yarn.Line"/>.
        /// </summary>
        /// <remarks>
        /// This method should not be called if <see
        /// cref="LinesAvailable"/> returns <see langword="false"/>.
        /// </remarks>
        /// <param name="line">The <see cref="Yarn.Line"/> to produce the
        /// <see cref="LocalizedLine"/> from.</param>
        /// <returns>A localized line, ready to be presented to the
        /// player.</returns>
        public abstract YarnLineTask GetLocalizedLineAsync(Line line, CancellationToken cancellationToken);

        /// <summary>
        /// The YarnProject that contains the localized data for lines.
        /// </summary>
        /// <remarks>This property is set at run-time by the object that
        /// will be requesting content (typically a <see
        /// cref="DialogueRunner"/>).
        public YarnProject? YarnProject { get; set; }

        /// <summary>
        /// Signals to the line provider that lines with the provided line
        /// IDs may be presented shortly.        
        /// </summary>
        /// <remarks>
        /// <para>
        /// Subclasses of <see cref="LineProviderBehaviour"/> can override
        /// this to prepare any neccessary resources needed to present
        /// these lines, like pre-loading voice-over audio. The default
        /// implementation does nothing.
        /// </para>
        /// <para style="info">
        /// Not every line may run; this method serves as a way to give the
        /// line provider advance notice that a line <i>may</i> run, not <i>will</i>
        /// run.
        /// </para>
        /// <para>
        /// When this method is run, the value returned by the <see
        /// cref="LinesAvailable"/> property should change to false until the
        /// necessary resources have loaded.
        /// </para>
        /// </remarks>
        /// <param name="lineIDs">A collection of line IDs that the line
        /// provider should prepare for.</param>
        public virtual YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            // No-op by default.
            return YarnTask.CompletedTask;
        }

        public virtual bool LinesAvailable => true;

        /// <summary>
        /// Gets the user's current locale identifier, as a BCP-47 code.
        /// </summary>
        /// <remarks>
        /// This value is used by the <see cref="DialogueRunner"/> to control
        /// how certain replacement markers behave (for example, the
        /// <c>[plural]</c> marker, which behaves differently depending on the
        /// user's locale.)
        /// </remarks>
        public abstract string LocaleCode { get; set; }

        /// <summary>
        /// Called by Unity when the <see cref="LineProviderBehaviour"/>
        /// has first appeared in the scene.
        /// </summary>
        /// <remarks>
        /// This method is <see langword="public"/> <see
        /// langword="virtual"/> to allow subclasses to override it.
        /// </remarks>
        public virtual void Start()
        {
        }

        public abstract void RegisterMarkerProcessor(string attributeName, IAttributeMarkerProcessor markerProcessor);
        public abstract void DeregisterMarkerProcessor(string attributeName);
    }

}
