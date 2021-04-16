using System.Diagnostics.Tracing;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Serilog;
using Serilog.Sinks.Http;

namespace Milou.Deployer.Web.Agent.Host.Logging
{
    public sealed class CustomHttpClient : IHttpClient
    {
        private readonly DeploymentTargetId _deploymentTargetId;
        private readonly AgentId _agentId;
        private readonly ILogger _logger;
        private readonly string _deploymentTaskId;
        private readonly IHttpClientFactory _httpClientFactory;

        public CustomHttpClient(IHttpClientFactory httpClientFactory,
            string deploymentTaskId,
            DeploymentTargetId deploymentTargetId,
            AgentId agentId,
            ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _deploymentTaskId = deploymentTaskId;
            _deploymentTargetId = deploymentTargetId;
            _agentId = agentId;
            _logger = logger;
        }

        public void Dispose()
        {
            // ignore
        }

        public void Configure(IConfiguration configuration)
        {
            // interface method
        }

        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            HttpClient httpClient = _httpClientFactory.CreateClient(HttpConfigurationModule.AgentLoggerClient);

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri) {Content = content};

            request.Headers.Add("X-Deployment-Task-Id", _deploymentTaskId);
            request.Headers.Add("X-Deployment-Target-Id", _deploymentTargetId.TargetId);
            HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(request);

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                _logger.Warning("Failed to send log item from agent {AgentId} to server for deployment task id {DeploymentTaskId}, deployment target id {DeploymentTargetId}", _agentId, _deploymentTaskId, _deploymentTargetId);
            }

            return httpResponseMessage;
        }
    }
}