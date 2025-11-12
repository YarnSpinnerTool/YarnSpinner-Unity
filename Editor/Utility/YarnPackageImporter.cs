/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;

#nullable enable

namespace Yarn.Unity.Editor
{
    public class YarnPackageImporterException : Exception
    {
        public YarnPackageImporterException() { }

        public YarnPackageImporterException(string message)
            : base(message) { }

        public YarnPackageImporterException(string message, Exception inner)
            : base(message, inner) { }
    }

    public static partial class YarnPackageImporter
    {
        public enum SamplesPackageStatus
        {
            Installed, NotInstalled, Installing, FailedToInstall
        }

        private const string yarnSpinnerPackageName = "dev.yarnspinner.unity";
        private const string samplesPackageName = "dev.yarnspinner.unity.samples";

        public enum InstallApproach
        {
            Itch, AssetStore, Manual
        }

        // What is the status of the samples package?
        public static SamplesPackageStatus Status
        {
            get
            {
                // if we have the samples we don't really care about HOW, so
                // just return that
                if (IsSamplesPackageInstalled)
                {
                    return SamplesPackageStatus.Installed;
                }

                // each approach has their own approach for this, at this stage
                // only UPM can really do anything. so in that case we bounce
                // out to it, and for all others they are not installed later on
                // this will ideally change
#pragma warning disable 162
                switch (InstallationApproach)
                {
                    case InstallApproach.Manual:
                        {
                            return UPMSamplesInstaller.Status;
                        }
                }
                return SamplesPackageStatus.NotInstalled;
#pragma warning restore
            }
        }

        // install the samples package if not installed
        [UnityEditor.MenuItem("Window/Yarn Spinner/Install Samples Package", false)]
        internal static void InstallSamples()
        {
#pragma warning disable 162
            switch (InstallationApproach)
            {
                // there are two variants here
                case InstallApproach.Manual:
                    {
                        if (IsYarnSpinnerPackageInstalled)
                        {
                            // if the yarn spinner package is installed as a
                            // package we want to also install the samples this
                            // way
                            UPMSamplesInstaller.InstallSamples();
                        }
                        else
                        {
                            // otherwise it's a fully manually vendored version
                            // of YS
                            ManualSamplesInstaller.InstallSamples();
                        }
                        break;
                    }
                case InstallApproach.Itch:
                    {
                        ItchSamplesInstaller.InstallSamples();
                        break;
                    }
                case InstallApproach.AssetStore:
                    {
                        AssetStoreSamplesInstaller.InstallSamples();
                        break;
                    }
            }
#pragma warning restore
        }

        // open the samples up if they are installed
        private static void ShowSamples()
        {
            if (IsSamplesPackageInstalled)
            {
                Window.Open(samplesPackageName);
            }
        }

#if UNITY_2022_3_33_OR_NEWER
        static PackageInfo? GetInstalledPackageInfo(string packageName)
        {
            return PackageInfo.FindForPackageName(packageName);
        }
#else
        // prior to 2022.3.33f1 they didn't have a good way to get a specific selected package
        // so instead what we do is run through every installed package and see if it has the same name
        // if it does we return that
        // otherwise we return null.
        // In my testing this hasn't caused any issues but I am sure there are gaps I have missed
        // which I think is an acceptable tradeoff considering the age of <.33
        // and the failure state is that instead of opening the samples we open the docs
        // which feels ok to me as a fallback.
        static PackageInfo? GetInstalledPackageInfo(string packageName)
        {
            var allPackages = PackageInfo.GetAllRegisteredPackages();
            foreach (var package in allPackages)
            {
                if (package.name.ToLower() == packageName.ToLower())
                {
                    return package;
                }
            }
            return null;
        }
#endif

        static IEnumerable<Sample> GetSamplesForInstalledPackage(PackageInfo package)
        {
            return Sample.FindByPackage(package.name, package.version);
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

        public static IEnumerable<Sample> GetSamplesPackageSamples()
        {
            if (SamplesPackageInfo != null)
            {
                return GetSamplesForInstalledPackage(SamplesPackageInfo);
            }
            return new List<Sample>();
        }

        public static void OpenSamplesUI()
        {
            if (!IsSamplesPackageInstalled)
            {
                return;
            }
            ShowSamples();
        }
    }
}
