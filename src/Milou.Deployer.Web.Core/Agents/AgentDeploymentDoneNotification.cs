using MediatR;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentDeploymentDoneNotification : INotification
    {
        public AgentDeploymentDoneNotification(string deploymentTaskId, string deploymentTargetId, string agentId)
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