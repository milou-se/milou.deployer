using JetBrains.Annotations;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.IisHost.Areas.Deployment.ViewInputModels
{
    [PublicAPI]
    public class DeploymentTaskInput
    {
        public DeploymentTargetId TargetId { get; set; }

        public string PackageId { get; set; }

        public string Version { get; set; }
    }
}