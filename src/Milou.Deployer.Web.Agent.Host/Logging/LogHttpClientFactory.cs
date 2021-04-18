using System.Net.Http;
using Serilog;
using Serilog.Sinks.Http;

namespace Milou.Deployer.Web.Agent.Host.Logging
{
    public class LogHttpClientFactory
    {
        private readonly IHttpClientFactory _clientFactory;

        public LogHttpClientFactory(IHttpClientFactory clientFactory) => _clientFactory = clientFactory;

        public IHttpClient CreateClient(string deploymentTaskId, DeploymentTargetId deploymentTargetId, AgentId agentId, ILogger logger) =>
            new CustomHttpClient(_clientFactory, deploymentTaskId, deploymentTargetId, agentId, logger);
    }
}