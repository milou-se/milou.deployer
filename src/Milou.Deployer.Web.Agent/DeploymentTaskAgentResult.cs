using MediatR;

namespace Milou.Deployer.Web.Agent
{
    public class DeploymentTaskAgentResult : IRequest<Unit>
    {
        public DeploymentTaskAgentResult(string deploymentTaskId, DeploymentTargetId deploymentTargetId, bool succeeded)
        {
            DeploymentTaskId = deploymentTaskId;
            DeploymentTargetId = deploymentTargetId;
            Succeeded = succeeded;
        }

        public string DeploymentTaskId { get; }

        public DeploymentTargetId DeploymentTargetId { get; }

        public bool Succeeded { get; }
    }
}