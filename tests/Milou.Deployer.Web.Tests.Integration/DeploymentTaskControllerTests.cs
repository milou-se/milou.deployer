using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    public sealed class DeploymentTaskControllerTests : HttpTest
    {
        public DeploymentTaskControllerTests([NotNull] ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task GetTaskPackageShouldReturn401()
        {
            using var client = _server.CreateClient();

            using var response = await client.GetAsync(AgentConstants.DeploymentTaskPackageRoute);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task PostResultShouldReturn401()
        {
            using var client = _server.CreateClient();

            using var response = await client.PostAsync(AgentConstants.DeploymentTaskResult, TestContent.EmptyJson);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}