using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.IisHost.Areas.Agents;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    [UsedImplicitly]
    public class ClearAgentRequest : HttpRequestFixture
    {
        public ClearAgentRequest([NotNull] IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink, CreateRequest())
        {
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new(HttpMethod.Post, "http://localhost" + AgentsController.ClearAgentWorkTasksRoute.TrimStart('~'))
            {
                Content = new StringContent(JsonConvert.SerializeObject(new ClearAgentWorkTasks(new AgentId("Agent1"))),Encoding.UTF8,"application/json")
            };
        }
    }
}