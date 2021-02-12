using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Marten
{
    public record GetAgentConfigurationQuery : IQuery<GetAgentConfigurationQueryResult>
    {
        public GetAgentConfigurationQuery(AgentId agentId) => AgentId = agentId;

        public AgentId AgentId { get; }
    }
}