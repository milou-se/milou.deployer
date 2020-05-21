using MediatR;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents.Pools;

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