using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Marten;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Deployment;

namespace Milou.Deployer.Web.Marten.DeploymentTasks
{
    public class DeploymentTaskPackageStore : IDeploymentTaskPackageStore
    {
        private readonly IDocumentStore _martenStore;

        public DeploymentTaskPackageStore(IDocumentStore martenStore) => _martenStore = martenStore;

        public async Task<DeploymentTaskPackage?> GetDeploymentTaskPackageAsync(string deploymentTaskId,
            CancellationToken cancellationToken)
        {
            using IDocumentSession lightweightSession = _martenStore.LightweightSession();

            DeploymentTaskPackageData found = await lightweightSession.Query<DeploymentTaskPackageData>()
                .SingleOrDefaultAsync(data => data.Id == deploymentTaskId, cancellationToken);

            if (found is null)
            {
                return null;
            }

            return Map(found);
        }

        private DeploymentTaskPackage Map(DeploymentTaskPackageData data) =>
            new (
                data.Id,
                new DeploymentTargetId(data.DeploymentTargetId),
                data.AgentId)
                {
                    ManifestJson = data.ManifestJson,
                    NugetConfigXml = data.NuGetConfigXml,
                    PublishSettingsXml = data.PublishSettingsXml,
                    DeployerProcessArgs = data.ProcessArgs.ToImmutableArray()
                };
    }
}