﻿using System;
using NuGet.Versioning;

namespace Milou.Deployer.Core.Deployment
{
    public class InstalledPackage
    {
        public InstalledPackage(string packageId, SemanticVersion version, string nugetPackageFullPath)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException("Argument is null or whitespace", nameof(packageId));
            }

            if (string.IsNullOrWhiteSpace(nugetPackageFullPath))
            {
                throw new ArgumentException("Argument is null or whitespace", nameof(nugetPackageFullPath));
            }

            NugetPackageFullPath = nugetPackageFullPath;
            PackageId = packageId;
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        private InstalledPackage()
        {
        }

        public string NugetPackageFullPath { get; }

        public string PackageId { get; }

        public SemanticVersion Version { get; }

        public static InstalledPackage None => new InstalledPackage();

        public static InstalledPackage Create(string packageId, SemanticVersion version, string nugetPackageFullPath)
        {
            return new InstalledPackage(packageId, version, nugetPackageFullPath);
        }
    }
}
