using System.Collections.Immutable;
using Arbor.App.Extensions;
using Arbor.KVConfiguration.Core;
using Milou.Deployer.Core.Messaging;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class CreateOrganizationResult : ICommandResult
    {
        public CreateOrganizationResult(params ValidationError[] validationErrors) =>
            ValidationErrors = validationErrors.SafeToImmutableArray();

        public ImmutableArray<ValidationError> ValidationErrors { get; }
    }
}