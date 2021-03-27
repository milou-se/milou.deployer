using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.IisHost.Controllers;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class DeploymentTaskLogController : AgentApiController
    {
        private readonly ILogger _logger;

        public DeploymentTaskLogController(ILogger logger) => _logger = logger;

        [HttpPost]
        [Route(AgentConstants.DeploymentTaskLogRoute, Name = AgentConstants.DeploymentTaskLogRouteName)]
        public async Task<IActionResult> Log([FromBody] SerilogSinkEvents events, [FromServices] IMediator mediator)
        {
            string deploymentTaskId = Request.Headers["x-deployment-task-id"];
            var deploymentTargetId = new DeploymentTargetId(Request.Headers["x-deployment-target-id"]);

            if (events?.Events is null)
            {
                return Ok();
            }

            foreach (SerilogSinkEvent serilogSinkEvent in events.Events.NotNull())
            {
                if (!string.IsNullOrWhiteSpace(deploymentTaskId) &&
                    !string.IsNullOrWhiteSpace(serilogSinkEvent.RenderedMessage))
                {
                    await mediator.Publish(new AgentLogNotification(deploymentTaskId, deploymentTargetId,
                        serilogSinkEvent.RenderedMessage));
                }
            }

            return Ok();
        }
    }
}