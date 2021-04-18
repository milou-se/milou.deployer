using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [UsedImplicitly]
    public class UnknownAgentHandler : INotificationHandler<UnknownAgentConnected>
    {
        private readonly AgentsData _agents;

        public UnknownAgentHandler(AgentsData agents) => _agents = agents;

        public Task Handle(UnknownAgentConnected notification, CancellationToken cancellationToken)
        {
            _agents.UnknownAgentConnected(notification);

            return Task.CompletedTask;
        }
    }
}