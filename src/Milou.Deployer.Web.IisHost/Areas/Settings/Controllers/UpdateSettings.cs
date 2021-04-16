using System;
using MediatR;

namespace Milou.Deployer.Web.IisHost.Areas.Settings.Controllers
{
    public class UpdateSettings : IRequest<Unit>
    {
        public UpdateSettings(TimeSpan? cacheTime, NexusUpdate? nexusConfig, AutoDeployUpdate autoDeploy, DefaultNugetConfigUpdate? defaultNugetConfig)
        {
            CacheTime = cacheTime;
            NexusConfig = nexusConfig;
            AutoDeploy = autoDeploy;
            DefaultNuGetConfig = defaultNugetConfig;
        }

        public TimeSpan? CacheTime { get; }

        public NexusUpdate? NexusConfig { get; }

        public DefaultNugetConfigUpdate? DefaultNuGetConfig { get; }

        public AutoDeployUpdate AutoDeploy { get; }

        public TimeSpan? ApplicationSettingsCacheTimeout { get; set; }

        public TimeSpan? DefaultMetadataTimeout { get; set; }

        public TimeSpan? MetadataCacheTimeout { get; set; }

        public string? AgentExe { get; set; }

        public bool? HostAgentEnabled { get; set; }
    }
}