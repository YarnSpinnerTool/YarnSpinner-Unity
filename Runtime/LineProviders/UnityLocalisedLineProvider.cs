/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

#nullable enable

namespace Yarn.Unity.UnityLocalization
{
    // This partial declaration exists to ensure that Unity is able to work with
    // this class, because MonoBehaviour types are required to be defined in a
    // file whose name matches the type's name. The actual definition of
    // UnityLocalisedLineProvider is found in either
    // UnityLocalisedLineProvider.Installed.cs (if Unity Localization is
    // installed), or UnityLocalisedLineProvider.NotInstalled.cs (if not).
    public sealed partial class UnityLocalisedLineProvider : LineProviderBehaviour { }
}

