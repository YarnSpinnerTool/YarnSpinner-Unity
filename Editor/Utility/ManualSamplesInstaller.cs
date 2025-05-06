#nullable enable

namespace Yarn.Unity.Editor
{
    internal static class ManualSamplesInstaller
    {
        private const string manualInstallURL = "https://docs.yarnspinner.dev/next/yarn-spinner-for-game-engines/unity/installation-and-setup#installing-the-samples";
        internal static void InstallSamples()
        {
            if (YarnPackageImporter.IsSamplesPackageInstalled)
            {
                return;
            }
            UnityEngine.Application.OpenURL(manualInstallURL);
        }
    }
}
