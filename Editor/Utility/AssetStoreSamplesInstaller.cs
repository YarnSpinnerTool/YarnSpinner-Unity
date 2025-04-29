#nullable enable

namespace Yarn.Unity.Editor
{
    internal static class AssetStoreSamplesInstaller
    {
        internal const string assetStoreURL = "https://assetstore.unity.com/packages/tools/behavior-ai/yarn-spinner-for-unity-267061";
        internal static void InstallSamples()
        {
            if (YarnPackageImporter.IsSamplesPackageInstalled)
            {
                return;
            }
            UnityEngine.Application.OpenURL(assetStoreURL);
        }
    }
}