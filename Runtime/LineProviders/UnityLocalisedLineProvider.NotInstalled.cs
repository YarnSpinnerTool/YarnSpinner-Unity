/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#if !USE_UNITY_LOCALIZATION
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Yarn.Unity.UnityLocalization
{
    /// <summary>
    /// A line provider that uses the Unity Localization system to get localized
    /// content for Yarn lines.
    /// </summary>
    public partial class UnityLocalisedLineProvider : LineProviderBehaviour
    {
        /// <inheritdoc/>
        public override string LocaleCode { get => "error"; set { } }

        private const string NotInstalledError = nameof(UnityLocalisedLineProvider) + "requires that the Unity Localization package is installed in the project. To fix this, install Unity Localization.";

        /// <inheritdoc/>
        public override YarnTask PrepareForLinesAsync(IEnumerable<string> lineIDs, CancellationToken cancellationToken)
        {
            Debug.LogError(NotInstalledError);
            return YarnTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override void Start()
        {
            Debug.LogError(NotInstalledError);
        }

        /// <inheritdoc/>
        public override YarnTask<LocalizedLine> GetLocalizedLineAsync(Yarn.Line line, CancellationToken cancellationToken)
        {
            Debug.LogError($"{nameof(UnityLocalisedLineProvider)}: Can't create a localised line for ID {line.ID} because the Unity Localization package is not installed in this project. To fix this, install Unity Localization.");

            return YarnTask.FromResult(new LocalizedLine()
            {
                TextID = line.ID,
                RawText = $"{line.ID}: Unable to create a localised line, because the Unity Localization package is not installed in this project.",
                Substitutions = line.Substitutions,
            });
        }

        /// <inheritdoc/>
        public override void RegisterMarkerProcessor(string attributeName, Markup.IAttributeMarkerProcessor markerProcessor)
        {
            Debug.LogWarning($"Unable to add a marker processor for {attributeName}, as the Unity Localization package is not installed in this project");
        }

        /// <inheritdoc/>
        public override void DeregisterMarkerProcessor(string attributeName)
        {
            Debug.LogWarning($"Unable to remove a marker processor for {attributeName}, as the Unity Localization package is not installed in this project");
        }
    }

}
#endif
