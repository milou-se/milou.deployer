using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public class AssignAgentToPool : ICommand<AssignAgentToPoolResult>
    {
        public AssignAgentToPool(AgentPoolId agentPoolId, AgentId agentId)
        {
            AgentPoolId = agentPoolId;
            AgentId = agentId;
        }

        public AgentPoolId AgentPoolId { get; }

        public AgentId AgentId { get; }
    }
}