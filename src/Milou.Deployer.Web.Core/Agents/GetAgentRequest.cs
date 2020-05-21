using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class GetAgentRequest : IQuery<AgentInfo?>
    {
        public GetAgentRequest(AgentId agentId) => AgentId = agentId;

        public AgentId AgentId { get; }
    }
}