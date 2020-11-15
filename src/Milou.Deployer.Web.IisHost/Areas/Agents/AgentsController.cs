using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.IisHost.AspNetCore.Results;
using Milou.Deployer.Web.IisHost.Controllers;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [Area(nameof(Agents))]
    public class AgentsController : BaseApiController
    {
        public const string AgentsRoute = "~/agents";
        public const string AgentsRouteName = nameof(AgentsRoute);
        private readonly AgentsData _agentsData;

        public AgentsController(AgentsData agentsData) => _agentsData = agentsData;

        [HttpGet]
        [Route(AgentsRoute, Name = AgentsRouteName)]
        public async Task<IActionResult> Index()
        {
            var agents = _agentsData.Agents;
            var unknownAgents = _agentsData.UnknownAgents;

            return View(new AgentsViewModel(agents, unknownAgents));
        }

        [HttpPost]
        [Route(AgentsRoute, Name = AgentsRouteName)]
        public async Task<IActionResult> Index(
            CreateAgent createAgent,
            [FromServices] IMediator mediator) =>
            (await mediator.Send(createAgent)).ToActionResult();
    }
}