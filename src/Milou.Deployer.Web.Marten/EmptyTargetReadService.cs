using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Sources;
using Milou.Deployer.Web.Core.Deployment.Targets;

namespace Milou.Deployer.Web.Marten
{
    public class EmptyTargetReadService : IDeploymentTargetReadService
    {
        public Task<DeploymentTarget?> GetDeploymentTargetAsync(DeploymentTargetId deploymentTargetId,
            CancellationToken cancellationToken = default) => Task.FromResult((DeploymentTarget?)null);

        public Task<ImmutableArray<OrganizationInfo>>
            GetOrganizationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ImmutableArray<OrganizationInfo>.Empty);

        public Task<ImmutableArray<DeploymentTarget>> GetDeploymentTargetsAsync(TargetOptions? options = default,
            CancellationToken stoppingToken = default) => Task.FromResult(ImmutableArray<DeploymentTarget>.Empty);

        public Task<ImmutableArray<ProjectInfo>> GetProjectsAsync(string organizationId,
            CancellationToken cancellationToken = default) => Task.FromResult(ImmutableArray<ProjectInfo>.Empty);
    }
}