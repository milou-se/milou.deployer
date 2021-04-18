using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [UsedImplicitly]
    public class AgentStatusHandler : INotificationHandler<AgentConnected>, INotificationHandler<AgentDisconnected>
    {
        private readonly AgentsData _agents;

        public AgentStatusHandler(AgentsData agents) => _agents = agents;

        public Task Handle(AgentConnected notification, CancellationToken cancellationToken)
        {
            _agents.AgentConnected(notification);

            return Task.CompletedTask;
        }

        public Task Handle(AgentDisconnected notification, CancellationToken cancellationToken)
        {
            _agents.AgentDisconnected(notification);

            return Task.CompletedTask;
        }
    }
}