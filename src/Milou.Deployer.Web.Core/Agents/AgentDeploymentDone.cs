using Arbor.App.Extensions.Messaging;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Agents
{
    public class AgentDeploymentDone : IEvent
    {
        public AgentDeploymentDone(string deploymentTaskId, DeploymentTargetId deploymentTargetId, string agentId)
        {
            DeploymentTaskId = deploymentTaskId;
            DeploymentTargetId = deploymentTargetId;
            AgentId = new AgentId(agentId);
        }

        public string DeploymentTaskId { get; }

        public DeploymentTargetId DeploymentTargetId { get; }

        public AgentId AgentId { get; }
    }
}