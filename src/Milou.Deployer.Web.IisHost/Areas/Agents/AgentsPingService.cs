using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [UsedImplicitly]
    public class AgentsPingService : BackgroundService
    {
        private readonly IHubContext<AgentHub> _agentHub;

        public AgentsPingService(IHubContext<AgentHub> agentHub) => _agentHub = agentHub;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                await _agentHub.Clients.All.SendAsync(AgentConstants.SignalRPingCommand,"Ping!", cancellationToken: stoppingToken);
            }
        }
    }
}