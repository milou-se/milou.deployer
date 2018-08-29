using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Arbor.KVConfiguration.Core;
using Arbor.KVConfiguration.JsonConfiguration;
using Arbor.KVConfiguration.SystemConfiguration;
using Arbor.KVConfiguration.UserConfiguration;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Extensions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Milou.Deployer.ConsoleClient
{
    public static class AppBuilder
    {
        public static DeployerApp BuildApp(string[] args)
        {
            string outputTemplate = GetOutputTemplate(args);

            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

            Logger logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: outputTemplate)
                .MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();

            string machineSettings = GetMachineSettingsFile(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));

            AppSettingsBuilder appSettingsBuilder = KeyValueConfigurationManager
                .Add(new ReflectionKeyValueConfiguration(typeof(AppBuilder).Assembly))
                .Add(new ReflectionKeyValueConfiguration(typeof(ConfigurationKeys).Assembly))
                .Add(new AppSettingsKeyValueConfiguration());

            if (!string.IsNullOrWhiteSpace(machineSettings))
            {
                logger.Information("Using machine specific configuration file '{Settings}'", machineSettings);
                appSettingsBuilder =
                    appSettingsBuilder.Add(new JsonKeyValueConfiguration(machineSettings, false));
            }

            string configurationFile = Environment.GetEnvironmentVariable(ConfigurationKeys.KeyValueConfigurationFile);

            if (!string.IsNullOrWhiteSpace(configurationFile) && File.Exists(configurationFile))
            {
                logger.Information("Using configuration values from file '{ConfigurationFile}'", configurationFile);
                appSettingsBuilder = appSettingsBuilder.Add(new JsonKeyValueConfiguration(configurationFile, false));
            }

            MultiSourceKeyValueConfiguration configuration = appSettingsBuilder
                .Add(new EnvironmentVariableKeyValueConfigurationSource())
                .Add(new UserConfiguration())
                .Build();

            logger.Information("Using configuration: {Configuration}", configuration.SourceChain);

            string logPath = configuration[ConsoleConfigurationKeys.LoggingFilePath];

            if (string.IsNullOrWhiteSpace(logPath))
            {
                var currentDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

                bool isUpdate = args.Contains(Commands.Update, StringComparer.OrdinalIgnoreCase);
                bool isUpdating = args.Contains(Commands.Updating, StringComparer.OrdinalIgnoreCase);
                bool isUpdated = args.Contains(Commands.Updated, StringComparer.OrdinalIgnoreCase);

                if (isUpdated || isUpdate || isUpdating)
                {
                    if (isUpdating)
                    {
                        currentDirectory = currentDirectory.Parent;
                    }

                    logPath = Path.Combine(currentDirectory.FullName, "Updates.log");
                }
            }

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

            if (logger is IDisposable disposable)
            {
                disposable.Dispose();
            }

            logger = loggerConfiguration
                .MinimumLevel.ControlledBy(levelSwitch)
                .CreateLogger();

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

            var deployerConfiguration = new DeployerConfiguration(webDeployConfig)
            {
                NuGetExePath = configuration[ConfigurationKeys.NuGetExePath],
                NuGetConfig = configuration[ConfigurationKeys.NuGetConfig],
                AllowPreReleaseEnabled = allowPreReleaseEnabled,
                StopStartIisWebSiteEnabled = configuration[ConfigurationKeys.StopStartIisWebSiteEnabled].ParseAsBooleanOrDefault(true)
            };

            var deploymentService = new DeploymentService(
                deployerConfiguration,
                logger, configuration);

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

            return new DeployerApp(logger, deploymentService, fileReader, configuration);
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
            catch (Exception)
            {
                // ignore
                return null;
            }
        }
    }
}
