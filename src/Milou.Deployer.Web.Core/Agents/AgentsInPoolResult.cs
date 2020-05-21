using System.Collections.Generic;
using System.Collections.Immutable;
using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents.Pools;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentsInPoolResult : IQueryResult
    {
        public ImmutableArray<AgentId> Agents { get; }

        public AgentsInPoolResult(IReadOnlyCollection<AgentId> agentIds)
        {
            Agents = agentIds.ToImmutableArray();
        }
    }
}