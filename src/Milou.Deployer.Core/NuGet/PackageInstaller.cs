using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arbor.KVConfiguration.Core;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Extensions;
using Milou.Deployer.Core.Processes;
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

        public async Task<MayBe<InstalledPackage>> InstallPackageAsync(
            DeploymentExecutionDefinition deploymentExecutionDefinition,
            DirectoryInfo tempDirectory,
            bool includeVersion = true)
        {
            if (deploymentExecutionDefinition == null)
            {
                throw new ArgumentNullException(nameof(deploymentExecutionDefinition));
            }

            if (tempDirectory == null)
            {
                throw new ArgumentNullException(nameof(tempDirectory));
            }

            string executePath = _deployerConfiguration.NuGetExePath;

            if (string.IsNullOrWhiteSpace(_deployerConfiguration.NuGetExePath))
            {
                throw new InvalidOperationException("The NuGet executable file path is not defined");
            }

            if (!File.Exists(executePath))
            {
                throw new InvalidOperationException($"The NuGet executable file '{executePath}' does not exist");
            }

            var arguments = new List<string> { "install", deploymentExecutionDefinition.PackageId };

            if (deploymentExecutionDefinition.SemanticVersion.HasValue)
            {
                arguments.Add("-Version");
                arguments.Add(deploymentExecutionDefinition.SemanticVersion.Value.ToNormalizedString());
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

            if (!string.IsNullOrWhiteSpace(deploymentExecutionDefinition.NuGetConfigFile)
                && File.Exists(deploymentExecutionDefinition.NuGetConfigFile))
            {
                arguments.Add("-ConfigFile");
                arguments.Add(deploymentExecutionDefinition.NuGetConfigFile);
            }
            else if (!string.IsNullOrWhiteSpace(_deployerConfiguration.NuGetConfig)
                     && File.Exists(_deployerConfiguration.NuGetConfig))
            {
                arguments.Add("-ConfigFile");
                arguments.Add(_deployerConfiguration.NuGetConfig);
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

            if (
                _keyValueConfiguration[ConfigurationKeys.NuGetNoCache]
                    .ParseAsBooleanOrDefault())
            {
                arguments.Add("-NoCache");
            }

            const string sourceKey = ConfigurationKeys.NuGetSource;
            string source = _keyValueConfiguration[sourceKey];

            if (!string.IsNullOrWhiteSpace(source))
            {
                _logger.Information("A specific NuGet source is defined in definition: '{Source}'",
                    deploymentExecutionDefinition.NuGetPackageSource);
                arguments.Add("-Source");
                arguments.Add(deploymentExecutionDefinition.NuGetPackageSource);
            }
            else if (!string.IsNullOrWhiteSpace(source))
            {
                _logger.Information(
                    "A specific NuGet source is defined in app settings [key '{SourceKey}']: '{Source}'",
                    sourceKey,
                    source);
                arguments.Add("-Source");
                arguments.Add(source);
            }
            else
            {
                _logger.Debug("A specific NuGet source is not defined in app settings");
            }

            ExitCode exitCode = await
                ProcessRunner.ExecuteProcessAsync(
                        executePath,
                        arguments,
                        _logger.Debug,
                        _logger.Error,
                        _logger.Debug,
                        verboseAction: _logger.Verbose,
                        debugAction: _logger.Debug)
                    .ConfigureAwait(false);

            if (!exitCode.IsSuccess)
            {
                _logger.Error("The package installer process '{Process}' {Arguments} failed with exit code {ExitCode}",
                    executePath,
                    string.Join(" ", arguments.Select(arg => $"\"{arg}\"")),
                    exitCode);
                return MayBe<InstalledPackage>.Nothing;
            }

            List<FileInfo> packageFiles =
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

                return MayBe<InstalledPackage>.Nothing;
            }

            if (packageFiles.Count > 1)
            {
                _logger.Error(
                    "Found multiple installed packages matching '{PackageId}' in output directory '{FullName}' or in any of it's sub directories, expected exactly 1. Found files [{Count}]: '{V}",
                    deploymentExecutionDefinition.PackageId,
                    tempDirectory.FullName,
                    packageFiles.Count,
                    string.Join(",", packageFiles.Select(file => $"'{file.FullName}'")));

                return MayBe<InstalledPackage>.Nothing;
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

                return MayBe<InstalledPackage>.Nothing;
            }

            var installedPackage = new MayBe<InstalledPackage>(InstalledPackage.Create(packageId,
                semanticVersion,
                foundPackageFile.FullName));

            return installedPackage;
        }
    }
}
