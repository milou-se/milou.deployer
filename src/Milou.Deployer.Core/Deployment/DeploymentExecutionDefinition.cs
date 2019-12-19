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
            string publishType = null,
            string webConfigTransformFile = null,
            string ftpPath = null)
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

            _ = FtpPath.TryParse(ftpPath, FileSystemType.Directory, out FtpPath path);
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
            Parameters = parameters?.ToDictionary(pair => pair.Key,
                                 pair => new StringValues(pair.Value ?? Array.Empty<string>()))
                             .ToImmutableDictionary() ??
                         ImmutableDictionary<string, StringValues>.Empty;
            ExcludedFilePatternsCombined = excludedFilePatterns;

            _ = PublishType.TryParseOrDefault(publishType, out PublishType publishTypeValue);

            PublishType = publishTypeValue;
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
            string webConfigTransformFile = null,
            string publishType = null,
            string ftpPath = null)
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

            _ = PublishType.TryParseOrDefault(publishType, out PublishType publishTypeValue);
            _ = FtpPath.TryParse(ftpPath, FileSystemType.Directory, out FtpPath path);
            FtpPath = path;

            PublishType = publishTypeValue;

            ExcludedFilePatternsCombined = excludedFilePatterns;
        }

        [JsonIgnore]
        public ImmutableArray<string> ExcludedFilePatterns { get; }

        [JsonIgnore]
        public PublishType PublishType { get; }

        [JsonProperty(nameof(PublishType))]
        public string PublishTypeValue => PublishType.Name;

        [JsonProperty(PropertyName = nameof(ExcludedFilePatterns))]
        public string ExcludedFilePatternsCombined { get; }

        public string EnvironmentConfig { get; }

        public string PublishSettingsFile { get; }

        public bool Force { get; }

        public string PackageId { get; }

        public ImmutableDictionary<string, StringValues> Parameters { get; }

        [JsonIgnore] [CanBeNull] public SemanticVersion SemanticVersion { get; }

        [JsonProperty(PropertyName = nameof(SemanticVersion))]
        [CanBeNull]
        public string NormalizedVersion =>
            SemanticVersion?.ToNormalizedString();

        public string TargetDirectoryPath { get; }

        public bool IsPreRelease { get; private set; }

        [JsonIgnore]
        public string Version => SemanticVersion?.ToNormalizedString() ?? "{any version}";

        public bool RequireEnvironmentConfig { get; }

        public string WebConfigTransformFile { get; }

        public string IisSiteName { get; }

        public string NuGetConfigFile { get; }

        public string NuGetPackageSource { get; }

        [JsonProperty(nameof(FtpPath))]
        public string FtpPathValue => FtpPath?.Path;

        [JsonIgnore]
        public FtpPath FtpPath { get; }

        private void SetPreRelease(bool isPreRelease) => IsPreRelease = SemanticVersion?.IsPrerelease ?? isPreRelease;

        public override string ToString() => $"{PackageId} {Version} {TargetDirectoryPath} {EnvironmentConfig}";
    }
}
