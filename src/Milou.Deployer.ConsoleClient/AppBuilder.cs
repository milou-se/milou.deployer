using System;
using System.Diagnostics;
using System.IO;
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
        public static DeployerApp BuildApp()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Information)
                .WriteTo.Console()
                .CreateLogger();

            AppSettingsBuilder appSettingsBuilder = KeyValueConfigurationManager
                .Add(new ReflectionKeyValueConfiguration(typeof(AppBuilder).Assembly))
                .Add(new ReflectionKeyValueConfiguration(typeof(ConfigurationKeys).Assembly))
                .Add(new AppSettingsKeyValueConfiguration());

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
    }
}
