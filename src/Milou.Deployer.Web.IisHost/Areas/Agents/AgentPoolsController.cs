using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Milou.Deployer.Web.Core.Agents.Pools;
using Milou.Deployer.Web.IisHost.AspNetCore.Results;
using Milou.Deployer.Web.IisHost.Controllers;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [Area(nameof(Agents))]
    public class AgentPoolsController : BaseApiController
    {
        public const string AgentPoolsRoute = "~/agent-pools";
        public const string AgentPoolsRouteName = nameof(AgentPoolsRoute);
        public const string CreateAgentPoolRoute = "~/agent-pools/create";
        public const string CreateAgentPoolRouteName = nameof(CreateAgentPoolRoute);
        public const string AssignAgentToPoolRoute = "~/agent-pools/assignment";
        public const string AssignAgentToPoolRouteName = nameof(AssignAgentToPoolRoute);

        [HttpGet]
        [Route(AgentPoolsRoute, Name = AgentPoolsRouteName)]
        public async Task<IActionResult> Index([FromServices] IMediator mediator)
        {
            var result = await mediator.Send(new GetAssignedAgentsInPoolsQuery());

            return View(new AgentPoolsViewModel(result.AssignedAgents));
        }

        [HttpGet]
        [Route(CreateAgentPoolRoute, Name = CreateAgentPoolRouteName)]
        public IActionResult Create() => View();

        [HttpPost]
        [Route(AgentPoolsRoute, Name = AgentPoolsRouteName)]
        public async Task<IActionResult> Index(
            [FromBody] CreateAgentPool createAgentPool,
            [FromServices] IMediator mediator) =>
            (await mediator.Send(createAgentPool)).ToActionResult();

        [HttpPost]
        [Route(AssignAgentToPoolRoute, Name = AssignAgentToPoolRouteName)]
        public async Task<IActionResult> Assign(
            [FromBody] AssignAgentToPool assignAgentToPool,
            [FromServices] IMediator mediator) =>
            (await mediator.Send(assignAgentToPool)).ToActionResult();
    }
}