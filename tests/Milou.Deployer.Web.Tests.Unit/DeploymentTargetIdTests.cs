using FluentAssertions;
using Milou.Deployer.Web.Agent;
using Xunit;

namespace Milou.Deployer.Web.Tests.Unit
{
    public class DeploymentTargetIdTests
    {
        [Fact]
        public void CtorShouldCreateInstanceForValid()
        {
            var deploymentTargetId = new DeploymentTargetId("abc");

            deploymentTargetId.Should().NotBeNull();
        }
    }
}