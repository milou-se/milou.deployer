using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Primitives;
using Milou.Deployer.Core.Deployment.Ftp;
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
            string? nuGetConfigFile = null,
            string? nuGetPackageSource = null,
            string? iisSiteName = null,
            bool isPreRelease = false,
            bool force = false,
            string? environmentConfig = null,
            string? publishSettingsFile = null,
            Dictionary<string, string[]>? parameters = null,
            string? excludedFilePatterns = null,
            bool requireEnvironmentConfig = false,
            string? publishType = null,
            string? webConfigTransformFile = null,
            string? ftpPath = null,
            string? nugetExePath = null,
            string? packageListPrefix = null,
            bool? packageListPrefixEnabled = null)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
            }

            if (!string.IsNullOrWhiteSpace(semanticVersion))
            {
                if (
                    !SemanticVersion.TryParse(semanticVersion,
                        out SemanticVersion parsedResultValue))
                {
                    throw new FormatException(
                        $"Could not parse a valid semantic version from string value '{semanticVersion}'");
                }

                SemanticVersion = parsedResultValue;
            }
            else
            {
                SemanticVersion = null;
            }

            ExcludedFilePatterns =
                excludedFilePatterns?.Split(';').Where(pattern => !string.IsNullOrWhiteSpace(pattern))
                    .ToImmutableArray() ?? ImmutableArray<string>.Empty;

            SetPreRelease(isPreRelease);

            FtpPath.TryParse(ftpPath, FileSystemType.Directory, out var path);
            PackageId = packageId;
            TargetDirectoryPath = targetDirectoryPath;
            FtpPath = path;
            NuGetConfigFile = nuGetConfigFile;
            NuGetPackageSource = nuGetPackageSource;
            IisSiteName = iisSiteName;
            IsPreRelease = SemanticVersion?.IsPrerelease ?? isPreRelease;
            Force = force;
            EnvironmentConfig = environmentConfig;
            PublishSettingsFile = publishSettingsFile;
            RequireEnvironmentConfig = requireEnvironmentConfig;
            WebConfigTransformFile = webConfigTransformFile;
            PackageListPrefix = packageListPrefix;
            PackageListPrefixEnabled = packageListPrefixEnabled;
            NugetExePath = nugetExePath;
            Parameters = parameters?.ToDictionary(pair => pair.Key,
                                 pair => new StringValues(pair.Value ?? Array.Empty<string>()))
                             .ToImmutableDictionary() ??
                         ImmutableDictionary<string, StringValues>.Empty;
            ExcludedFilePatternsCombined = excludedFilePatterns;

            PublishType = PublishType.TryParseOrDefault(publishType, out var publishTypeValue)
                ? publishTypeValue!
                : PublishType.Default;
        }

        public DeploymentExecutionDefinition(
            string packageId,
            string targetDirectoryPath,
            [CanBeNull] SemanticVersion semanticVersion,
            string? nuGetConfigFile = null,
            string? nuGetPackageSource = null,
            string? iisSiteName = null,
            bool isPreRelease = false,
            bool force = false,
            string? environmentConfig = null,
            string? publishSettingsFile = null,
            Dictionary<string, string[]>? parameters = null,
            string? excludedFilePatterns = null,
            bool requireEnvironmentConfig = false,
            string? webConfigTransformFile = null,
            string? publishType = null,
            string? ftpPath = null,
            string? nugetExePath = null,
            string? packageListPrefix = null,
            bool? packageListPrefixEnabled = null)
        {
            SemanticVersion = semanticVersion;
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
            }

            ExcludedFilePatterns =
                excludedFilePatterns?.Split(';').Where(pattern => !string.IsNullOrWhiteSpace(pattern))
                    .ToImmutableArray() ?? ImmutableArray<string>.Empty;

            PackageId = packageId;
            TargetDirectoryPath = targetDirectoryPath;
            NuGetConfigFile = nuGetConfigFile;
            NuGetPackageSource = nuGetPackageSource;
            IisSiteName = iisSiteName;
            IsPreRelease = SemanticVersion?.IsPrerelease ?? isPreRelease;
            Force = force;
            EnvironmentConfig = environmentConfig;
            PublishSettingsFile = publishSettingsFile;
            RequireEnvironmentConfig = requireEnvironmentConfig;
            WebConfigTransformFile = webConfigTransformFile;
            Parameters = parameters?.ToDictionary(pair => pair.Key,
                                 pair => new StringValues(pair.Value ?? Array.Empty<string>()))
                             .ToImmutableDictionary() ??
                         ImmutableDictionary<string, StringValues>.Empty;

            PublishType.TryParseOrDefault(publishType, out var publishTypeValue);
            FtpPath.TryParse(ftpPath, FileSystemType.Directory, out var path);
            FtpPath = path;

            PublishType = publishTypeValue ?? PublishType.Default;

            ExcludedFilePatternsCombined = excludedFilePatterns ?? "";

            NuGetExePath = nugetExePath ?? "";
            PackageListPrefix = packageListPrefix ?? "";
            PackageListPrefixEnabled = packageListPrefixEnabled;
        }

        [JsonIgnore]
        public ImmutableArray<string> ExcludedFilePatterns { get; }

        [JsonIgnore]
        public PublishType PublishType { get; }

        [JsonProperty(nameof(PublishType))]
        public string PublishTypeValue => PublishType.Name;

        [JsonProperty(PropertyName = nameof(ExcludedFilePatterns))]
        public string? ExcludedFilePatternsCombined { get; }

        public string? EnvironmentConfig { get; }

        public string? PublishSettingsFile { get; }

        public bool Force { get; }

        public string PackageId { get; }

        public string NuGetExePath { get; }

        public ImmutableDictionary<string, StringValues> Parameters { get; }

        [JsonIgnore]
        public SemanticVersion? SemanticVersion { get; }

        [JsonProperty(PropertyName = nameof(SemanticVersion))]
        [CanBeNull]
        public string NormalizedVersion =>
            SemanticVersion?.ToNormalizedString() ?? "";

        public string TargetDirectoryPath { get; }

        public bool IsPreRelease { get; private set; }

        [JsonIgnore]
        public string Version => SemanticVersion?.ToNormalizedString() ?? "{any version}";

        public bool RequireEnvironmentConfig { get; }

        public string? WebConfigTransformFile { get; }

        public string? PackageListPrefix { get; }

        public bool? PackageListPrefixEnabled { get; }

        public string? NugetExePath { get; }

        public string? IisSiteName { get; }

        public string? NuGetConfigFile { get; }

        public string? NuGetPackageSource { get; }

        [JsonProperty(nameof(FtpPath))]
        public string? FtpPathValue => FtpPath?.Path;

        [JsonIgnore]
        public FtpPath? FtpPath { get; }

        private void SetPreRelease(bool isPreRelease) => IsPreRelease = SemanticVersion?.IsPrerelease ?? isPreRelease;

        public override string ToString() => $"{PackageId} {Version} {TargetDirectoryPath} {EnvironmentConfig}";
    }
}