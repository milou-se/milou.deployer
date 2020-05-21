using MediatR;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Agents.Pools;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentDeploymentFailedNotification : INotification
    {
        public AgentDeploymentFailedNotification(string deploymentTaskId, string deploymentTargetId, string agentId)
        {
            DeploymentTaskId = deploymentTaskId;
            DeploymentTargetId = deploymentTargetId;
            AgentId = new AgentId(agentId);
        }

        public string DeploymentTaskId { get; }

        public string DeploymentTargetId { get; }

        public AgentId AgentId { get; }
    }
}