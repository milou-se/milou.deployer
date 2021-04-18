using System.Collections.Immutable;
using System.Linq;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Core.Agents.Pools;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentsViewModel
    {
        public AgentsViewModel(ImmutableArray<AgentInfo> connectedAgents,
            ImmutableArray<AgentInfo> agents,
            ImmutableDictionary<AgentId, string> unknownAgents,
            ImmutableArray<AgentPoolInfo> agentPools,
            ImmutableDictionary<AgentPoolInfo, ImmutableArray<AgentId>> assignedAgents)
        {
            ConnectedAgents = connectedAgents;
            DisconnectedAgents = agents
                .Where(agent => !connectedAgents.Any(connectedAgent => connectedAgent.Id == agent.Id))
                .ToImmutableArray();
            Agents = agents;
            UnknownAgents = unknownAgents;
            AgentPools = agentPools;
            AssignedAgents = assignedAgents;
        }

        public ImmutableArray<AgentInfo> ConnectedAgents { get; }

        public ImmutableArray<AgentInfo> DisconnectedAgents { get; }

        public ImmutableArray<AgentInfo> Agents { get; }

        public ImmutableDictionary<AgentId, string> UnknownAgents { get; }
        public ImmutableArray<AgentPoolInfo> AgentPools { get; }

        public ImmutableDictionary<AgentPoolInfo, ImmutableArray<AgentId>> AssignedAgents { get; }

        public AgentPoolId CurrentPool(AgentId agentId) =>
            AssignedAgents.SingleOrDefault(pair => pair.Value.Contains(agentId)).Key?.AgentPoolId ?? AgentPoolId.Empty;
    }
}