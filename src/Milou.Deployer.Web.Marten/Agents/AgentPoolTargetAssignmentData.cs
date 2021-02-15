using Milou.Deployer.Web.Core.Agents.Pools;

namespace Milou.Deployer.Web.Marten.Agents
{
    public record AgentPoolTargetAssignmentData
    {
        public string Id { get; set; }

        public AgentPoolId PoolId { get; set; }
    }
}