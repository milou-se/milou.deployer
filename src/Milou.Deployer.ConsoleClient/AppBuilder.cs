using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arbor.KVConfiguration.Core;
using Arbor.KVConfiguration.JsonConfiguration;
using Arbor.KVConfiguration.UserConfiguration;
using Arbor.Tooler;
using JetBrains.Annotations;
using Milou.Deployer.Core;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Extensions;
using Milou.Deployer.IIS;
using Milou.Deployer.Waws;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Milou.Deployer.ConsoleClient
{
    public static class AppBuilder
    {
        public static async Task<DeployerApp> BuildAppAsync([NotNull] string[] args, ILogger logger = null, CancellationToken cancellationToken = default)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            bool hasDefinedLogger = logger != null;

            string outputTemplate = GetOutputTemplate(args);

            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

            logger = logger ?? new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: outputTemplate)
                .MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();

            try
            {
                string machineSettings =
                    GetMachineSettingsFile(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "tools", "Milou.Deployer")));

                AppSettingsBuilder appSettingsBuilder;

                try
                {
                    appSettingsBuilder = KeyValueConfigurationManager
                        .Add(new ReflectionKeyValueConfiguration(typeof(AppBuilder).Assembly))
                        .Add(new ReflectionKeyValueConfiguration(typeof(ConfigurationKeys).Assembly));
                }
                catch (Exception ex) when(!ex.IsFatal())
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

                string configurationFile =
                    Environment.GetEnvironmentVariable(ConfigurationKeys.KeyValueConfigurationFile);

                if (!string.IsNullOrWhiteSpace(configurationFile) && File.Exists(configurationFile))
                {
                    logger.Information("Using configuration values from file '{ConfigurationFile}'", configurationFile);
                    appSettingsBuilder =
                        appSettingsBuilder.Add(new JsonKeyValueConfiguration(configurationFile, false));
                }

                MultiSourceKeyValueConfiguration configuration = appSettingsBuilder
                    .Add(new EnvironmentVariableKeyValueConfigurationSource())
                    .Add(new UserConfiguration())
                    .Build();

                logger.Information("Using configuration: {Configuration}", configuration.SourceChain);

                string logPath = configuration[ConsoleConfigurationKeys.LoggingFilePath];

                string environmentLogLevel =
                    configuration[ConfigurationKeys.LogLevelEnvironmentVariable];

                string configurationLogLevel = configuration[ConfigurationKeys.LogLevel];

                LogEventLevel logLevel = environmentLogLevel.WithDefault(configurationLogLevel)
                    .TryParseOrDefault(LogEventLevel.Information);

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

                var webDeployConfig = new WebDeployConfig(new WebDeployRulesConfig(
                    true,
                    true,
                    false,
                    true,
                    true));

                bool allowPreReleaseEnabled =
                    configuration[ConfigurationKeys.AllowPreReleaseEnvironmentVariable]
                        .ParseAsBooleanOrDefault(false)
                    || (Debugger.IsAttached
                        && configuration[ConfigurationKeys.ForceAllowPreRelease]
                            .ParseAsBooleanOrDefault());

                string nuGetExePath = configuration[ConfigurationKeys.NuGetExePath];

                if (string.IsNullOrWhiteSpace(nuGetExePath))
                {
                    logger.Debug("nuget.exe is not specified, downloading");

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
                                "NuGet exe is not specified and nuget.exe could not be downloaded");
                        }

                        nuGetExePath = nuGetDownloadResult.NuGetExePath;
                    }

                    logger.Debug("Successfully downloaded nuget.exe to '{DownloadedPath}'", nuGetExePath);
                }

                var deployerConfiguration = new DeployerConfiguration(webDeployConfig)
                {
                    NuGetExePath = nuGetExePath,
                    NuGetConfig = configuration[ConfigurationKeys.NuGetConfig],
                    AllowPreReleaseEnabled = allowPreReleaseEnabled,
                    StopStartIisWebSiteEnabled = configuration[ConfigurationKeys.StopStartIisWebSiteEnabled]
                        .ParseAsBooleanOrDefault(true)
                };

                var deploymentService = new DeploymentService(
                    deployerConfiguration,
                    logger,
                    configuration,
                    new WebDeployHelper(),
                    deploymentExecutionDefinition => IISManager.Create(deployerConfiguration, logger, deploymentExecutionDefinition));

                var fileReader = new DeploymentExecutionDefinitionFileReader();

                string temp = configuration[ConfigurationKeys.TempDirectory];

                const string tempEnvironmentVariableName = "temp";

                if (!string.IsNullOrWhiteSpace(temp))
                {
                    if (Directory.Exists(temp))
                    {
                        Environment.SetEnvironmentVariable(tempEnvironmentVariableName, temp);
                        Environment.SetEnvironmentVariable("tmp", temp);
                    }
                }

                CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                return new DeployerApp(logger, deploymentService, fileReader, configuration, cancellationTokenSource);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                logger.Error("Could not build application");
                throw;
            }
        }

        private static string GetOutputTemplate(string[] args)
        {
            if (args.Any(arg =>
                arg.Equals(LoggingConstants.PlainOutputFormatEnabled, StringComparison.OrdinalIgnoreCase)))
            {
                return LoggingConstants.PlainFormat;
            }

            return LoggingConstants.DefaultFormat;
        }

        private static string GetMachineSettingsFile(DirectoryInfo currentDirectory)
        {
            if (currentDirectory is null)
            {
                return null;
            }

            if (currentDirectory.Exists)
            {
                return null;
            }

            try
            {
                FileInfo file = currentDirectory.GetFiles($"{Environment.MachineName}.settings.json").SingleOrDefault();

                if (file is null)
                {
                    if (currentDirectory.Parent != null)
                    {
                        return GetMachineSettingsFile(currentDirectory.Parent);
                    }

                    return null;
                }

                return file.FullName;
            }
            catch (Exception ex) when(!ex.IsFatal())
            {
                // ignore
                return null;
            }
        }
    }
}
