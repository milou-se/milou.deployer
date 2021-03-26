using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentLogNotification : IEvent
    {
        public AgentLogNotification(string deploymentTaskId, DeploymentTargetId deploymentTargetId, string message)
        {
            DeploymentTaskId = deploymentTaskId;
            DeploymentTargetId = deploymentTargetId;
            Message = message;
        }

        public DeploymentTargetId DeploymentTargetId { get; }

        public string Message { get; }

        public string DeploymentTaskId { get; }
    }
}