using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.KVConfiguration.Core;
using JetBrains.Annotations;
using Milou.Deployer.Core;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Extensions;
using Milou.Deployer.Core.IO;
using Milou.Deployer.Core.Processes;
using NuGet.Versioning;
using Serilog;

namespace Milou.Deployer.ConsoleClient
{
    public sealed class DeployerApp : IDisposable
    {
        private readonly DeploymentService _deploymentService;
        private readonly DeploymentExecutionDefinitionFileReader _fileReader;
        private readonly IKeyValueConfiguration _appSettings;

        private readonly ILogger _logger;
        private readonly AppExit _appExit;

        public DeployerApp(
            [NotNull] ILogger logger,
            [NotNull] DeploymentService deploymentService,
            [NotNull] DeploymentExecutionDefinitionFileReader fileReader,
            [NotNull] IKeyValueConfiguration appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deploymentService = deploymentService ?? throw new ArgumentNullException(nameof(deploymentService));
            _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _appExit = new AppExit(_logger);
        }

        public async Task<int> ExecuteAsync(string[] args)
        {
            PrintVersion();

            PrintCommandLineArguments(args);

            PrintEnvironmentVariables(args);

            PrintAvailableArguments(args);

            string[] nonFlagArgs =
                args.Where(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (Debugger.IsAttached && nonFlagArgs.Length > 0
                                    && nonFlagArgs[0].Equals("fail"))
            {
                return _appExit.ExitFailure();
            }

            if (!string.IsNullOrWhiteSpace(args.SingleOrDefault(arg =>
                arg.Equals("--help", StringComparison.OrdinalIgnoreCase))))
            {
                _logger.Information("Help");

                _logger.Information("{Help}", Help.ShowHelp());

                return _appExit.ExitSuccess();
            }

            try
            {
                if (nonFlagArgs.Length == 1
                    && nonFlagArgs[0].Equals(Commands.Update, StringComparison.OrdinalIgnoreCase))
                {
                    return _appExit.Exit(await UpdateSelfAsync());
                }

                if (nonFlagArgs.Length == 1
                    && nonFlagArgs[0].Equals(Commands.Updating, StringComparison.OrdinalIgnoreCase))
                {
                    return _appExit.Exit(UpdatingSelf());
                }

                if (nonFlagArgs.Length == 1
                    && nonFlagArgs[0].Equals(Commands.Updated, StringComparison.OrdinalIgnoreCase))
                {
                    return _appExit.Exit(UpdatedSelf());
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Error");
                return _appExit.ExitFailure();
            }

            ExitCode exitCode;
            try
            {
                if (nonFlagArgs.Length <= 1)
                {
                    bool hasArgs = nonFlagArgs.Length == 0;

                    string fallbackManifestPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        ConfigurationKeys.ManifestFileName);

                    string manifestFile = hasArgs
                        ? fallbackManifestPath
                        : nonFlagArgs[0];

                    if (!hasArgs)
                    {
                        _logger.Verbose(
                            "No arguments were supplied, falling back trying to find a manifest based on current path, looking for '{FallbackManifestPath}'",
                            fallbackManifestPath);
                    }

                    exitCode = await ExecuteAsync(manifestFile);
                }
                else if (nonFlagArgs.Length == 2)
                {
                    _logger.Error("Invalid argument count");
                    return _appExit.ExitFailure();
                }
                else
                {
                    string packageId = nonFlagArgs[0];
                    string semanticVersion = nonFlagArgs[1];
                    string targetDirectory = nonFlagArgs[2];

                    switch (nonFlagArgs.Length)
                    {
                        case 3:
                            exitCode = await ExecuteAsync(packageId, semanticVersion, targetDirectory);
                            break;
                        case 4:
                        {
                            string allowPreRelease = nonFlagArgs[3];

                            exitCode = await ExecuteAsync(packageId, semanticVersion, targetDirectory, allowPreRelease);
                            break;
                        }
                        case 5:
                        {
                            string allowPreRelease = nonFlagArgs[3];

                            string environment = nonFlagArgs[4];

                            exitCode = await ExecuteAsync(packageId,
                                semanticVersion,
                                targetDirectory,
                                allowPreRelease,
                                environment);
                            break;
                        }
                        case 6:
                        {
                            string allowPreRelease = nonFlagArgs[3];

                            string environment = nonFlagArgs[4];

                            string publishSettingsFile = nonFlagArgs[5];

                            exitCode = await ExecuteAsync(packageId,
                                semanticVersion,
                                targetDirectory,
                                allowPreRelease,
                                environment,
                                publishSettingsFile);
                            break;
                        }
                        default:
                            return _appExit.ExitFailure();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unhandled application error");
                exitCode = _appExit.ExitFailure();
            }

            return _appExit.Exit(exitCode);
        }

        private void PrintAvailableArguments(string[] args)
        {
            if (_appSettings is MultiSourceKeyValueConfiguration
                multiSourceKeyValueConfiguration)
            {
                _logger.Information("Available parameters {Parameters}", multiSourceKeyValueConfiguration.AllKeys);
            }
        }

        private async Task<ExitCode> UpdateSelfAsync()
        {
            string targetTempDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp");
            string targetTempDirectory2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp2");

            string allowPreRelease =
                _deploymentService.DeployerConfiguration.AllowPreReleaseEnabled.ToString().ToLowerInvariant();

            ExitCode exitCode = await ExecuteAsync(
                "Milou.Deployer.ConsoleClient",
                string.Empty,
                targetTempDirectory,
                allowPreRelease);

            if (!exitCode.IsSuccess)
            {
                return exitCode;
            }

            ExitCode exitCode2 = await ExecuteAsync(
                "Milou.Deployer.ConsoleClient",
                string.Empty,
                targetTempDirectory2,
                allowPreRelease);

            if (!exitCode2.IsSuccess)
            {
                return exitCode2;
            }

            _logger.Debug("Starting process updating process");

            Process.Start(Path.Combine(targetTempDirectory, "Milou.Deployer.ConsoleClient.exe"),
                nameof(Commands.Updating));

            return exitCode;
        }

        private ExitCode UpdatingSelf()
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));

            _logger.Debug("Updating self");

            string targetTempDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);

            var tempDirectory = new DirectoryInfo(targetTempDirectory);

            if (tempDirectory.Name != "tmp")
            {
                return ExitCode.Failure;
            }

            DirectoryInfo parent = tempDirectory.Parent.ThrowIfNull();

            _logger.Debug("Deleting files in {FullName}", parent.FullName);

            var exclusionsContains = new List<string> { ".vshost." };
            var exclusionsStartsWith = new List<string> { Environment.MachineName + "." };
            var exclusionsByExtension = new List<string> { ".log" };

            foreach (
                FileInfo fileInfo in
                parent.GetFiles()
                    .Where(file =>
                        !exclusionsContains.Any(exclusion =>
                            file.Name.IndexOf(exclusion, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        && !exclusionsStartsWith.Any(exclusion =>
                            file.Name.StartsWith(exclusion, StringComparison.OrdinalIgnoreCase))
                        && !exclusionsByExtension.Any(exclusion =>
                            file.Extension.Equals(exclusion, StringComparison.OrdinalIgnoreCase))
                    ))
            {
                fileInfo.Delete();
            }

            _logger.Debug("Deleting directories in '{FullName}'", parent.FullName);

            foreach (DirectoryInfo directory in parent.GetDirectories()
                .Where(dir => !dir.Name.StartsWith("tmp", StringComparison.OrdinalIgnoreCase)))
            {
                directory.Delete(true);
            }

            var temp2 = new DirectoryInfo(Path.Combine(parent.FullName, "tmp2"));

            RecursiveIO.RecursiveCopy(temp2, parent, _logger, ImmutableArray<string>.Empty);

            _logger.Debug("Starting updated process");

            Process.Start(Path.Combine(parent.FullName, "Milou.Deployer.ConsoleClient.exe"), Commands.Updated);

            return _appExit.ExitSuccess();
        }

        private ExitCode UpdatedSelf()
        {
            _logger.Debug("Updated self");

            Thread.Sleep(TimeSpan.FromSeconds(2));

            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var temp1 = new DirectoryInfo(Path.Combine(currentDirectory, "tmp"));
            var temp2 = new DirectoryInfo(Path.Combine(currentDirectory, "tmp2"));

            if (temp1.Exists)
            {
                _logger.Debug("Deleting directory {FullName}", temp1.FullName);

                temp1.Delete(true);
            }

            if (temp2.Exists)
            {
                _logger.Debug("Deleting directory {FullName}", temp2.FullName);
                temp2.Delete(true);
            }

            var directoryInfo = new DirectoryInfo(currentDirectory);

            FileInfo[] files = directoryInfo.GetFiles(DeploymentService.AppOfflineHtm);

            foreach (FileInfo file in files)
            {
                file.Delete();
            }

            _logger.Debug("Updated self done");

            return _appExit.ExitSuccess();
        }

        private void PrintVersion()
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly().ThrowIfNull();

            AssemblyName assemblyName = executingAssembly.GetName();

            string assemblyVersion = assemblyName.Version.ToString().ThrowIfNullOrEmpty();

            string location = executingAssembly.Location.ThrowIfNullOrEmpty();

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(location);

            string fileVersion = fvi.FileVersion;

            Type type = typeof(Program);

            _logger.Information("{Namespace} assembly version {AssemblyVersion}, file version {FileVersion}",
                type.Namespace,
                assemblyVersion,
                fileVersion);
        }

        private async Task<ExitCode> ExecuteAsync(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (!File.Exists(file))
            {
                _logger.Error("The deployment manifest file '{File}' does not exist", file);
                return ExitCode.Failure;
            }

            string data = _fileReader.ReadAllData(file);

            ImmutableArray<DeploymentExecutionDefinition> deploymentExecutionDefinitions =
                new DeploymentExecutionDefinitionParser().Deserialize(data);

            if (!deploymentExecutionDefinitions.Any())
            {
                _logger.Error("Could not find any deployment definitions in file '{File}'", file);
                return ExitCode.Failure;
            }

            _logger.Information("Found {Length} deployment definitions", deploymentExecutionDefinitions.Length);

            _logger.Verbose("{V}",
                string.Join(", ", deploymentExecutionDefinitions.Select(definition => $"{definition}")));

            return await _deploymentService.DeployAsync(deploymentExecutionDefinitions);
        }

        private async Task<ExitCode> ExecuteAsync(string packageId, string semanticVersion, string targetDirectory)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
            }

            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                throw new ArgumentException("Argument is null or whitespace", nameof(targetDirectory));
            }

            MayBe<SemanticVersion> version = string.IsNullOrWhiteSpace(semanticVersion)
                ? MayBe<SemanticVersion>.Nothing()
                : new MayBe<SemanticVersion>(SemanticVersion.Parse(semanticVersion));

            ImmutableArray<DeploymentExecutionDefinition> deploymentExecutionDefinitions = new List
                <DeploymentExecutionDefinition>
                {
                    new DeploymentExecutionDefinition(
                        packageId,
                        targetDirectory,
                        version)
                }.ToImmutableArray();

            return await _deploymentService.DeployAsync(deploymentExecutionDefinitions);
        }

        private async Task<ExitCode> ExecuteAsync(
            string packageId,
            string semanticVersion,
            string targetDirectory,
            string allowPreRelease,
            string environmentConfig = "",
            string publishSettingsFile = null)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                throw new ArgumentNullException(nameof(packageId));
            }

            if (!bool.TryParse(allowPreRelease, out bool parsedResultValue))
            {
                parsedResultValue = false;
            }

            _logger.Verbose("Parsed pre-release flag as {ParsedResultValue}", parsedResultValue);

            MayBe<SemanticVersion> version = string.IsNullOrWhiteSpace(semanticVersion)
                ? MayBe<SemanticVersion>.Nothing()
                : new MayBe<SemanticVersion>(SemanticVersion.Parse(semanticVersion));

            ImmutableArray<DeploymentExecutionDefinition> deploymentExecutionDefinitions = new[]
            {
                new DeploymentExecutionDefinition(
                    packageId,
                    targetDirectory,
                    version,
                    isPreRelease: parsedResultValue,
                    environmentConfig: environmentConfig,
                    publishSettingsFile: publishSettingsFile)
            }.ToImmutableArray();

            return await _deploymentService.DeployAsync(deploymentExecutionDefinitions);
        }

        private void PrintEnvironmentVariables(string[] args)
        {
            if (args.Any(arg => arg.Equals("--debug")))
            {
                _logger.Debug("Used variables:");

                foreach (StringPair variable in _appSettings.AllValues
                    .OrderBy(entry => entry.Key))
                {
                    _logger.Debug("ENV '{Key}': '{Value}'", variable.Key, variable.Value);
                }
            }
        }

        private void PrintCommandLineArguments(string[] args)
        {
            if (args.Any(arg => arg.Equals("--debug")))
            {
                _logger.Debug("Command line arguments:");

                foreach (string arg in args)
                {
                    _logger.Debug("ARG '{Arg}'", arg);
                }
            }
        }

        public void Dispose()
        {
            if (_logger is IDisposable disposableLogger)
            {
                disposableLogger.Dispose();
            }

            if (_appSettings is IDisposable disposableSettings)
            {
                disposableSettings.Dispose();
            }
        }
    }
}
