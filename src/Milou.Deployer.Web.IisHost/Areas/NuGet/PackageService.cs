using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions;
using Arbor.App.Extensions.Configuration;
using Arbor.KVConfiguration.Core;
using Arbor.Tooler;
using JetBrains.Annotations;
using Milou.Deployer.Web.Core;
using Milou.Deployer.Web.Core.Deployment.Packages;
using Milou.Deployer.Web.Core.NuGet;
using Serilog;
using Serilog.Events;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Milou.Deployer.Web.IisHost.Areas.NuGet
{
    [UsedImplicitly]
    public class PackageService : IPackageService
    {
        private readonly NuGetListConfiguration _deploymentConfiguration;

        [NotNull]
        private readonly IKeyValueConfiguration _keyValueConfiguration;

        private readonly ILogger _logger;

        private readonly NuGetConfiguration _nuGetConfiguration;

        private readonly NuGetPackageInstaller _packageInstaller;

        public PackageService(
            [NotNull] NuGetListConfiguration deploymentConfiguration,
            [NotNull] IKeyValueConfiguration keyValueConfiguration,
            [NotNull] ILogger logger,
            [NotNull] NuGetConfiguration nuGetConfiguration,
            [NotNull] NuGetPackageInstaller packageInstaller)
        {
            _deploymentConfiguration = deploymentConfiguration ??
                                       throw new ArgumentNullException(nameof(deploymentConfiguration));
            _keyValueConfiguration =
                keyValueConfiguration ?? throw new ArgumentNullException(nameof(keyValueConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nuGetConfiguration = nuGetConfiguration ?? throw new ArgumentNullException(nameof(nuGetConfiguration));
            _packageInstaller = packageInstaller ?? throw new ArgumentNullException(nameof(packageInstaller));
        }


        public async Task<IReadOnlyCollection<PackageVersion>> GetPackageVersionsAsync(
            string packageId,
            bool useCache = true,
            ILogger? logger = null,
            bool includePreReleased = false,
            string? nugetPackageSource = null,
            string? nugetConfigFile = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageId));
            }

            if (packageId.Equals(Constants.NotAvailable, StringComparison.OrdinalIgnoreCase))
            {
                return ImmutableArray<PackageVersion>.Empty;
            }

            if (string.IsNullOrWhiteSpace(_nuGetConfiguration.NugetExePath))
            {
                throw new DeployerAppException("The nuget.exe path is not set");
            }

            if (!File.Exists(_nuGetConfiguration.NugetExePath))
            {
                throw new DeployerAppException(
                    $"The nuget.exe path '{_nuGetConfiguration.NugetExePath}' does not exist");
            }

            string packageSourceAppSettingsKey = ConfigurationConstants.NuGetPackageSourceName;

            string? packageSource = nugetPackageSource.WithDefault(_keyValueConfiguration[packageSourceAppSettingsKey]);

            if (!string.IsNullOrWhiteSpace(packageSource))
            {
                logger?.Debug("Using package source '{PackageSource}' for package {Package}", packageSource, packageId);
            }
            else
            {
                logger?.Debug(
                    "There is no package source defined in app settings, key '{PackageSourceAppSettingsKey}', using all sources",
                    packageSourceAppSettingsKey);
            }

            string? configFile =
                nugetConfigFile.WithDefault(_keyValueConfiguration[ConfigurationConstants.NugetConfigFile]);

            if (configFile.HasValue() && File.Exists(configFile))
            {
                _logger.Debug("Using NuGet config file {NuGetConfigFile} for package {Package}", configFile, packageId);
            }

            logger?.Debug(
                "Running NuGet from package service to find package {PackageId} with timeout {Seconds} seconds",
                packageId,
                _deploymentConfiguration.ListTimeOutInSeconds);

            Stopwatch stopwatch = Stopwatch.StartNew();

            var allVersions = await _packageInstaller.GetAllVersionsAsync(
                new NuGetPackageId(packageId),
                nuGetSource: nugetPackageSource,
                nugetConfig: nugetConfigFile,
                allowPreRelease: includePreReleased,
                nugetExePath: _nuGetConfiguration.NugetExePath);

            stopwatch.Stop();

            _logger.Debug(
                "Get package versions external process took {Elapsed} milliseconds",
                stopwatch.ElapsedMilliseconds);

            var addedPackages = new List<string>();

            IReadOnlyCollection<PackageVersion> packageVersions = allVersions
                .Select(version => new PackageVersion(packageId, version))
                .ToArray();

            foreach (PackageVersion packageVersion in packageVersions)
            {
                _logger.Debug(
                    "Found package {Package} {Version}",
                    packageVersion.PackageId,
                    packageVersion.Version.ToNormalizedString());

                addedPackages.Add(packageVersion.ToString());
            }

            if (_logger.IsEnabled(LogEventLevel.Verbose))
            {
                _logger.Verbose(
                    "Added {Count} packages for package id {PackageId} {PackageVersions}",
                    addedPackages.Count,
                    packageId,
                    addedPackages);
            }
            else if (addedPackages.Count > 0 && addedPackages.Count < 20)
            {
                _logger.Information(
                    "Added {Count} packages for package id {PackageId} {PackageVersions}",
                    addedPackages.Count,
                    packageId,
                    addedPackages);
            }
            else if (addedPackages.Any())
            {
                _logger.Information(
                    "Added {Count} packages for package id {PackageId}",
                    addedPackages.Count,
                    packageId);
            }

            return packageVersions;
        }
    }
}