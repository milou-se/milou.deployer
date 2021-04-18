﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Marten;
using Milou.Deployer.Web.Core.Caching;
using Milou.Deployer.Web.Core.Integration.Nexus;
using Milou.Deployer.Web.Core.Settings;
using Milou.Deployer.Web.Marten.AutoDeploy;
using Milou.Deployer.Web.Marten.Settings;

namespace Milou.Deployer.Web.Marten
{
    public class MartenSettingsStore : IApplicationSettingsStore
    {
        private const string AppSettings = "appsettings";

        private readonly IDocumentStore _documentStore;

        private readonly ICustomMemoryCache _memoryCache;

        public MartenSettingsStore(IDocumentStore documentStore, ICustomMemoryCache memoryCache)
        {
            _documentStore = documentStore;
            _memoryCache = memoryCache;
        }

        public async Task<ApplicationSettings> GetApplicationSettings(CancellationToken cancellationToken)
        {
            if (_memoryCache.TryGetValue(AppSettings, out ApplicationSettings? applicationSettings))
            {
                return applicationSettings!;
            }

            using IQuerySession querySession = _documentStore.QuerySession();
            ApplicationSettingsData? applicationSettingsData =
                await querySession.LoadAsync<ApplicationSettingsData>(AppSettings, cancellationToken);

            applicationSettings = Map(applicationSettingsData ?? new ApplicationSettingsData());

            return applicationSettings;
        }

        public async Task Save(ApplicationSettings applicationSettings)
        {
            using (IDocumentSession querySession = _documentStore.OpenSession())
            {
                ApplicationSettingsData data = MapToData(applicationSettings);
                querySession.Store(data);

                await querySession.SaveChangesAsync();
            }

            _memoryCache.SetValue(AppSettings, applicationSettings,
                applicationSettings.ApplicationSettingsCacheTimeout);
        }

        private ApplicationSettings Map(ApplicationSettingsData? applicationSettingsData)
        {
            var applicationSettingsCacheTime = TimeSpan.FromSeconds(300);

            var applicationSettingsCacheTimeout = TimeSpan.FromMinutes(10);
            var metadataCacheTimeout = TimeSpan.FromMinutes(5);
            var applicationSettings = new ApplicationSettings
            {
                CacheTime = applicationSettingsData?.CacheTime ?? applicationSettingsCacheTime,
                NexusConfig = MapFromNexusData(applicationSettingsData?.NexusConfig),
                DefaultNuGetConfig = MapFromNuGetData(applicationSettingsData?.DefaultNuGetConfig),
                AutoDeploy = MapAutoDeploy(applicationSettingsData?.AutoDeploy),
                DefaultMetadataRequestTimeout =
                    applicationSettingsData?.DefaultMetadataTimeout ?? TimeSpan.FromSeconds(30),
                ApplicationSettingsCacheTimeout =
                    applicationSettingsData?.ApplicationSettingsCacheTimeout ?? applicationSettingsCacheTimeout,
                MetadataCacheTimeout = applicationSettingsData?.MetadataCacheTimeout ?? metadataCacheTimeout,
                AgentExe = applicationSettingsData?.AgentExe,
                HostAgentEnabled = applicationSettingsData?.HostAgentEnabled ?? false,
            };

            if (applicationSettings.CacheTime <= TimeSpan.Zero || applicationSettings.CacheTime.TotalSeconds < 1)
            {
                applicationSettings.CacheTime = applicationSettingsCacheTime;
            }

            if (applicationSettings.ApplicationSettingsCacheTimeout <= TimeSpan.Zero ||
                applicationSettings.ApplicationSettingsCacheTimeout.TotalSeconds < 1)
            {
                applicationSettings.ApplicationSettingsCacheTimeout = metadataCacheTimeout;
            }

            if (applicationSettings.MetadataCacheTimeout <= TimeSpan.Zero ||
                applicationSettings.MetadataCacheTimeout.TotalSeconds < 1)
            {
                applicationSettings.MetadataCacheTimeout = applicationSettingsCacheTimeout;
            }

            return applicationSettings;
        }

        private AutoDeploySettings MapAutoDeploy(AutoDeployData? autoDeploy) =>
            new()
            {
                Enabled = autoDeploy?.Enabled ?? false, PollingEnabled = autoDeploy?.PollingEnabled ?? false
            };

        private NexusConfig MapFromNexusData(NexusConfigData? data) =>
            new() {HmacKey = data?.HmacKey, NuGetSource = data?.NuGetSource, NuGetConfig = data?.NuGetConfig};

        private DefaultNuGetConfig MapFromNuGetData(DefaultNuGetConfigData? data) =>
            new() {NuGetSource = data?.NuGetSource, NuGetConfig = data?.NuGetConfig};

        private AutoDeployData MapToAutoDeployData(AutoDeploySettings? autoDeploySettings) =>
            new()
            {
                Enabled = autoDeploySettings?.Enabled ?? false,
                PollingEnabled = autoDeploySettings?.PollingEnabled ?? false
            };

        private ApplicationSettingsData MapToData(ApplicationSettings applicationSettings) =>
            new()
            {
                CacheTime = applicationSettings.CacheTime,
                Id = AppSettings,
                NexusConfig = MapToNexusData(applicationSettings.NexusConfig),
                AutoDeploy = MapToAutoDeployData(applicationSettings.AutoDeploy),
                ApplicationSettingsCacheTimeout = applicationSettings.ApplicationSettingsCacheTimeout,
                DefaultMetadataTimeout = applicationSettings.DefaultMetadataRequestTimeout,
                MetadataCacheTimeout = applicationSettings.MetadataCacheTimeout,
                AgentExe = applicationSettings.AgentExe,
                HostAgentEnabled = applicationSettings.HostAgentEnabled,
                DefaultNuGetConfig = MapToNuGetData(applicationSettings.DefaultNuGetConfig)
            };

        private DefaultNuGetConfigData MapToNuGetData(DefaultNuGetConfig defaultNuGetConfig) => new() {NuGetSource = defaultNuGetConfig.NuGetSource, NuGetConfig = defaultNuGetConfig.NuGetConfig};

        private NexusConfigData MapToNexusData(NexusConfig nexusConfig) =>
            new()
            {
                HmacKey = nexusConfig.HmacKey,
                NuGetSource = nexusConfig.NuGetSource,
                NuGetConfig = nexusConfig.NuGetConfig
            };
    }
}