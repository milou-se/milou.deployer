using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Milou.Deployer.Web.Core.Settings;
using Milou.Deployer.Web.IisHost.Areas.Settings.Controllers;

namespace Milou.Deployer.Web.IisHost.Areas.Settings
{
    [UsedImplicitly]
    public class UpdateSettingsHandler : IRequestHandler<UpdateSettings, Unit>
    {
        private readonly IApplicationSettingsStore _settingsStore;

        public UpdateSettingsHandler(IApplicationSettingsStore martenSettingsStore) =>
            _settingsStore = martenSettingsStore;

        public async Task<Unit> Handle(UpdateSettings request, CancellationToken cancellationToken)
        {
            ApplicationSettings applicationSettings = await _settingsStore.GetApplicationSettings(cancellationToken);

            if (request.CacheTime.HasValue)
            {
                applicationSettings.CacheTime = request.CacheTime.Value;
            }

            if (request.NexusConfig is { })
            {
                applicationSettings.NexusConfig.HmacKey = request.NexusConfig.HmacKey;
                applicationSettings.NexusConfig.NuGetSource = request.NexusConfig.NuGetSource;
                applicationSettings.NexusConfig.NuGetConfig = request.NexusConfig.NuGetConfig;
            }

            applicationSettings.AutoDeploy.Enabled = request.AutoDeploy.Enabled;
            applicationSettings.AutoDeploy.PollingEnabled = request.AutoDeploy.PollingEnabled;

            if (request.ApplicationSettingsCacheTimeout is {TotalSeconds: >= 0.5D})
            {
                applicationSettings.ApplicationSettingsCacheTimeout = request.ApplicationSettingsCacheTimeout.Value;
            }

            if (request.DefaultMetadataTimeout is {TotalSeconds: >= 0.5D})
            {
                applicationSettings.DefaultMetadataRequestTimeout = request.DefaultMetadataTimeout.Value;
            }

            if (request.MetadataCacheTimeout is {TotalSeconds: >= 0.5D})
            {
                applicationSettings.MetadataCacheTimeout = request.MetadataCacheTimeout.Value;
            }

            if (request.DefaultNuGetConfig is { })
            {
                applicationSettings.DefaultNuGetConfig.NuGetConfig = request.DefaultNuGetConfig.NuGetConfig;
                applicationSettings.DefaultNuGetConfig.NuGetSource = request.DefaultNuGetConfig.NuGetSource;
            }

            applicationSettings.AgentExe = string.IsNullOrWhiteSpace(request.AgentExe) ? null : request.AgentExe;

            if (request.HostAgentEnabled.HasValue)
            {
                applicationSettings.HostAgentEnabled = request.HostAgentEnabled.Value;
            }

            await _settingsStore.Save(applicationSettings);

            return Unit.Value;
        }
    }
}