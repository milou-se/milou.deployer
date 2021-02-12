using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [UsedImplicitly]
    public class AgentLifeCycleService : BackgroundService
    {
        private readonly IHubContext<AgentHub> _hubContext;

        public AgentLifeCycleService(IHubContext<AgentHub> hubContext) => _hubContext = hubContext;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            await stoppingToken;

            await _hubContext.Clients.All.SendAsync("ServerShuttingDown", CancellationToken.None);
        }
    }
}