using System.Threading;
using System.Threading.Tasks;
using Marten;
using MediatR;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Marten.Agents;

namespace Milou.Deployer.Web.Marten
{
    public class GetAgentConfigurationQueryHandler : IRequestHandler<GetAgentConfigurationQuery, GetAgentConfigurationQueryResult>
    {
        private readonly IDocumentStore _documentStore;

        public GetAgentConfigurationQueryHandler(IDocumentStore documentStore) => _documentStore = documentStore;

        public async Task<GetAgentConfigurationQueryResult> Handle(GetAgentConfigurationQuery request,
            CancellationToken cancellationToken)
        {
            using var session = _documentStore.QuerySession();

            string agentIdValue = request.AgentId.Value;

            var agentData = await session.LoadAsync<AgentData>(agentIdValue, cancellationToken);

            if (agentData is { })
            {
                return new GetAgentConfigurationQueryResult(new AgentId(agentData.AgentId))
                {
                    AccessToken = agentData.AccessToken
                };
            }

            return new GetAgentConfigurationQueryResult(request.AgentId);
        }
    }
}