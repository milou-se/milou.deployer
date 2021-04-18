using System;
using Arbor.App.Extensions.Messaging;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment
{
    public class DeploymentLogNotification : IEvent
    {
        public DeploymentLogNotification([NotNull] DeploymentTargetId deploymentTargetId, [NotNull] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(message));
            }

            DeploymentTargetId = deploymentTargetId;
            Message = message;
        }

        public DeploymentTargetId DeploymentTargetId { get; }

        public string Message { get; }
    }
}