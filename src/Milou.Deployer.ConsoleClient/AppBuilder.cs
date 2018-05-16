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
using Serilog.Events;

namespace Milou.Deployer.ConsoleClient
{
    public static class AppBuilder
    {
        public static DeployerApp BuildApp(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Information)
                .WriteTo.Console()
                .CreateLogger();

            string machineSettings = GetMachineSettingsFile(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory));

            AppSettingsBuilder appSettingsBuilder = KeyValueConfigurationManager
                .Add(new ReflectionKeyValueConfiguration(typeof(AppBuilder).Assembly))
                .Add(new ReflectionKeyValueConfiguration(typeof(ConfigurationKeys).Assembly))
                .Add(new AppSettingsKeyValueConfiguration());

            if (!string.IsNullOrWhiteSpace(machineSettings))
            {
                Log.Logger.Information("Using machine specific configuration file '{Settings}'", machineSettings);
                appSettingsBuilder =
                    appSettingsBuilder.Add(new JsonKeyValueConfiguration(machineSettings, throwWhenNotExists: false));
            }

            string configurationFile = Environment.GetEnvironmentVariable(ConfigurationKeys.KeyValueConfigurationFile);

            if (!string.IsNullOrWhiteSpace(configurationFile) && File.Exists(configurationFile))
            {
                Log.Logger.Information("Using configuration values from file '{ConfigurationFile}'", configurationFile);
                appSettingsBuilder = appSettingsBuilder.Add(new JsonKeyValueConfiguration(configurationFile, false));
            }

            MultiSourceKeyValueConfiguration configuration = appSettingsBuilder
                .Add(new UserConfiguration())
                .Build();

            Log.Logger.Information("Using configuration: {Configuration}", configuration.SourceChain);

            StaticKeyValueConfigurationManager.Initialize(configuration);

            string logPath = StaticKeyValueConfigurationManager.AppSettings[ConsoleConfigurationKeys.LoggingFilePath];

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
                Environment.GetEnvironmentVariable(ConfigurationKeys.LogLevelEnvironmentVariable);

            string configurationLogLevel = StaticKeyValueConfigurationManager.AppSettings[ConfigurationKeys.LogLevel];

            LogEventLevel logLevel = environmentLogLevel.WithDefault(configurationLogLevel)
                .TryParseOrDefault(LogEventLevel.Information);

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Console();

            if (!string.IsNullOrWhiteSpace(logPath))
            {
                loggerConfiguration = loggerConfiguration.WriteTo.File(logPath);
            }

            Log.Logger = loggerConfiguration
                .MinimumLevel.Is(logLevel)
                .CreateLogger();

            var webDeployConfig = new WebDeployConfig(new WebDeployRulesConfig(true, true, false, true, true));

            bool allowPreReleaseEnabled =
                Environment.GetEnvironmentVariable(ConfigurationKeys.AllowPreReleaseEnvironmentVariable)
                    .ParseAsBooleanOrDefault(false)
                || (Debugger.IsAttached
                    && StaticKeyValueConfigurationManager.AppSettings[ConfigurationKeys.ForceAllowPreRelease]
                        .ParseAsBooleanOrDefault());

            var deployerConfiguration = new DeployerConfiguration(webDeployConfig)
            {
                NuGetExePath = StaticKeyValueConfigurationManager.AppSettings[ConfigurationKeys.NuGetExePath],
                AllowPreReleaseEnabled = allowPreReleaseEnabled
            };

            var deploymentService = new DeploymentService(
                deployerConfiguration,
                Log.Logger);

            var fileReader = new DeploymentExecutionDefinitionFileReader();

            string temp = StaticKeyValueConfigurationManager.AppSettings[ConfigurationKeys.TempDirectory];

            const string tempEnvironmentVariableName = "temp";

            if (!string.IsNullOrWhiteSpace(temp))
            {
                if (Directory.Exists(temp))
                {
                    Environment.SetEnvironmentVariable(tempEnvironmentVariableName, temp);
                    Environment.SetEnvironmentVariable("tmp", temp);
                }
            }

            return new DeployerApp(Log.Logger, deploymentService, fileReader);
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
