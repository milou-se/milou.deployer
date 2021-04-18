using System.Net;
using System.Text.Unicode;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    public sealed class AgentTaskLogControllerTests : HttpTest
    {
        public AgentTaskLogControllerTests([NotNull] ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task PostDeploymentTaskLogRouteShouldReturn401()
        {
            using var client = _server.CreateClient();

            using var response = await client.PostAsync(AgentConstants.DeploymentTaskLogRoute, TestContent.EmptyJson);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}