using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentStatusHandler : INotificationHandler<AgentConnected>
    {
        private readonly AgentsData _agents;

        public AgentStatusHandler(AgentsData agents) => _agents = agents;

        public Task Handle(AgentConnected notification, CancellationToken cancellationToken)
        {
            _agents.AgentConnected(notification.AgentId);

            return Task.CompletedTask;
        }
    }
}