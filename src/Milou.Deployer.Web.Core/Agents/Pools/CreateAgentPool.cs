using Arbor.App.Extensions.Messaging;
using MediatR;


namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public class CreateAgentPool : ICommand<CreateAgentPoolResult>
    {
        public CreateAgentPool(AgentPoolId agentPoolId) => AgentPoolId = agentPoolId;

        public AgentPoolId AgentPoolId { get; }
    }
}