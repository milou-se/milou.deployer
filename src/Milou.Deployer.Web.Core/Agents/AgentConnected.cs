using MediatR;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentConnected : INotification
    {
        public AgentConnected(AgentId agentId, string connectionId)
        {
            ConnectionId = connectionId;
            AgentId = agentId;
        }

        public string ConnectionId { get; }

        public AgentId AgentId { get; }
    }
}