using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class CreateAgentResult : ICommandResult
    {
        public CreateAgentResult(AgentId agentId) => AgentId = agentId;

        public AgentId AgentId { get; }
    }
}