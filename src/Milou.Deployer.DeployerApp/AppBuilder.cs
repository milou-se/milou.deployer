using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.KVConfiguration.Core;
using Arbor.KVConfiguration.Core.Extensions.CommandLine;
using Arbor.KVConfiguration.JsonConfiguration;
using Arbor.KVConfiguration.UserConfiguration;
using Arbor.Tooler;
using JetBrains.Annotations;
using Milou.Deployer.Core;
using Milou.Deployer.Core.Cli;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Configuration;

using Milou.Deployer.Core.Logging;
using Milou.Deployer.Ftp;
using Milou.Deployer.IIS;
using Milou.Deployer.Waws;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Milou.Deployer.DeployerApp
{
    public static class AppBuilder
    {
        public static async Task<DeployerApp> BuildAppAsync([NotNull] string[] inputArgs,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            if (inputArgs is null)
            {
                throw new ArgumentNullException(nameof(inputArgs));
            }

            var args = inputArgs.ToImmutableArray();

            bool hasDefinedLogger = logger is {};

            string outputTemplate = GetOutputTemplate(args);

            var levelSwitch = new LoggingLevelSwitch();

            logger ??= new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: outputTemplate, standardErrorFromLevel: LogEventLevel.Error)
                .MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();

            logger.Verbose("Using output template {Template}", outputTemplate);

            try
            {
                string? machineSettings =
                    GetMachineSettingsFile(new DirectoryInfo(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "tools", "Milou.Deployer")));

                AppSettingsBuilder appSettingsBuilder;

                try
                {
                    appSettingsBuilder = KeyValueConfigurationManager
                        .Add(new ReflectionKeyValueConfiguration(typeof(AppBuilder).Assembly))
                        .Add(new ReflectionKeyValueConfiguration(typeof(ConfigurationKeys).Assembly));
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    logger.Error(ex, "Could note create settings");
                    throw;
                }

                if (!string.IsNullOrWhiteSpace(machineSettings))
                {
                    logger.Information("Using machine specific configuration file '{Settings}'", machineSettings);
                    appSettingsBuilder =
                        appSettingsBuilder.Add(new JsonKeyValueConfiguration(machineSettings, false));
                }

                string? configurationFile =
                    Environment.GetEnvironmentVariable(ConfigurationKeys.KeyValueConfigurationFile);

                if (!string.IsNullOrWhiteSpace(configurationFile) && File.Exists(configurationFile))
                {
                    logger.Information("Using configuration values from file '{ConfigurationFile}'", configurationFile);
                    appSettingsBuilder =
                        appSettingsBuilder.Add(new JsonKeyValueConfiguration(configurationFile, false));
                }

                var argsAsParameters = args
                    .Where(arg => arg.StartsWith("-", StringComparison.OrdinalIgnoreCase))
                    .Select(arg => arg.TrimStart('-'))
                    .ToImmutableArray();

                MultiSourceKeyValueConfiguration configuration = appSettingsBuilder
                    .Add(new EnvironmentVariableKeyValueConfigurationSource())
                    .AddCommandLineArgsSettings(argsAsParameters)
                    .Add(new UserJsonConfiguration())
                    .Build();

                logger.Debug("Using configuration: {Configuration}", configuration.SourceChain);

                string logPath = configuration[ConsoleConfigurationKeys.LoggingFilePath];

                string environmentLogLevel =
                    configuration[ConfigurationKeys.LogLevelEnvironmentVariable];

                string configurationLogLevel = configuration[ConfigurationKeys.LogLevel];

                var logLevel =
                    Arbor.App.Extensions.Logging.LogEventLevelExtensions.ParseOrDefault(
                        environmentLogLevel.WithDefault(configurationLogLevel));


                levelSwitch.MinimumLevel = logLevel;

                LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                    .WriteTo.Console(outputTemplate: outputTemplate);

                if (!string.IsNullOrWhiteSpace(logPath))
                {
                    loggerConfiguration = loggerConfiguration.WriteTo.File(logPath);
                }

                if (!hasDefinedLogger)
                {
                    if (logger is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    logger = loggerConfiguration
                        .MinimumLevel.ControlledBy(levelSwitch)
                        .CreateLogger();
                }

                if (!string.IsNullOrWhiteSpace(machineSettings))
                {
                    logger.Information("Using machine specific configuration file '{Settings}'", machineSettings);
                }

                string? nugetSource = args.GetArgumentValueOrDefault("nuget-source");
                string? nugetConfig = args.GetArgumentValueOrDefault("nuget-config");

                var webDeployConfig = new WebDeployConfig(new WebDeployRulesConfig(
                    true,
                    true,
                    false,
                    true,
                    true));

                bool allowPreReleaseEnabled =
                    configuration[ConfigurationKeys.AllowPreReleaseEnvironmentVariable]
                        .ParseAsBooleanOrDefault()
                    || (Debugger.IsAttached
                        && configuration[ConfigurationKeys.ForceAllowPreRelease]
                            .ParseAsBooleanOrDefault());

                string? nuGetExePath = configuration[ConfigurationKeys.NuGetExePath];

                if (string.IsNullOrWhiteSpace(nuGetExePath))
                {
                    logger.Debug("nuget.exe is not specified, downloading with {Tool}", nameof(NuGetDownloadClient));

                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        var nuGetDownloadClient = new NuGetDownloadClient();
                        NuGetDownloadResult nuGetDownloadResult;

                        using (var httpClient = new HttpClient())
                        {
                            nuGetDownloadResult = await nuGetDownloadClient
                                .DownloadNuGetAsync(NuGetDownloadSettings.Default, logger, httpClient, cts.Token)
                                .ConfigureAwait(false);
                        }

                        if (!nuGetDownloadResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                Resources.NuGetExeCouldNotBeDownloaded);
                        }

                        nuGetExePath = nuGetDownloadResult.NuGetExePath;
                    }

                    logger.Debug("Successfully downloaded nuget.exe to '{DownloadedPath}'", nuGetExePath);
                }

                var deployerConfiguration = new DeployerConfiguration(webDeployConfig)
                {
                    NuGetExePath = nuGetExePath,
                    NuGetConfig = nugetConfig.WithDefault(configuration[ConfigurationKeys.NuGetConfig]),
                    NuGetSource = nugetSource.WithDefault(configuration[ConfigurationKeys.NuGetSource]),
                    AllowPreReleaseEnabled = allowPreReleaseEnabled,
                    StopStartIisWebSiteEnabled = configuration[ConfigurationKeys.StopStartIisWebSiteEnabled]
                        .ParseAsBooleanOrDefault(true)
                };

                var nuGetCliSettings = new NuGetCliSettings(
                    deployerConfiguration.NuGetSource,
                    nuGetExePath: deployerConfiguration.NuGetExePath,
                    nugetConfigFile: deployerConfiguration.NuGetConfig);

                var nuGetPackageInstaller =
                    new NuGetPackageInstaller(logger: logger, nugetCliSettings: nuGetCliSettings);

                var deploymentService = new DeploymentService(
                    deployerConfiguration,
                    logger,
                    configuration,
                    new WebDeployHelper(logger),
                    deploymentExecutionDefinition =>
                        IisManager.Create(deployerConfiguration, logger, deploymentExecutionDefinition),
                    nuGetPackageInstaller,
                    new FtpHandlerFactory());

                string temp = configuration[ConfigurationKeys.TempDirectory];

                const string tempEnvironmentVariableName = "temp";

                if (!string.IsNullOrWhiteSpace(temp) && Directory.Exists(temp))
                {
                    Environment.SetEnvironmentVariable(tempEnvironmentVariableName, temp);
                    Environment.SetEnvironmentVariable("tmp", temp);
                }

                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                return new DeployerApp(logger, deploymentService, configuration, levelSwitch, cancellationTokenSource);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.Error("Could not build application");
                throw;
            }
        }

        private static string GetOutputTemplate(ImmutableArray<string> args)
        {
            if (args.Any(arg =>
                arg.Equals(LoggingConstants.PlainOutputFormatEnabled, StringComparison.OrdinalIgnoreCase)))
            {
                string prefix = "";
                if (args.Any(arg =>
                    arg.Equals(LoggingConstants.LoggingCategoryFormatEnabled, StringComparison.OrdinalIgnoreCase)))
                {
                    prefix = "[{Level}] ";
                }

                return $"{prefix}{LoggingConstants.PlainFormat}";
            }

            return LoggingConstants.DefaultFormat;
        }

        private static string? GetMachineSettingsFile(DirectoryInfo currentDirectory)
        {
            if (currentDirectory is null)
            {
                return null;
            }

            currentDirectory.Refresh();

            if (!currentDirectory.Exists)
            {
                return null;
            }

            try
            {
                FileInfo? file = currentDirectory.GetFiles($"{Environment.MachineName}.settings.json").SingleOrDefault();

                if (file is null)
                {
                    if (currentDirectory.Parent is {})
                    {
                        return GetMachineSettingsFile(currentDirectory.Parent);
                    }

                    return null;
                }

                return file.FullName;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                // ignore
                return null;
            }
        }
    }
}