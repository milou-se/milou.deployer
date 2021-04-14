using Microsoft.AspNetCore.Mvc;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.IisHost.Controllers;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class TestAgentController : AgentApiController
    {
        [HttpGet]
        [Route("/test/{agentId}")]
        public IActionResult Index([FromRoute] AgentId agentId) => Ok(agentId);
    }
}