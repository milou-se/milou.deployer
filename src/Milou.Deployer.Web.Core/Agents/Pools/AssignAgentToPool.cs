using System.Collections.Immutable;
using Arbor.App.Extensions.Messaging;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    [UsedImplicitly]
    public sealed record AssignAgentToPool
        (AgentPoolId AgentPoolId, AgentId AgentId) : ICommand<AssignAgentToPoolResult>;

    public sealed record GetAssignedAgentsInPoolsQuery
        : IQuery<AssignedAgentsInPoolsQueryResult>;

    public record AssignedAgentsInPoolsQueryResult(ImmutableDictionary<AgentPoolInfo, ImmutableArray<AgentId>> AssignedAgents) : IQueryResult;
}