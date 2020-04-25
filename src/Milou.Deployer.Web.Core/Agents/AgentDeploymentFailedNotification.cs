using MediatR;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentDeploymentFailedNotification : INotification
    {
        public AgentDeploymentFailedNotification(string deploymentTaskId, string deploymentTargetId, string agentId)
        {
            DeploymentTaskId = deploymentTaskId;
            DeploymentTargetId = deploymentTargetId;
            AgentId = agentId;
        }

        public string DeploymentTaskId { get; }

        public string DeploymentTargetId { get; }

        public string AgentId { get; }
    }
}