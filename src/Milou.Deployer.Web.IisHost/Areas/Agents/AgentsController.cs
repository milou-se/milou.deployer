using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Agents.Pools;
using Milou.Deployer.Web.IisHost.AspNetCore.Results;
using Milou.Deployer.Web.IisHost.Controllers;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [Area(nameof(Agents))]
    public class AgentsController : BaseApiController
    {
        public const string AgentsRoute = "~/agents";
        public const string AgentsRouteName = nameof(AgentsRoute);
        public const string CreateAgentRoute = "~/agents/create";
        public const string CreateAgentRouteName = nameof(CreateAgentRoute);
        public const string ResetAgentTokenRoute = "~/agents/reset-token";
        public const string ResetAgentTokenRouteName = nameof(ResetAgentTokenRoute);
        public const string ClearAgentWorkTasksRoute = "~/agents/clear";
        public const string ClearAgentWorkTasksRouteName = nameof(ClearAgentWorkTasksRoute);
        private readonly AgentsData _agentsData;

        public AgentsController(AgentsData agentsData) => _agentsData = agentsData;

        [HttpGet]
        [Route(AgentsRoute, Name = AgentsRouteName)]
        public async Task<IActionResult> Index([FromServices] IMediator mediator)
        {
            var connectedAgents = _agentsData.Agents;
            var unknownAgents = _agentsData.UnknownAgents;

            var result = await mediator.Send(new GetAgentsQuery());
            var agentPoolListResult = await mediator.Send(new GetAgentPoolsQuery());
            var assignedAgentsInPoolsResult = await mediator.Send(new GetAssignedAgentsInPoolsQuery());

            return View(new AgentsViewModel(connectedAgents, result.Agents, unknownAgents, agentPoolListResult.AgentPools, assignedAgentsInPoolsResult.AssignedAgents));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        [Route(AgentsRoute, Name = AgentsRouteName)]
        public async Task<IActionResult> Index(
            [FromBody] CreateAgent createAgent,
            [FromServices] IMediator mediator) =>
            this.ToActionResult(await mediator.Send(createAgent), AgentsRouteName);

        [HttpGet]
        [Route(CreateAgentRoute, Name = CreateAgentRouteName)]
        public IActionResult Create() => View();

        [HttpPost]
        [Route(ResetAgentTokenRoute, Name = ResetAgentTokenRouteName)]
        public async Task<IActionResult> ResetToken(
            [FromBody] ResetAgentToken resetToken,
            [FromServices] IMediator mediator) =>
            this.ToActionResult(await mediator.Send(resetToken), AgentsRouteName);

        [HttpPost]
        [Route(ClearAgentWorkTasksRoute, Name = ClearAgentWorkTasksRouteName)]
        public async Task<IActionResult> ClearAgentWorkTasks(
            [FromBody] ClearAgentWorkTasks clearAgentWorkTasks,
            [FromServices] IMediator mediator) =>
            this.ToActionResult(await mediator.Send(clearAgentWorkTasks), AgentsRouteName);
    }
}