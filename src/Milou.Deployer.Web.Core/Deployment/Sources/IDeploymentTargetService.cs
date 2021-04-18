using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment.Sources
{
    public interface IDeploymentTargetService
    {
        Task<DeploymentTarget?> GetDeploymentTargetAsync(
            DeploymentTargetId deploymentTargetId,
            CancellationToken cancellationToken = default);
    }
}