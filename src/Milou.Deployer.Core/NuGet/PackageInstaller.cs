using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.KVConfiguration.Core;
using Arbor.Processing;
using JetBrains.Annotations;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Configuration;
using Milou.Deployer.Core.Extensions;
using NuGet.Packaging;
using NuGet.Versioning;
using Serilog;

namespace Milou.Deployer.Core.NuGet
{
    public class PackageInstaller
    {
        private readonly DeployerConfiguration _deployerConfiguration;
        private readonly IKeyValueConfiguration _keyValueConfiguration;

        private readonly ILogger _logger;

        public PackageInstaller(
            ILogger logger,
            DeployerConfiguration deployerConfiguration,
            IKeyValueConfiguration keyValueConfiguration)
        {
            _logger = logger;
            _deployerConfiguration = deployerConfiguration;
            _keyValueConfiguration = keyValueConfiguration;
        }

        [ItemCanBeNull]
        public async Task<InstalledPackage?> InstallPackageAsync(
            DeploymentExecutionDefinition deploymentExecutionDefinition,
            DirectoryInfo tempDirectory,
            bool includeVersion = true,
            SemanticVersion? explicitVersion = null,
            CancellationToken cancellationToken = default)
        {
            if (deploymentExecutionDefinition is null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            if (tempDirectory is null)
            {
                throw new ArgumentNullException(nameof(tempDirectory));
            }

            string executePath =
                deploymentExecutionDefinition.NuGetExePath.WithDefault(_deployerConfiguration.NuGetExePath);

            if (string.IsNullOrWhiteSpace(executePath))
            {
                throw new InvalidOperationException("The NuGet executable file path is not defined");
            }

            if (!File.Exists(executePath))
            {
                throw new InvalidOperationException($"The NuGet executable file '{executePath}' does not exist");
            }

            var arguments = new List<string> {"install", deploymentExecutionDefinition.PackageId};

            void AddVersion(string value)
            {
                arguments.Add("-Version");
                arguments.Add(value);
            }

            if (explicitVersion is {})
            {
                AddVersion(explicitVersion.ToNormalizedString());
            }
            else if (deploymentExecutionDefinition.SemanticVersion is {})
            {
                AddVersion(deploymentExecutionDefinition.SemanticVersion.ToNormalizedString());
            }

            if (deploymentExecutionDefinition.IsPreRelease)
            {
                if (!_deployerConfiguration.AllowPreReleaseEnabled && !deploymentExecutionDefinition.Force)
                {
                    throw new InvalidOperationException(
                        $"The deployer configuration is set to not allow pre-releases, environment variable '{ConfigurationKeys.AllowPreReleaseEnvironmentVariable}'");
                }

                arguments.Add("-PreRelease");
            }

            if (!tempDirectory.Exists)
            {
                _logger.Debug("Creating temp directory '{FullName}'", tempDirectory.FullName);
                tempDirectory.Create();
            }

            if (!string.IsNullOrWhiteSpace(deploymentExecutionDefinition.NuGetConfigFile))
            {
                if (File.Exists(deploymentExecutionDefinition.NuGetConfigFile))
                {
                    arguments.Add("-ConfigFile");
                    arguments.Add(deploymentExecutionDefinition.NuGetConfigFile);
                }
                else
                {
                    _logger.Warning(
                        "The deployment execution definition {Definition} has nuget config file set to {ConfigFile} but it does not exist",
                        deploymentExecutionDefinition,
                        deploymentExecutionDefinition.NuGetConfigFile);
                }
            }
            else if (!string.IsNullOrWhiteSpace(_deployerConfiguration.NuGetConfig))
            {
                if (File.Exists(_deployerConfiguration.NuGetConfig))
                {
                    arguments.Add("-ConfigFile");
                    arguments.Add(_deployerConfiguration.NuGetConfig);
                }
                else
                {
                    _logger.Warning(
                        "The deployment configuration has nuget config file set to {ConfigFile} but it does not exist",
                        _deployerConfiguration.NuGetConfig);
                }
            }

            arguments.Add("-OutputDirectory");
            arguments.Add(tempDirectory.FullName);
            arguments.Add("-Verbosity");
            arguments.Add("detailed");
            arguments.Add("-NonInteractive");

            if (!includeVersion)
            {
                arguments.Add("-ExcludeVersion");
            }

            if (_keyValueConfiguration[ConfigurationKeys.NuGetNoCache]
                .ParseAsBooleanOrDefault())
            {
                arguments.Add("-NoCache");
            }

            const string sourceKey = ConfigurationKeys.NuGetSource;
            string nugetSourceInConfiguration = _deployerConfiguration.NuGetSource;
            string? nugetSourceInDeploymentExecution = deploymentExecutionDefinition.NuGetPackageSource;

            if (!string.IsNullOrWhiteSpace(nugetSourceInDeploymentExecution))
            {
                _logger.Information("A specific NuGet source is defined in definition: '{Source}'",
                    nugetSourceInDeploymentExecution);

                arguments.Add("-Source");
                arguments.Add(nugetSourceInDeploymentExecution);
            }
            else if (!string.IsNullOrWhiteSpace(nugetSourceInConfiguration))
            {
                _logger.Information(
                    "A specific NuGet source is defined in app settings [key '{SourceKey}']: '{Source}'",
                    sourceKey,
                    nugetSourceInConfiguration);

                arguments.Add("-Source");
                arguments.Add(nugetSourceInConfiguration);
            }
            else
            {
                _logger.Debug(
                    "A specific NuGet source is not defined in settings or in deployment execution definition");
            }

            ExitCode? exitCode = default;

            using (var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    exitCode = await ProcessRunner.ExecuteProcessAsync(
                        executePath,
                        arguments,
                        (message, category) => _logger.Debug("{Category} {Message}", category, message),
                        (message, category) => _logger.Error("{Category} {Message}", category, message),
                        (message, category) => _logger.Debug("{Category} {Message}", category, message),
                        (message, category) => _logger.Verbose("{Category} {Message}", category, message),
                        debugAction: (message, category) => _logger.Debug(
                            "{Category} {Message}",
                            category,
                            message),
                        cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException ex)
                {
                    _logger.Error(ex, "NuGet package install timed out");
                }
            }

            if (exitCode?.IsSuccess != true)
            {
                _logger.Error("The package installer process '{Process}' {Arguments} failed with exit code {ExitCode}",
                    executePath,
                    string.Join(" ", arguments.Select(arg => $"\"{arg}\"")),
                    exitCode);
                return default;
            }

            var packageFiles =
                tempDirectory.EnumerateFiles("*.nupkg", SearchOption.AllDirectories)
                    .Where(
                        file =>
                            file.Name.IndexOf(
                                deploymentExecutionDefinition.PackageId,
                                StringComparison.InvariantCultureIgnoreCase) >= 0)
                    .ToList();

            if (!packageFiles.Any())
            {
                _logger.Error(
                    "Could not find the installed package '{PackageId}' in output directory '{FullName}' or in any of it's sub directories",
                    deploymentExecutionDefinition.PackageId,
                    tempDirectory.FullName);

                return default;
            }

            if (packageFiles.Count > 1)
            {
                _logger.Error(
                    "Found multiple installed packages matching '{PackageId}' in output directory '{FullName}' or in any of it's sub directories, expected exactly 1. Found files [{Count}]: '{V}",
                    deploymentExecutionDefinition.PackageId,
                    tempDirectory.FullName,
                    packageFiles.Count,
                    string.Join(",", packageFiles.Select(file => $"'{file.FullName}'")));

                return default;
            }

            FileInfo foundPackageFile = packageFiles.Single();

            SemanticVersion semanticVersion;
            string packageId;

            using (var package = new PackageArchiveReader(foundPackageFile.FullName))
            {
                semanticVersion = SemanticVersion.Parse(package.NuspecReader.GetVersion().ToNormalizedString());
                packageId = package.NuspecReader.GetId();
            }

            if (!packageId.Equals(deploymentExecutionDefinition.PackageId, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.Error(
                    "The installed package id '{PackageId}' is different than the expected package id '{PackageId1}'",
                    packageId,
                    deploymentExecutionDefinition.PackageId);

                return default;
            }

            var installedPackage = InstalledPackage.Create(packageId,
                semanticVersion,
                foundPackageFile.FullName);

            return installedPackage;
        }
    }
}