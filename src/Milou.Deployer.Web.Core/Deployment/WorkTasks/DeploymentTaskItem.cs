using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment.WorkTasks
{
    public class DeploymentTaskItem
    {
        public DeploymentTaskItem(string deploymentTaskId,
            string packageVersion,
            DeploymentTargetId deploymentTargetId,
            string? startedBy)
        {
            DeploymentTaskId = deploymentTaskId;
            PackageVersion = packageVersion;
            DeploymentTargetId = deploymentTargetId;
            StartedBy = startedBy;
        }

        public string DeploymentTaskId { get; }

        public string PackageVersion { get; }

        public DeploymentTargetId DeploymentTargetId { get; }

        public string? StartedBy { get; }
    }
}