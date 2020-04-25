using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Milou.Deployer.Web.IisHost.Areas.Deployment.Signaling;
using Milou.Deployer.Web.IisHost.Controllers;
using NuGet.Common;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [Area(nameof(Agents))]
    public class AgentsController : BaseApiController
    {
        private readonly AgentsData _agentsData;

        public AgentsController(AgentsData agentsData)
        {
            _agentsData = agentsData;
        }

        [HttpGet]
        [Route("~/agents")]
        public async Task<IActionResult> Index()
        {
            var agents = _agentsData.Agents;

            return View(new AgentsViewModel(agents));
        }
    }
}