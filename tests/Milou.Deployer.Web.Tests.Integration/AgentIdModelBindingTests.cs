using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    public sealed class AgentIdModelBindingTests : HttpTest
    {
        public AgentIdModelBindingTests([NotNull] ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task PostDeploymentTaskLogRouteShouldReturn401()
        {
            using var client = _server.CreateClient();
            client.AddTestBasicAuthentication();

            using var response = await client.GetAsync("test/abc");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var agentId = await response.Content.ReadFromJsonAsync(typeof(AgentId)) as AgentId;

            agentId.Should().NotBeNull();

            agentId!.Value.Should().Be("abc");
        }
    }
}