using System.Collections.Immutable;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents.Pools;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentPoolsViewModel
    {
        public AgentPoolsViewModel(ImmutableDictionary<AgentPoolInfo, ImmutableArray<AgentId>> assignedAgents) =>
            AssignedAgents = assignedAgents;

        public ImmutableDictionary<AgentPoolInfo, ImmutableArray<AgentId>> AssignedAgents { get; }
        public ImmutableArray<AgentPoolInfo> AgentPools => AssignedAgents.Keys.ToImmutableArray();
    }
}