using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Marten
{
    public record GetAgentConfigurationQueryResult : IQueryResult
    {
        public GetAgentConfigurationQueryResult(AgentId agentId) => AgentId = agentId;

        public AgentId AgentId { get; }

        public string? AccessToken { get; init; }
    }
}