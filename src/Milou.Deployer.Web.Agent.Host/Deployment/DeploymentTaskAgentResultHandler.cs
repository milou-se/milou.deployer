using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Serilog;

namespace Milou.Deployer.Web.Agent.Host.Deployment
{
    [UsedImplicitly]
    public class DeploymentTaskAgentResultHandler : IRequestHandler<DeploymentTaskAgentResult>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly AgentConfiguration _agentConfiguration;

        public DeploymentTaskAgentResultHandler(IHttpClientFactory httpClientFactory,
            ILogger logger, AgentConfiguration agentConfiguration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _agentConfiguration = agentConfiguration;
        }

        public async Task<Unit> Handle(DeploymentTaskAgentResult request, CancellationToken cancellationToken)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient(HttpConfigurationModule.AgentClient);

            var uriBuilder =
                new UriBuilder(_agentConfiguration.ServerBaseUri) {Path = AgentConstants.DeploymentTaskResult};

            var response = await httpClient.PostAsJson(uriBuilder.Uri, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("Response for deployment task agent result {@Result} failed with status code {StatusCode}", request, response.StatusCode);
            }
            else
            {
                _logger.Debug("Successfully sent deployment task agent result {@Result}", request);
            }

            return Unit.Value;
        }
    }
}