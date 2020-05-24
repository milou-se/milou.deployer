using Arbor.App.Extensions.Messaging;
using MediatR;

namespace Milou.Deployer.Web.Agent
{
    public class AgentDisconnected : IEvent
    {
        public AgentId AgentId { get; }

        public AgentDisconnected(AgentId agentId)
        {
            AgentId = agentId;
        }
    }
}