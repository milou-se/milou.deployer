using Arbor.App.Extensions;
using Arbor.App.Extensions.Configuration;
using Arbor.KVConfiguration.Core;
using Serilog.Events;

namespace Milou.Deployer.Web.Core.Deployment
{
    public class MilouDeployerConfiguration
    {
        public MilouDeployerConfiguration(
            IKeyValueConfiguration keyValueConfiguration,
            string logLevel = "") =>
            LogLevel = logLevel.WithDefault(keyValueConfiguration[ConfigurationConstants.LogLevel] ?? LogEventLevel.Information.ToString());

        public string LogLevel { get; }
    }
}