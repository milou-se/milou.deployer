using System;
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

        public static InstalledPackage None => new();

        public string NugetPackageFullPath { get; }

        public string PackageId { get; }

        public SemanticVersion Version { get; }

        public static InstalledPackage Create(string packageId, SemanticVersion version, string nugetPackageFullPath) =>
            new(packageId, version, nugetPackageFullPath);
    }
}