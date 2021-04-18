using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public record GetAgentPoolsQuery : IQuery<AgentPoolListResult>;
}