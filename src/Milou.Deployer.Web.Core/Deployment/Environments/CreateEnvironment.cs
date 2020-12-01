using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Deployment.Environments
{
    public class CreateEnvironment : ICommand<CreateEnvironmentResult>
    {
        public string EnvironmentTypeId { get; init; }

        public string EnvironmentTypeName { get; init; }

        public string PreReleaseBehavior { get; init; }
    }
}