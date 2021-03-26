using System.Collections.Immutable;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Messaging;
using Arbor.KVConfiguration.Core;
using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;
using Newtonsoft.Json;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class CreateTargetResult : ITargetResult, ICommandResult
    {
        public CreateTargetResult(DeploymentTargetId targetId, string targetName)
        {
            TargetId = targetId;
            TargetName = targetName;
            ValidationErrors = ImmutableArray<ValidationError>.Empty;
        }

        public CreateTargetResult(params ValidationError[] validationErrors) =>
            ValidationErrors = validationErrors.SafeToImmutableArray();

        [JsonConstructor]
        private CreateTargetResult(string targetName, ValidationError[] validationErrors)
        {
            TargetName = targetName;
            ValidationErrors = validationErrors.ToImmutableArray();
        }

        public DeploymentTargetId TargetId { get; }

        [PublicAPI]
        public ImmutableArray<ValidationError> ValidationErrors { get; }

        [PublicAPI]
        public string TargetName { get; }
    }
}