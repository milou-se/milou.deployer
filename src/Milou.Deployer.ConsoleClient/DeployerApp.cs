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
    public class DeployerApp
    {
        private readonly DeploymentService _deploymentService;
        private readonly DeploymentExecutionDefinitionFileReader _fileReader;

        private readonly ILogger _logger;

        public DeployerApp(
            [NotNull] ILogger logger,
            [NotNull] DeploymentService deploymentService,
            [NotNull] DeploymentExecutionDefinitionFileReader fileReader)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deploymentService = deploymentService ?? throw new ArgumentNullException(nameof(deploymentService));
            _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
        }

        public async Task<int> ExecuteAsync(string[] args)
        {
            PrintVersion();

            PrintCommandLineArguments(args);

            PrintEnvironmentVariables(args);

            PrintAvailableArguments(args);

            string[] nonFlagArgs = args.Where(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (Debugger.IsAttached && nonFlagArgs.Length > 0
                && nonFlagArgs.First().Equals("fail"))
            {
                return ExitCode.Failure;
            }

            if (!string.IsNullOrWhiteSpace(args.SingleOrDefault(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase))))
            {
                Serilog.Log.Logger.Information("Help");

                return ExitCode.Success;
            }

            try
            {
                if (nonFlagArgs.Length == 1 && nonFlagArgs.First().Equals(Commands.Update, StringComparison.OrdinalIgnoreCase))
                {
                    return await UpdateSelfAsync();
                }

                if (nonFlagArgs.Length == 1 && nonFlagArgs.First().Equals(Commands.Updating, StringComparison.OrdinalIgnoreCase))
                {
                    return UpdatingSelf();
                }

                if (nonFlagArgs.Length == 1 && nonFlagArgs.First().Equals(Commands.Updated, StringComparison.OrdinalIgnoreCase))
                {
                    return UpdatedSelf();
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex.ToString());
                return ExitCode.Failure;
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
                        : nonFlagArgs.First();

                    if (!hasArgs)
                    {
                        _logger.Verbose("No arguments were supplied, falling back trying to find a manifest based on current path, looking for '{FallbackManifestPath}'", fallbackManifestPath);
                    }

                    exitCode = await ExecuteAsync(manifestFile);
                }
                else if (nonFlagArgs.Length == 2)
                {
                    _logger.Error("Invalid argument count");
                    return ExitCode.Failure;
                }
                else
                {
                    string packageId = nonFlagArgs[0];
                    string semanticVersion = nonFlagArgs[1];
                    string targetDirectory = nonFlagArgs[2];

                    if (nonFlagArgs.Length == 3)
                    {
                        exitCode = await ExecuteAsync(packageId, semanticVersion, targetDirectory);
                    }
                    else if (nonFlagArgs.Length == 4)
                    {
                        string allowPreRelease = nonFlagArgs[3];

                        exitCode = await ExecuteAsync(packageId, semanticVersion, targetDirectory, allowPreRelease);
                    }
                    else if (nonFlagArgs.Length == 5)
                    {
                        string allowPreRelease = nonFlagArgs[3];

                        string environment = nonFlagArgs[4];

                        exitCode = await ExecuteAsync(packageId,
                            semanticVersion,
                            targetDirectory,
                            allowPreRelease,
                            environment);
                    }
                    else if (nonFlagArgs.Length == 6)
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
                    }
                    else
                    {
                        return ExitCode.Failure;
                    }
                }
            }
            catch (Exception ex)
            {
                exitCode = ExitCode.Failure;
                Serilog.Log.Error(ex, "Unhandled application error");
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press ENTER to continue");
                Console.ReadLine();
            }

            Serilog.Log.Information("Exit code {ExitCode}", exitCode);

            return exitCode;
        }

        private void PrintAvailableArguments(string[] args)
        {
            if (StaticKeyValueConfigurationManager.AppSettings is MultiSourceKeyValueConfiguration
                multiSourceKeyValueConfiguration)
            {
               Serilog.Log.Logger.Information("Available parameters {Parameters}", multiSourceKeyValueConfiguration.AllKeys);
            }
        }

        private async Task<ExitCode> UpdateSelfAsync()
        {
            string targetTempDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp");
            string targetTempDirectory2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tmp2");

            string allowPreRelease =
                _deploymentService.DeployerConfiguration.AllowPreReleaseEnabled.ToString().ToLowerInvariant();

            ExitCode exicCode = await ExecuteAsync(
                "Milou.Deployer.ConsoleClient",
                string.Empty,
                targetTempDirectory,
                allowPreRelease);

            if (!exicCode.IsSuccess)
            {
                return exicCode;
            }

            ExitCode exicCode2 = await ExecuteAsync(
                "Milou.Deployer.ConsoleClient",
                string.Empty,
                targetTempDirectory2,
                allowPreRelease);

            if (!exicCode2.IsSuccess)
            {
                return exicCode2;
            }

            _logger.Debug("Starting process updating process");

            Process.Start(Path.Combine(targetTempDirectory, "Milou.Deployer.ConsoleClient.exe"),
                nameof(Commands.Updating));

            return exicCode;
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

            foreach (
                FileInfo fileInfo in
                parent.GetFiles()
                    .Where(file => file.Name.IndexOf(".vshost.", StringComparison.InvariantCultureIgnoreCase) < 0))
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

            return ExitCode.Success;
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

            return ExitCode.Success;
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

            _logger.Information("{Namespace} assembly version {AssemblyVersion}, file version {FileVersion}", type.Namespace, assemblyVersion, fileVersion);
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

            _logger.Verbose("{V}", string.Join(", ", deploymentExecutionDefinitions.Select(definition => $"{definition}")));

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

            bool parsedResultValue;

            if (!bool.TryParse(allowPreRelease, out parsedResultValue))
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
                    parsedResultValue,
                    environmentConfig: environmentConfig,
                    publishSettingsFile: publishSettingsFile)
            }.ToImmutableArray();

            return await _deploymentService.DeployAsync(deploymentExecutionDefinitions);
        }

        private void PrintEnvironmentVariables(string[] args)
        {
            if (args.Any(arg => arg.Equals("--debug")))
            {
                _logger.Debug("Environment variables:");

                foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables()
                    .OfType<DictionaryEntry>().OrderBy(entry => entry.Key))
                {
                    _logger.Debug("ENV '{Key}': '{Value}'", environmentVariable.Key, environmentVariable.Value);
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
    }
}
