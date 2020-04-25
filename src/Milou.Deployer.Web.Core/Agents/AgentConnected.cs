using MediatR;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentConnected : INotification
    {
        public AgentConnected(string agentId, string connectionId)
        {
            ConnectionId = connectionId;
            AgentId = agentId;
        }

        public string ConnectionId { get; }

        public string AgentId { get; }
    }
}