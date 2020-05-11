using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;

namespace Milou.Deployer.Web.Agent
{
    /// <summary>
    ///     Executes the deployment task
    /// </summary>
    public interface IDeploymentPackageAgent
    {
        string AgentId { get; }

        Task<ExitCode> RunAsync(
            string deploymentTaskId,
            string deploymentTargetId,
            CancellationToken cancellationToken = default);
    }
}