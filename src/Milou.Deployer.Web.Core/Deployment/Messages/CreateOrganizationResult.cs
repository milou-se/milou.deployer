using System.Collections.Immutable;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Messaging;
using Arbor.KVConfiguration.Core;

namespace Milou.Deployer.Web.Core.Deployment.Messages
{
    public class CreateOrganizationResult : ICommandResult
    {
        public CreateOrganizationResult(params ValidationError[] validationErrors) =>
            ValidationErrors = validationErrors.SafeToImmutableArray();

        public ImmutableArray<ValidationError> ValidationErrors { get; }
    }
}