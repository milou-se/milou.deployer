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
            string nuGetPackageSource = null,
            string iisSiteName = null,
            bool isPreRelease = false,
            bool force = false,
            string environmentConfig = null,
            string publishSettingsFile = null,
            Dictionary<string, string[]> parameters = null,
            string excludedFilePatterns = null,
            bool requireEnvironmentConfig = false,
            string webConfigTransformFile = null)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
            }

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
            WebConfigTransformFile = webConfigTransformFile;
            Parameters = parameters?.ToDictionary(pair => pair.Key,
                                 pair => new StringValues(pair.Value ?? Array.Empty<string>()))
                             .ToImmutableDictionary() ??
                         ImmutableDictionary<string, StringValues>.Empty;
            ExcludedFilePatternsCombined = excludedFilePatterns;
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
            bool requireEnvironmentConfig = false,
            string webConfigTransformFile = null)
        {
            SemanticVersion = semanticVersion ?? MayBe<SemanticVersion>.Nothing;
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
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
            WebConfigTransformFile = webConfigTransformFile;
            Parameters = parameters?.ToDictionary(pair => pair.Key,
                                 pair => new StringValues(pair.Value ?? Array.Empty<string>()))
                             .ToImmutableDictionary() ??
                         ImmutableDictionary<string, StringValues>.Empty;
            ExcludedFilePatternsCombined = excludedFilePatterns;
        }

        [JsonIgnore]
        public ImmutableArray<string> ExcludedFilePatterns { get; }

        [JsonProperty(PropertyName = nameof(ExcludedFilePatterns))]
        public string ExcludedFilePatternsCombined { get; }

        public string EnvironmentConfig { get; }

        public string PublishSettingsFile { get; }

        public bool Force { get; }

        public string PackageId { get; }

        public ImmutableDictionary<string, StringValues> Parameters { get; }

        [JsonIgnore]
        public MayBe<SemanticVersion> SemanticVersion { get; }

        [JsonProperty(PropertyName = nameof(SemanticVersion))]
        public string NormalizedVersion =>
            SemanticVersion.HasValue ? SemanticVersion.Value.ToNormalizedString(): null;

        public string TargetDirectoryPath { get; }

        public bool IsPreRelease { get; private set; }

        [JsonIgnore]
        public string Version => SemanticVersion.HasValue
            ? SemanticVersion.Value.ToNormalizedString()
            : "{any version}";

        public bool RequireEnvironmentConfig { get; }

        public string WebConfigTransformFile { get; }

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
