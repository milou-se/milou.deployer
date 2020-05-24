using Arbor.App.Extensions.Messaging;

namespace Milou.Deployer.Web.Core.Deployment.Environments
{
    public class CreateEnvironment : ICommand<CreateEnvironmentResult>
    {
        public string EnvironmentTypeId { get; set; }

        public string EnvironmentTypeName { get; set; }

        public string PreReleaseBehavior { get; set; }
    }
}