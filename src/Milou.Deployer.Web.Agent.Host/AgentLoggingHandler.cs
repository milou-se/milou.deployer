using Arbor.App.Extensions.Logging;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Serilog;

namespace Milou.Deployer.Web.Agent.Host
{
    public class AgentLoggingHandler : ILoggerConfigurationHandler
    {
        private readonly AgentConfiguration? _agentConfiguration;

        public AgentLoggingHandler(AgentConfiguration? agentConfiguration = null) => _agentConfiguration = agentConfiguration;

        public LoggerConfiguration Handle(LoggerConfiguration loggerConfiguration)
        {
            if (_agentConfiguration is null
                || string.IsNullOrWhiteSpace(_agentConfiguration.AccessToken)
                || _agentConfiguration?.AgentId() is not {} agentId)
            {
                return loggerConfiguration;
            }

            return loggerConfiguration.Enrich.WithProperty("AgentId", agentId.Value);
        }
    }
}