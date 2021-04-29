using System.Net;
using Milou.Deployer.Tests.Integration;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class WhenMakingHttpGetRequestToClearAgent : TestBase<ClearAgentRequest>
    {
        public WhenMakingHttpGetRequestToClearAgent(
            ClearAgentRequest webFixture,
            ITestOutputHelper output) : base(webFixture, output)
        {
        }

        [Fact(Skip = "WIP")]
        //[ConditionalFact]
        public void ThenItShouldReturnHttpStatusCodeOk200()
        {
            Assert.Null(WebFixture.Exception);

            Output.WriteLine($"Response status code {WebFixture?.ResponseMessage?.StatusCode}");

            Assert.Equal(HttpStatusCode.OK, WebFixture?.ResponseMessage?.StatusCode);
        }
    }
}