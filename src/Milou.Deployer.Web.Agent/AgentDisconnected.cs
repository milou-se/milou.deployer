using MediatR;

namespace Milou.Deployer.Web.Agent
{
    public class AgentDisconnected : INotification
    {
        public AgentId AgentId { get; }

        public AgentDisconnected(AgentId agentId)
        {
            AgentId = agentId;
        }
    }
}