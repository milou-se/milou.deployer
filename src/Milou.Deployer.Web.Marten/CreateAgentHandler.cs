using System;
using System.Threading;
using System.Threading.Tasks;
using Marten;
using MediatR;
using Milou.Deployer.Web.Core.Agents;
using Milou.Deployer.Web.Marten.Agents;

namespace Milou.Deployer.Web.Marten
{
    public class CreateAgentHandler : IRequestHandler<CreateAgent, CreateAgentResult>
    {
        private readonly IDocumentStore _documentStore;
        private readonly IMediator _mediator;

        public CreateAgentHandler(IDocumentStore documentStore, IMediator mediator)
        {
            _documentStore = documentStore;
            _mediator = mediator;
        }

        public async Task<CreateAgentResult> Handle(CreateAgent request, CancellationToken cancellationToken)
        {
            using var session = _documentStore.OpenSession();

            string agentIdValue = request.AgentId.Value;

            var agentData = await session.LoadAsync<AgentData>(agentIdValue, cancellationToken);

            if (agentData is { })
            {
                throw new InvalidOperationException($"The agent '{agentIdValue}' already exists");
            }

            var result = await _mediator.Send(new CreateAgentInstallConfiguration(request.AgentId));

            agentData = new AgentData
            {
                AgentId = agentIdValue,
                AccessToken = result.AccessToken
            };

            session.Store(agentData);

            await session.SaveChangesAsync(cancellationToken);


            return new CreateAgentResult(request.AgentId);
        }
    }
}