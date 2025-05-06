#nullable enable

namespace Yarn.Unity.Editor
{
    internal static class AssetStoreSamplesInstaller
    {
        internal const string assetStoreURL = "https://assetstore.unity.com/packages/slug/319418";
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
