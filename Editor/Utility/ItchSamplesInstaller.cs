#nullable enable

namespace Yarn.Unity.Editor
{
    internal static class ItchSamplesInstaller
    {
        internal const string itchURL = "https://yarnspinner.itch.io/yarn-spinner";
        internal static void InstallSamples()
        {
            if (YarnPackageImporter.IsSamplesPackageInstalled)
            {
                return;
            }
            UnityEngine.Application.OpenURL(itchURL);
        }
    }
}
