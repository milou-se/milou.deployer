
using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public class AssignAgentToPoolResult : ICommandResult
    {
        public AgentId? AgentId { get; init; }

        public AgentPoolId? AgentPoolId { get; init; }

        public bool Updated { get; init; }
    }
}