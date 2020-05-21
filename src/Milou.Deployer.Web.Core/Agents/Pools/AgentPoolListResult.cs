using System.Collections.Immutable;
using Milou.Deployer.Core.Messaging;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public class AgentPoolListResult : IQueryResult
    {
        public AgentPoolListResult(ImmutableArray<AgentPoolId> agentPools)
        {
            AgentPools = agentPools;
        }

        public ImmutableArray<AgentPoolId> AgentPools { get; }
    }
}