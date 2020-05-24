using System;
using Arbor.App.Extensions.Messaging;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class DeploymentMetadataLog : IEvent
    {
        public DeploymentMetadataLog(
            [NotNull] DeploymentTask deploymentTask,
            [NotNull] DeploymentTaskResult result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (string.IsNullOrWhiteSpace(result.Metadata))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(result));
            }

            DeploymentTask = deploymentTask ?? throw new ArgumentNullException(nameof(deploymentTask));
            Result = result;
        }

        public DeploymentTask DeploymentTask { get; }

        [NotNull]
        public DeploymentTaskResult Result { get; }

        public override string ToString() => $"{nameof(DeploymentTask)}: {DeploymentTask}, {nameof(Result)}: {Result}";
    }
}