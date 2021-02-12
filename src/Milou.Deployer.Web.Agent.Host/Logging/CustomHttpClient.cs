using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Serilog.Sinks.Http;

namespace Milou.Deployer.Web.Agent.Host.Logging
{
    public sealed class CustomHttpClient : IHttpClient
    {
        private readonly string _deploymentTargetId;
        private readonly string _deploymentTaskId;
        private readonly IHttpClientFactory _httpClientFactory;

        public CustomHttpClient(IHttpClientFactory httpClientFactory,
            string deploymentTaskId,
            string deploymentTargetId)
        {
            _httpClientFactory = httpClientFactory;
            _deploymentTaskId = deploymentTaskId;
            _deploymentTargetId = deploymentTargetId;
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

            request.Headers.Add("x-deployment-task-id", _deploymentTaskId);
            request.Headers.Add("x-deployment-target-id", _deploymentTargetId);
            HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(request);

            return httpResponseMessage;
        }
    }
}