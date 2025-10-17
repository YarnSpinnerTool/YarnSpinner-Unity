using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

#nullable enable

namespace Yarn.Unity.Editor
{
    internal static class UPMSamplesInstaller
    {
        private const string samplesPackageURL = "https://github.com/YarnSpinnerTool/YarnSpinner-Unity-Samples.git#current";
        private static AddRequest? installationRequest;

        private static void MonitorInstallation()
        {
            if (installationRequest == null)
            {
                return;
            }

            if (!installationRequest.IsCompleted)
            {
                return;
            }

            if (installationRequest.Status == StatusCode.Failure)
            {
                // it failed, log the error but don't clean up the installation
                // request as we need the error it has for determining the
                // status
                UnityEngine.Debug.LogError(installationRequest.Error);
            }
            else
            {
                // we succeeded, so we wipe out the request
                installationRequest = null;
            }
            // we remove ourselves from the update loop
            UnityEditor.EditorApplication.update -= MonitorInstallation;
        }

        internal static void InstallSamples()
        {
            switch (Status)
            {
                case YarnPackageImporter.SamplesPackageStatus.Installed:
                    {
                        // we already have it, ignoring this
                        break;
                    }
                case YarnPackageImporter.SamplesPackageStatus.NotInstalled:
                    {
                        // it's not installed so we need to request it
                        installationRequest = Client.Add(samplesPackageURL);
                        UnityEditor.EditorApplication.update += MonitorInstallation;
                        break;
                    }
                case YarnPackageImporter.SamplesPackageStatus.Installing:
                    {
                        // its in progress so just wait, jeez
                        break;
                    }
                case YarnPackageImporter.SamplesPackageStatus.FailedToInstall:
                    {
                        // it failed but that's fine, we can just go again!
                        installationRequest = Client.Add(samplesPackageURL);
                        UnityEditor.EditorApplication.update += MonitorInstallation;
                        break;
                    }
            }
        }

        internal static YarnPackageImporter.SamplesPackageStatus Status
        {
            get
            {
                // ok so first things first if the package is installed we can
                // say that
                if (YarnPackageImporter.IsSamplesPackageInstalled)
                {
                    return YarnPackageImporter.SamplesPackageStatus.Installed;
                }

                // we aren't installed but we could be one of:
                // - installing in progress
                // - failed while attempting an install
                // - not even attempted to install it
                if (installationRequest != null)
                {
                    // we might be in the process of installing
                    if (installationRequest.Status == StatusCode.InProgress)
                    {
                        return YarnPackageImporter.SamplesPackageStatus.Installing;
                    }

                    // at this point we must have had a failure, so we report
                    // that
                    if (installationRequest.Status == StatusCode.Failure)
                    {
                        return YarnPackageImporter.SamplesPackageStatus.FailedToInstall;
                    }
                }

                // at this point we simply aren't installed
                return YarnPackageImporter.SamplesPackageStatus.NotInstalled;
            }
        }
    }
}
