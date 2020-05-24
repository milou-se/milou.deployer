using System;
using Arbor.App.Extensions.Messaging;
using JetBrains.Annotations;

namespace Milou.Deployer.Web.Core.Deployment
{
    public class DeploymentLogNotification : IEvent
    {
        public DeploymentLogNotification([NotNull] string deploymentTargetId, [NotNull] string message)
        {
            if (string.IsNullOrWhiteSpace(deploymentTargetId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(deploymentTargetId));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(message));
            }

            DeploymentTargetId = deploymentTargetId;
            Message = message;
        }

        public string DeploymentTargetId { get; }

        public string Message { get; }
    }
}