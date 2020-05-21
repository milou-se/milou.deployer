using MediatR;
using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Core.Deployment;

namespace Milou.Deployer.Web.Core.Agents.Pools
{
    public class AssignTargetToPool : ICommand<AssignTargetToPoolResult>
    {
        public AssignTargetToPool(AgentPoolId agentPoolId, DeploymentTargetId deploymentTargetId)
        {
            AgentPoolId = agentPoolId;
            DeploymentTargetId = deploymentTargetId;
        }

        public AgentPoolId AgentPoolId { get; }

        public DeploymentTargetId DeploymentTargetId { get; }
    }
}