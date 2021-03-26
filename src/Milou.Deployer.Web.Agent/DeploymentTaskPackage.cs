using System.Collections.Immutable;

namespace Milou.Deployer.Web.Agent
{
    public sealed record DeploymentTaskPackage(
        string DeploymentTaskId,
        DeploymentTargetId DeploymentTargetId,
        string AgentId)
    {
        public ImmutableArray<string> DeployerProcessArgs { get; init; }

        public string? ManifestJson { get; init; }

        public string? PublishSettingsXml { get; init; }

        public string? NugetConfigXml { get; init; }
    }
}