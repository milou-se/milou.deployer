using System.Collections.Immutable;
using Arbor.App.Extensions.Messaging;
using Arbor.KVConfiguration.Core;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class UpdateDeploymentTargetResult : ITargetResult, IEvent, ICommandResult
    {
        public UpdateDeploymentTargetResult(string targetName,
            DeploymentTargetId targetId,
            params ValidationError[] validationErrors)
        {
            TargetName = targetName;
            TargetId = targetId;
            ValidationErrors = validationErrors.ToImmutableArray();
        }

        public DeploymentTargetId TargetId { get; }

        [PublicAPI]
        public ImmutableArray<ValidationError> ValidationErrors { get; }

        public string TargetName { get; }
    }
}