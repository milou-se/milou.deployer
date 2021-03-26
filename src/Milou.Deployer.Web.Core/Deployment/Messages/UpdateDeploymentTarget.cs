﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Ftp;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class UpdateDeploymentTarget : ICommand<UpdateDeploymentTargetResult>, IValidatableObject
    {
        public UpdateDeploymentTarget(
            DeploymentTargetId id,
            bool allowExplicitPreRelease,
            string? url,
            string packageId,
            string? iisSiteName = null,
            string? nugetPackageSource = null,
            string? nugetConfigFile = null,
            bool autoDeployEnabled = false,
            string? publishSettingsXml = null,
            string? targetDirectory = null,
            string? webConfigTransform = null,
            string? excludedFilePatterns = null,
            string? environmentTypeId = null,
            string? packageListTimeout = null,
            string? publishType = null,
            string? ftpPath = null,
            string? metadataTimeout = default,
            bool requireEnvironmentConfig = default,
            string? environmentConfiguration = null,
            bool packageListPrefixEnabled = false,
            string? packageListPrefix = null)
        {
            Id = id;
            AllowExplicitPreRelease = allowExplicitPreRelease;
            Uri.TryCreate(url, UriKind.Absolute, out var uri);
            Url = uri;
            PackageId = packageId?.Trim();
            ExcludedFilePatterns = excludedFilePatterns;
            PublishType.TryParseOrDefault(publishType, out var foundPublishType);
            FtpPath.TryParse(ftpPath, FileSystemType.Directory, out var path);
            PublishType = foundPublishType ?? PublishType.Default;
            FtpPath = path;
            IisSiteName = iisSiteName;
            NugetPackageSource = nugetPackageSource;
            NugetConfigFile = nugetConfigFile;
            AutoDeployEnabled = autoDeployEnabled;
            PublishSettingsXml = publishSettingsXml;
            TargetDirectory = targetDirectory;
            WebConfigTransform = webConfigTransform;
            IsValid = Id != DeploymentTargetId.Invalid;
            EnvironmentTypeId = environmentTypeId?.Trim();
            PackageListPrefix = packageListPrefix;

            if (TimeSpan.TryParse(packageListTimeout, out var timeout) && timeout.TotalSeconds > 0.5D)
            {
                PackageListTimeout = timeout;
            }

            if (TimeSpan.TryParse(metadataTimeout, out var parsedMetadataTimeout) &&
                parsedMetadataTimeout.TotalSeconds > 0.5D)
            {
                MetadataTimeout = parsedMetadataTimeout;
            }

            RequireEnvironmentConfig = requireEnvironmentConfig;
            EnvironmentConfiguration = environmentConfiguration;
            PackageListPrefixEnabled = packageListPrefixEnabled;
        }

        public TimeSpan? MetadataTimeout { get; }

        public string? EnvironmentTypeId { get; }

        public DeploymentTargetId Id { get; }

        public Uri? Url { get; }

        public bool AllowExplicitPreRelease { get; }

        public string? IisSiteName { get; }

        public string? NugetPackageSource { get; }

        public string? NugetConfigFile { get; }

        public bool AutoDeployEnabled { get; }

        public string? PublishSettingsXml { get; }

        public string? TargetDirectory { get; }

        public string? WebConfigTransform { get; }

        public string? PackageId { get; }

        public string? ExcludedFilePatterns { get; }

        public PublishType PublishType { get; }

        public FtpPath? FtpPath { get; }

        public bool IsValid { get; }

        public TimeSpan? PackageListTimeout { get; }

        public bool? RequireEnvironmentConfig { get; }

        public string? EnvironmentConfiguration { get; }

        public bool PackageListPrefixEnabled { get; }

        public string? PackageListPrefix { get; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Url is null)
            {
                yield return new ValidationResult("URL must be defined", new[] {nameof(Url)});
            }

            if (RequireEnvironmentConfig == true && string.IsNullOrWhiteSpace(EnvironmentConfiguration))
            {
                yield return new ValidationResult(
                    $"{nameof(RequireEnvironmentConfig)} can only be true when environment configuration is set",
                    new[] {nameof(RequireEnvironmentConfig)});
            }
        }

        public override string ToString() =>
            $"{nameof(Id)}: {Id}, {nameof(Url)}: {Url}, {nameof(AllowExplicitPreRelease)}: {AllowExplicitPreRelease}, {nameof(IisSiteName)}: {IisSiteName}, {nameof(NugetPackageSource)}: {NugetPackageSource}, {nameof(NugetConfigFile)}: {NugetConfigFile}, {nameof(AutoDeployEnabled)}: {AutoDeployEnabled}, {nameof(PublishSettingsXml)}: {PublishSettingsXml}, {nameof(TargetDirectory)}: {TargetDirectory}, {nameof(PackageId)}: {PackageId}, {nameof(IsValid)}: {IsValid}";
    }
}