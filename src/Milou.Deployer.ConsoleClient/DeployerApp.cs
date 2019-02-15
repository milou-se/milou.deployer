﻿using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.KVConfiguration.Core;
using JetBrains.Annotations;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Extensions;
using Milou.Deployer.Core.Processes;
using Serilog;

namespace Milou.Deployer.ConsoleClient
{
    public sealed class DeployerApp : IDisposable
    {
        private readonly AppExit _appExit;
        private readonly DeploymentService _deploymentService;
        private readonly DeploymentExecutionDefinitionFileReader _fileReader;
        private IKeyValueConfiguration _appSettings;
        private CancellationTokenSource _cancellationTokenSource;

        private ILogger _logger;

        public DeployerApp(
            [NotNull] ILogger logger,
            [NotNull] DeploymentService deploymentService,
            [NotNull] DeploymentExecutionDefinitionFileReader fileReader,
            [NotNull] IKeyValueConfiguration appSettings,
            [NotNull] CancellationTokenSource cancellationTokenSource)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _deploymentService = deploymentService ?? throw new ArgumentNullException(nameof(deploymentService));
            _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _cancellationTokenSource = cancellationTokenSource ??
                                       throw new ArgumentNullException(nameof(cancellationTokenSource));
            _appExit = new AppExit(_logger);
        }

        public void Dispose()
        {
            _logger?.Verbose("Disposing app");

            if (_logger is IDisposable disposableLogger)
            {
                disposableLogger.Dispose();
                _logger = null;
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

            PrintVersion();

            PrintCommandLineArguments(args);

            PrintEnvironmentVariables(args);

            PrintAvailableArguments(args);

            string[] nonFlagArgs =
                args.Where(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase)).ToArray();

            if (Debugger.IsAttached && nonFlagArgs.Length > 0
                                    && nonFlagArgs[0].Equals("fail", StringComparison.OrdinalIgnoreCase))
            {
                return _appExit.ExitFailure();
            }

            if (!string.IsNullOrWhiteSpace(args.SingleOrDefault(arg =>
                arg.Equals(ConsoleConfigurationKeys.HelpArgument, StringComparison.OrdinalIgnoreCase))))
            {
                _logger.Information("{Help}", Help.ShowHelp());

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

                    if (!hasArgs)
                    {
                        _logger.Verbose(
                            "No arguments were supplied, falling back trying to find a manifest based on current path, looking for '{FallbackManifestPath}'",
                            fallbackManifestPath);
                    }

                    exitCode = await ExecuteAsync(manifestFile, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _logger.Error("Invalid argument count");
                    return _appExit.ExitFailure();
                }
            }
            catch (Exception ex) when(!ex.IsFatal())
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

        private void PrintVersion()
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly().ThrowIfNull();

            AssemblyName assemblyName = executingAssembly.GetName();

            string assemblyVersion = assemblyName.Version.ToString().ThrowIfNullOrEmpty();

            string location = executingAssembly.Location.ThrowIfNullOrEmpty();

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(location);

            string fileVersion = fvi.FileVersion;

            Type type = typeof(Program);

            _logger.Information("{Namespace} assembly version {AssemblyVersion}, file version {FileVersion} at {Location}",
                type.Namespace,
                assemblyVersion,
                fileVersion,
                executingAssembly.Location);
        }

        private async Task<ExitCode> ExecuteAsync(string file, CancellationToken cancellationToken = default)
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

            if (deploymentExecutionDefinitions.Length == 0)
            {
                _logger.Error("Could not find any deployment definitions in file '{File}'", file);
                return ExitCode.Failure;
            }

            if (deploymentExecutionDefinitions.Length == 1)
            {
                _logger.Information("Found 1 deployment definition");
            }
            else
            {
                _logger.Information("Found {Length} deployment definitions", deploymentExecutionDefinitions.Length);
            }

            _logger.Verbose("{Definitions}",
                string.Join(", ", deploymentExecutionDefinitions.Select(definition => $"{definition}")));

            ExitCode exitCode = await _deploymentService.DeployAsync(deploymentExecutionDefinitions, cancellationToken).ConfigureAwait(false);

            if (exitCode.IsSuccess)
            {
                _logger.Information(
                    "Successfully deployed deployment execution definition {DeploymentExecutionDefinition}",
                    deploymentExecutionDefinitions);
            }
            else
            {
                _logger.Error("Failed to deploy definition {DeploymentExecutionDefinition}",
                    deploymentExecutionDefinitions);
            }

            return exitCode;
        }

        private void PrintEnvironmentVariables(string[] args)
        {
            if (args.Any(arg => arg.Equals(ConsoleConfigurationKeys.DebugArgument, StringComparison.OrdinalIgnoreCase)))
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
            if (args.Any(arg => arg.Equals(ConsoleConfigurationKeys.DebugArgument, StringComparison.OrdinalIgnoreCase)))
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
