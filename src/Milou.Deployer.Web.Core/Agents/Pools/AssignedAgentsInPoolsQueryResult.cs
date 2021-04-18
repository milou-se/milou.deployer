using System.Collections.Immutable;
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public record AssignedAgentsInPoolsQueryResult(ImmutableDictionary<AgentPoolInfo, ImmutableArray<AgentId>> AssignedAgents) : IQueryResult;
}