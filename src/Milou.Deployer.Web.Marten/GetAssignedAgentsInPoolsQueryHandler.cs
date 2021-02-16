using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Marten;
using MediatR;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents.Pools;
using Milou.Deployer.Web.Marten.Agents;

namespace Milou.Deployer.Web.Marten
{
    public class
        GetAssignedAgentsInPoolsQueryHandler : IRequestHandler<GetAssignedAgentsInPoolsQuery,
            AssignedAgentsInPoolsQueryResult>
    {
        private readonly IDocumentStore _documentStore;

        public GetAssignedAgentsInPoolsQueryHandler(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task<AssignedAgentsInPoolsQueryResult> Handle(GetAssignedAgentsInPoolsQuery request,
            CancellationToken cancellationToken)
        {
            using var session = _documentStore.QuerySession();

            var agentPoolAssignmentData = await session.LoadAsync<AgentPoolAssignmentData>("/agentAssignments", cancellationToken);

            var pools = await session.Query<AgentPoolData>().ToListAsync(token: cancellationToken);
            var agents = await session.Query<AgentData>().ToListAsync(token: cancellationToken);

            return Map(agentPoolAssignmentData ?? new AgentPoolAssignmentData {Id = "/agentAssignments"}, pools, agents);
        }

        private AssignedAgentsInPoolsQueryResult Map(AgentPoolAssignmentData agentPoolAssignmentData,
            IReadOnlyList<AgentPoolData> pools, IReadOnlyList<AgentData> agents)
        {
            (AgentId?, AgentPoolId?)[] agentTuples = agents.Select(a =>
                !agentPoolAssignmentData.Agents.TryGetValue(a.AgentId, out string? value)
                    ? (null, null)
                    : ((AgentId?) new AgentId(a.AgentId),(AgentPoolId?) new AgentPoolId(value))).ToArray();

            var dictionary =  pools.ToImmutableDictionary(pool => new AgentPoolInfo(new AgentPoolId(pool.Id), new AgentPoolName(pool.Name)),

                pool => agentTuples.Where(t => string.Equals(pool.Id, t.Item2?.Value))
                    .Select(t => t.Item1)
                    .NotNull()
                    .ToImmutableArray());

            return new AssignedAgentsInPoolsQueryResult(dictionary);
        }
    }
}