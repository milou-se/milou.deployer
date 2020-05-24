using System.Collections.Immutable;
using Arbor.App.Extensions.Messaging;


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