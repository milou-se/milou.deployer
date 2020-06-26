using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly IReadOnlyDictionary<string, string> _environmentVariables;

        public DevelopmentModeConfigurator() {}

        public void Configure(EnvironmentConfiguration environmentConfiguration)
        {
            if (environmentConfiguration.CommandLineArgs.Any(arg =>
                    arg.Equals(ApplicationConstants.DevelopmentMode, StringComparison.OrdinalIgnoreCase)) || Debugger.IsAttached)

            {
                environmentConfiguration.UseVerboseLogging = true;
                environmentConfiguration.HttpEnabled = true;
                environmentConfiguration.IsDevelopmentMode = true;
                environmentConfiguration.EnvironmentName = "Development";
            }

            //|| (_environmentVariables.TryGetValue("development-mode", out var value) &&
            //bool.TryParse(value, out bool enabled) && enabled)
        }
    }
}