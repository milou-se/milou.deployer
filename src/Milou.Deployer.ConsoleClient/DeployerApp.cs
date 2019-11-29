using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.KVConfiguration.Core;
using Arbor.Processing;
using JetBrains.Annotations;

using Milou.Deployer.Core.Cli;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Extensions;

using NuGet.Versioning;

using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Milou.Deployer.ConsoleClient
{
    public sealed class DeployerApp : IDisposable
    {
        public LoggingLevelSwitch LevelSwitch { get; }
        private readonly AppExit _appExit;
        private readonly DeploymentService _deploymentService;
        private readonly DeploymentExecutionDefinitionFileReader _fileReader;
        private IKeyValueConfiguration _appSettings;
        private CancellationTokenSource _cancellationTokenSource;

        public ILogger Logger { get; private set; }

        public DeployerApp(
            [NotNull] ILogger logger,
            [NotNull] DeploymentService deploymentService,
            [NotNull] DeploymentExecutionDefinitionFileReader fileReader,
            [NotNull] IKeyValueConfiguration appSettings,
            [NotNull] LoggingLevelSwitch levelSwitch,
            [NotNull] CancellationTokenSource cancellationTokenSource)
        {
            LevelSwitch = levelSwitch ?? throw new ArgumentNullException(nameof(levelSwitch));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deploymentService = deploymentService ?? throw new ArgumentNullException(nameof(deploymentService));
            _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _cancellationTokenSource = cancellationTokenSource ??
                                       throw new ArgumentNullException(nameof(cancellationTokenSource));
            _appExit = new AppExit(Logger);
        }

        public void Dispose()
        {
            Logger?.Verbose("Disposing deployer app");

            if (Logger is IDisposable disposableLogger)
            {
                disposableLogger.Dispose();
                Logger = null;
            }

            if (_appSettings is IDisposable disposableSettings)
            {
                _appSettings = null;
                disposableSettings.Dispose();
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        {
            if (cancellationToken == CancellationToken.None)
            {
                cancellationToken = _cancellationTokenSource.Token;
            }

            args ??= Array.Empty<string>();

            PrintVersion();

            PrintCommandLineArguments(args);

            PrintEnvironmentVariables(args);

            string[] parameterArgs =
                args.Where(arg => arg.IndexOf("=", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

            string[] nonFlagArgs =
                args.Where(arg => !arg.StartsWith("-", StringComparison.OrdinalIgnoreCase))
                    .Except(parameterArgs)
                    .ToArray();

            if (Debugger.IsAttached && nonFlagArgs.Length > 0
                                    && nonFlagArgs[0].Equals("fail", StringComparison.OrdinalIgnoreCase))
            {
                return _appExit.ExitFailure();
            }

            if (!string.IsNullOrWhiteSpace(args.SingleOrDefault(arg =>
                arg.Equals(ConsoleConfigurationKeys.HelpArgument, StringComparison.OrdinalIgnoreCase))))
            {
                Logger.Information("{Help}", Help.ShowHelp());

                return _appExit.ExitSuccess();
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

                    string version = args.GetArgumentValueOrDefault("version");

                    SemanticVersion semanticVersion = default;

                    if (!string.IsNullOrWhiteSpace(version) && !SemanticVersion.TryParse(version, out semanticVersion))
                    {
                        Logger.Error("Argument version '{Version}' is not a valid semantic version", version);
                        exitCode = ExitCode.Failure;
                    }
                    else
                    {
                        if (!hasArgs)
                        {
                            Logger.Verbose(
                                "No arguments were supplied, falling back trying to find a manifest based on current path, looking for '{FallbackManifestPath}'",
                                fallbackManifestPath);
                        }

                        exitCode = await ExecuteAsync(manifestFile, semanticVersion, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    string actualArgs = string.Join(" ", args);
                    Logger.Error("Invalid argument count, got arguments {Args}", actualArgs);
                    return _appExit.ExitFailure();
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                Logger.Error(ex, "Unhandled application error");
                exitCode = _appExit.ExitFailure();
            }

            return _appExit.Exit(exitCode);
        }

        private void PrintVersion()
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly().ThrowIfNull();

            AssemblyName assemblyName = executingAssembly.GetName();

            string assemblyVersion = assemblyName.Version.ToString().ThrowIfNullOrEmpty();

            string location = executingAssembly.Location.ThrowIfNullOrEmpty();

            var fvi = FileVersionInfo.GetVersionInfo(location);

            string fileVersion = fvi.FileVersion;

            Type type = typeof(Program);

            Logger.Information("{Namespace} assembly version {AssemblyVersion}, file version {FileVersion} at {Location}",
                type.Namespace,
                assemblyVersion,
                fileVersion,
                executingAssembly.Location);
        }

        private async Task<ExitCode> ExecuteAsync(string file, SemanticVersion version, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (!File.Exists(file))
            {
                Logger.Error("The deployment manifest file '{File}' does not exist", file);
                return ExitCode.Failure;
            }

            string data = _fileReader.ReadAllData(file);

            ImmutableArray<DeploymentExecutionDefinition> deploymentExecutionDefinitions =
                new DeploymentExecutionDefinitionParser().Deserialize(data);

            if (deploymentExecutionDefinitions.Length == 0)
            {
                Logger.Error("Could not find any deployment definitions in file '{File}'", file);
                return ExitCode.Failure;
            }

            if (deploymentExecutionDefinitions.Length == 1)
            {
                Logger.Information("Found 1 deployment definition");
            }
            else
            {
                Logger.Information("Found {Length} deployment definitions", deploymentExecutionDefinitions.Length);
            }

            Logger.Verbose("{Definitions}",
                string.Join(", ", deploymentExecutionDefinitions.Select(definition => $"{definition}")));

            ExitCode exitCode = await _deploymentService.DeployAsync(deploymentExecutionDefinitions, version, cancellationToken).ConfigureAwait(false);

            if (exitCode.IsSuccess)
            {
                Logger.Information(
                    "Successfully deployed deployment execution definition {DeploymentExecutionDefinition}",
                    deploymentExecutionDefinitions);
            }
            else
            {
                Logger.Error("Failed to deploy definition {DeploymentExecutionDefinition}",
                    deploymentExecutionDefinitions);
            }

            return exitCode;
        }

        private void PrintEnvironmentVariables(string[] args)
        {
            if (args.Any(arg => arg.Equals(ConsoleConfigurationKeys.DebugArgument, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Debug("Used variables:");

                foreach (StringPair variable in _appSettings.AllValues
                    .OrderBy(entry => entry.Key))
                {
                    Logger.Debug("ENV '{Key}': '{Value}'", variable.Key, variable.Value);
                }
            }
        }

        private void PrintCommandLineArguments(string[] args)
        {
            if (args.Any(arg => arg.Equals(ConsoleConfigurationKeys.DebugArgument, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Debug("Command line arguments:");

                foreach (string arg in args)
                {
                    Logger.Debug("ARG '{Arg}'", arg);
                }
            }
        }
    }
}
