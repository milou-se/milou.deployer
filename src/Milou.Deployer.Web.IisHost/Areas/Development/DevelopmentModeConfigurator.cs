using System;
using System.Linq;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.Configuration;
using Arbor.Primitives;
using JetBrains.Annotations;

namespace Milou.Deployer.Web.IisHost.Areas.Development
{
    [UsedImplicitly]
    public class DevelopmentModeConfigurator : IConfigureEnvironment
    {
        private readonly EnvironmentVariables _environmentVariables;

        public DevelopmentModeConfigurator(EnvironmentVariables environmentVariables) =>
            _environmentVariables = environmentVariables;

        public void Configure(EnvironmentConfiguration environmentConfiguration)
        {
            bool hasDevelopmentInCommandLineArgs = environmentConfiguration.CommandLineArgs.Any(arg =>
                arg.Equals(ApplicationConstants.DevelopmentMode, StringComparison.OrdinalIgnoreCase));

            bool hasDevelopmentInEnvironmentVariables =
                _environmentVariables.Variables.TryGetValue(
                    ApplicationConstants.DevelopmentMode.TrimStart(trimChar: '-'),
                    out var value) && bool.TryParse(value, out bool enabledInEnvironment) && enabledInEnvironment;

            if (hasDevelopmentInCommandLineArgs || hasDevelopmentInEnvironmentVariables)
            {
                environmentConfiguration.UseVerboseLogging = true;
                environmentConfiguration.HttpEnabled = true;
                environmentConfiguration.IsDevelopmentMode = true;
                environmentConfiguration.EnvironmentName = "Development";
            }
        }
    }
}