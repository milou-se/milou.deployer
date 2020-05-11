using MediatR;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment
{
    public class CreateDeploymentTaskPackage : IRequest<Unit>
    {
        public CreateDeploymentTaskPackage(DeploymentTaskPackage deploymentTaskPackage) =>
            DeploymentTaskPackage = deploymentTaskPackage;

        public DeploymentTaskPackage DeploymentTaskPackage { get; }
    }
}