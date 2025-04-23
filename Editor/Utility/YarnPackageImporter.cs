/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;

#nullable enable

namespace Yarn.Unity.Editor
{
    public class YarnPackageImporterException : Exception
    {
        public YarnPackageImporterException(){}

        public YarnPackageImporterException(string message)
            : base(message){}

        public YarnPackageImporterException(string message, Exception inner)
            : base(message, inner) {}
    }

    public static class YarnPackageImporter
    {
        private const string yarnSpinnerPackageName = "dev.yarnspinner.unity";
        private const string samplesPackageName = "dev.yarnspinner.unity.samples";
        private const string samplesPackageURL = "https://github.com/YarnSpinnerTool/YarnSpinner-Unity-Samples.git";

        internal const string manualInstallURL = "https://docs.yarnspinner.dev/next/yarn-spinner-for-game-engines/unity/installation-and-setup#installing-the-samples";
        internal const string assetStoreURL = "https://assetstore.unity.com/packages/tools/behavior-ai/yarn-spinner-for-unity-267061";
        internal const string itchURL = "https://yarnspinner.itch.io/yarn-spinner";
        internal enum InstallApproach
        {
            Itch, AssetStore, Manual
        }
        internal static InstallApproach installation = InstallApproach.Manual;

        static PackageInfo? GetInstalledPackageInfo(string packageName)
        {
            return PackageInfo.FindForPackageName(packageName);
        }

        private static AddRequest? InstallPackage(string packageName, string? packageVersion=null)
        {
            PackageInfo? installed = GetInstalledPackageInfo(packageName);
            if (installed != null)
            {
                if (packageVersion != null && packageVersion != installed.version)
                { 
                    throw new YarnPackageImporterException($"{packageName} package already installed with incompatible version: {installed.version}"); 
                }
            }
            string version = (packageVersion == null) ? "" : $"@{packageVersion}";
            var packageRequest = Client.Add($"{packageName}{version}");
            return packageRequest;
        }

        static IEnumerable<Sample> GetSamplesForInstalledPackage(PackageInfo package)
        {
            return Sample.FindByPackage(package.name, package.version);
        }

        private static void InstallPackageSamples(string packageName)
        {
            PackageInfo? packageInfo = GetInstalledPackageInfo(packageName);
            if (packageInfo == null)
            {
                throw new YarnPackageImporterException($"{packageName} is not installed, unable to install samples without it being installed.");
            }
            InstallPackageSamples(packageInfo);
        }

        private static void InstallPackageSamples(PackageInfo package)
        {
            IEnumerable<Sample> samples = GetSamplesForInstalledPackage(package);
            InstallPackageSamples(samples);
        }
        
        private static void InstallPackageSamples(IEnumerable<Sample> samples)
        {
            List<Sample> failedSamples = new();
            foreach (Sample sample in samples)
            {
                if (!sample.isImported && !sample.Import())
                {
                    failedSamples.Add(sample);
                }
            }
            if (failedSamples.Any())
            {
                throw new YarnPackageImporterException($"{failedSamples.Count} samples failed to install.");
            }
        }

        // MARK: Yarn Spinner Package

        public static PackageInfo? YarnSpinnerPackageInfo
        {
            get
            {
                return GetInstalledPackageInfo(yarnSpinnerPackageName);
            } 
        }

        public static string? YarnSpinnerPackageVersion
        {
            get
            {
                PackageInfo? yarnspinnerPackage = YarnSpinnerPackageInfo;
                return yarnspinnerPackage?.version;
            }
        }

        public static bool IsYarnSpinnerPackageInstalled
        {
            get
            {
                return YarnSpinnerPackageInfo != null;
            }
        }

        // MARK: Samples Package

        public static PackageInfo? SamplesPackageInfo
        {
            get
            {
                return GetInstalledPackageInfo(samplesPackageName);
            } 
        }

        public static string? SamplesPackageVersion
        {
            get
            {
                PackageInfo? samplesPackage = SamplesPackageInfo;
                return samplesPackage?.version;
            }
        }

        public static bool IsSamplesPackageInstalled
        {
            get
            {
                return SamplesPackageInfo != null;
            }
        }

        [UnityEditor.MenuItem("Window/Yarn Spinner/Install Samples Package", true)]
        public static bool InstallSamplesPackageValidation()
        {
            return !IsSamplesPackageInstalled;
        }

        [UnityEditor.MenuItem("Window/Yarn Spinner/Install Samples Package", false)]
        internal static void QuickInstallSamples()
        {
            switch (installation)
            {
                case InstallApproach.Itch:
                {
                    UnityEngine.Application.OpenURL(YarnPackageImporter.itchURL);
                    break;
                }
                case InstallApproach.AssetStore:
                {
                    UnityEngine.Application.OpenURL(YarnPackageImporter.assetStoreURL);
                    break;
                }
                case InstallApproach.Manual:
                {
                    InstallSamplesPackage();
                    break;
                }
            }
        }

        public static AddRequest? InstallSamplesPackage()
        {
            if (IsSamplesPackageInstalled)
            {
                return null;
            }

            return InstallPackage(samplesPackageURL);
        }

        public static IEnumerable<Sample> GetSamplesPackageSamples()
        {
            if (SamplesPackageInfo != null)
            {
                return GetSamplesForInstalledPackage(SamplesPackageInfo);
            }
            return new List<Sample>();
        }

        public static void InstallSamplesPackageSamples()
        {
            if (!IsSamplesPackageInstalled)
            {
                return;
            }

            InstallPackageSamples(samplesPackageName);
        }

        public static void OpenSamplesUI()
        {
            if (!IsSamplesPackageInstalled)
            {
                return;
            }
            Window.Open("dev.yarnspinner.unity.samples");
        }
    }
}
