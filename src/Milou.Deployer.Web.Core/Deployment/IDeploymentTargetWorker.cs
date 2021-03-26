using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Web.Agent;

namespace Milou.Deployer.Web.Core.Deployment
{
    public interface IDeploymentTargetWorker
    {
        DeploymentTargetId TargetId { get; }

        bool IsRunning { get; }

        Task ExecuteAsync(CancellationToken cancellationToken);
    }
}