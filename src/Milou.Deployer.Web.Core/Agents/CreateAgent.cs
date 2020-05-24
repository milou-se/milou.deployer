
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class CreateAgent : ICommand<CreateAgentResult>
    {
        public CreateAgent(AgentId agentId) => AgentId = agentId;

        public AgentId AgentId { get; }
    }
}