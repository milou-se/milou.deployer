using System.Collections.Immutable;
using Arbor.KVConfiguration.Core;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class UpdateDeploymentTargetResult : ITargetResult, INotification, ICommandResult
    {
        public UpdateDeploymentTargetResult(string targetName,
            string targetId,
            params ValidationError[] validationErrors)
        {
            TargetName = targetName;
            TargetId = targetId;
            ValidationErrors = validationErrors.ToImmutableArray();
        }

        public string TargetId { get; }

        [PublicAPI]
        public ImmutableArray<ValidationError> ValidationErrors { get; }

        public string TargetName { get; }
    }
}