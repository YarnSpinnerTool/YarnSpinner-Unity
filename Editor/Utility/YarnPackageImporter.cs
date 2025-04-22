/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.PackageManager;
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

        static PackageInfo? GetInstalledPackageInfo(string packageName)
        {
            return PackageInfo.FindForPackageName(packageName);
        }

        private static void InstallPackage(string packageName, string? packageVersion=null)
        {
            PackageInfo? installed = GetInstalledPackageInfo(packageName);
            if (installed != null)
            {
                if (packageVersion != null && packageVersion != installed.version)
                { 
                    throw new YarnPackageImporterException($"{packageName} package already installed with incompatible version: {installed.version}"); 
                }
                return;
            }
            string version = (packageVersion == null) ? "" : $"@{packageVersion}";
            var packageRequest = Client.Add($"{packageName}{version}");
            while (!packageRequest.IsCompleted){}
            if (packageRequest.Error != null)
            {
                throw new YarnPackageImporterException(packageRequest.Error.message);
            }
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
                InstallPackage(packageName);
                packageInfo = GetInstalledPackageInfo(packageName);
                // should not be able to be null here 
                // because if install failed then it should have
                // thrown error out of this function by now
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
            get {
                return GetInstalledPackageInfo(yarnSpinnerPackageName);
            } 
        }

        public static string? YarnSpinnerPackageVersion
        {
            get {
                PackageInfo? yarnspinnerPackage = YarnSpinnerPackageInfo;
                return (yarnspinnerPackage == null) ? null : yarnspinnerPackage.version;
            }
        }

        public static bool IsYarnSpinnerPackageInstalled
        {
            get {
                return YarnSpinnerPackageInfo != null;
            }
        }

        // MARK: Samples Package

        public static PackageInfo? SamplesPackageInfo
        {
            get {
                return GetInstalledPackageInfo(samplesPackageName);
            } 
        }

        public static string? SamplesPackageVersion
        {
            get {
                PackageInfo? samplesPackage = SamplesPackageInfo;
                return (samplesPackage == null) ? null : samplesPackage.version;
            }
        }

        public static bool IsSamplesPackageInstalled
        {
            get {
                return SamplesPackageInfo != null;
            }
        }

        public static void InstallSamplesPackage()
        {
            if (!IsSamplesPackageInstalled)
            {
                // if YarnSpinner is not installed, the version
                // will be null and latest will be installed
                InstallPackage(samplesPackageName, YarnSpinnerPackageVersion);
            }
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
                InstallSamplesPackage();
            }
            InstallPackageSamples(samplesPackageName);
        }
    }
}
