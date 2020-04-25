using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [UsedImplicitly]
    public class AgentStatusHandler : INotificationHandler<AgentConnected>
    {
        private readonly AgentsData _agents;

        public AgentStatusHandler(AgentsData agents) => _agents = agents;

        public Task Handle(AgentConnected notification, CancellationToken cancellationToken)
        {
            _agents.AgentConnected(notification);

            return Task.CompletedTask;
        }
    }
}