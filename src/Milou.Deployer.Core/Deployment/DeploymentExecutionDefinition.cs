using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using NuGet.Versioning;

namespace Milou.Deployer.Core.Deployment
{
    public class DeploymentExecutionDefinition
    {
        [JsonConstructor]
        [UsedImplicitly]
        private DeploymentExecutionDefinition(
            string packageId,
            string semanticVersion,
            string targetDirectoryPath,
            string nuGetConfigFile = null,
            string nuGetPackagePackageSource = null,
            string iisSiteName = null,
            bool isPreRelease = false,
            bool force = false,
            string environmentConfig = null,
            string publishSettingsFile = null,
            Dictionary<string, string[]> parameters = null,
            string excludedFilePatterns = null,
            bool requireEnvironmentConfig = false)
        {
            SemanticVersion version = null;

            if (!string.IsNullOrWhiteSpace(semanticVersion))
            {
                if (
                    !global::NuGet.Versioning.SemanticVersion.TryParse(semanticVersion,
                        out SemanticVersion parsedResultValue))
                {
                    throw new FormatException(
                        $"Could not parse a valid semantic version from string value '{semanticVersion}'");
                }

                version = parsedResultValue;
            }

            ExcludedFilePatterns = excludedFilePatterns?.Split(';').ToImmutableArray() ?? ImmutableArray<string>.Empty;

            SemanticVersion = new MayBe<SemanticVersion>(version);

            SetPreRelease(isPreRelease);

            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
            }

            if (string.IsNullOrWhiteSpace(targetDirectoryPath))
            {
                throw new ArgumentNullException(nameof(targetDirectoryPath));
            }

            PackageId = packageId;
            TargetDirectoryPath = targetDirectoryPath;
            NuGetConfigFile = nuGetConfigFile;
            NuGetPackageSource = nuGetPackagePackageSource;
            IisSiteName = iisSiteName;
            IsPreRelease = SemanticVersion.HasValue ? SemanticVersion.Value.IsPrerelease : isPreRelease;
            Force = force;
            EnvironmentConfig = environmentConfig;
            PublishSettingsFile = publishSettingsFile;
            RequireEnvironmentConfig = requireEnvironmentConfig;
            Parameters = parameters?.ToDictionary(pair => pair.Key,
                                 pair => new StringValues(pair.Value ?? Array.Empty<string>()))
                             .ToImmutableDictionary() ??
                         ImmutableDictionary<string, StringValues>.Empty;
        }

        public DeploymentExecutionDefinition(
            string packageId,
            string targetDirectoryPath,
            MayBe<SemanticVersion> semanticVersion,
            string nuGetConfigFile = null,
            string nuGetPackageSource = null,
            string iisSiteName = null,
            bool isPreRelease = false,
            bool force = false,
            string environmentConfig = null,
            string publishSettingsFile = null,
            Dictionary<string, string[]> parameters = null,
            string excludedFilePatterns = null,
            bool requireEnvironmentConfig = false)
        {
            SemanticVersion = semanticVersion ?? MayBe<SemanticVersion>.Nothing;
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
            }

            if (string.IsNullOrWhiteSpace(targetDirectoryPath))
            {
                throw new ArgumentNullException(nameof(targetDirectoryPath));
            }

            ExcludedFilePatterns = excludedFilePatterns?.Split(';').ToImmutableArray() ?? ImmutableArray<string>.Empty;

            PackageId = packageId;
            TargetDirectoryPath = targetDirectoryPath;
            NuGetConfigFile = nuGetConfigFile;
            NuGetPackageSource = nuGetPackageSource;
            IisSiteName = iisSiteName;
            IsPreRelease = SemanticVersion.HasValue ? SemanticVersion.Value.IsPrerelease : isPreRelease;
            Force = force;
            EnvironmentConfig = environmentConfig;
            PublishSettingsFile = publishSettingsFile;
            RequireEnvironmentConfig = requireEnvironmentConfig;
            Parameters = parameters?.ToDictionary(pair => pair.Key,
                                 pair => new StringValues(pair.Value ?? Array.Empty<string>()))
                             .ToImmutableDictionary() ??
                         ImmutableDictionary<string, StringValues>.Empty;
        }

        public ImmutableArray<string> ExcludedFilePatterns { get; }

        public string EnvironmentConfig { get; }

        public string PublishSettingsFile { get; }

        public bool Force { get; }

        public string PackageId { get; }

        public ImmutableDictionary<string, StringValues> Parameters { get; }

        public MayBe<SemanticVersion> SemanticVersion { get; }

        public string TargetDirectoryPath { get; }

        public bool IsPreRelease { get; private set; }

        public string Version => SemanticVersion.HasValue
            ? SemanticVersion.Value.ToNormalizedString()
            : "{any version}";

        public bool RequireEnvironmentConfig { get; }

        public string IisSiteName { get; }

        public string NuGetConfigFile { get; }

        public string NuGetPackageSource { get; }

        public override string ToString()
        {
            return $"{PackageId} {Version} {TargetDirectoryPath} {EnvironmentConfig}";
        }

        private void SetPreRelease(bool isPreRelease)
        {
            IsPreRelease = SemanticVersion.HasValue ? SemanticVersion.Value.IsPrerelease : isPreRelease;
        }
    }
}
