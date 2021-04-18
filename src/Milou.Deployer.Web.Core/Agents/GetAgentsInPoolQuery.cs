using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Core.Agents.Pools;

namespace Milou.Deployer.Web.Core.Agents
{
    public class GetAgentsInPoolQuery : IQuery<AgentsInPoolResult>
    {
        public GetAgentsInPoolQuery(AgentPoolId agentPoolId) => AgentPoolId = agentPoolId;

        public AgentPoolId AgentPoolId { get; }
    }
}