using System;
using Arbor.App.Extensions.Messaging;
using JetBrains.Annotations;

namespace Milou.Deployer.Web.Core.Deployment.WorkTasks
{
    public class DeploymentTaskCreated : IEvent
    {
        public DeploymentTaskCreated([NotNull] DeploymentTask deploymentTask)
        {
            if (deploymentTask is null)
            {
                throw new ArgumentNullException(nameof(deploymentTask));
            }

            DeploymentTask =
                new DeploymentTaskItem(deploymentTask.DeploymentTaskId, deploymentTask.PackageId,
                    deploymentTask.DeploymentTargetId, deploymentTask.StartedBy);
        }

        public DeploymentTaskItem DeploymentTask { get; }
    }
}