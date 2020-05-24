using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.Configuration;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.AspNetCore.Host.Hosting;
using Arbor.KVConfiguration.Core;
using Arbor.KVConfiguration.Schema.Json;
using Arbor.KVConfiguration.Urns;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Configuration;
using Milou.Deployer.Web.Core.Deployment.Sources;
using Milou.Deployer.Web.Core.Settings;
using Milou.Deployer.Web.IisHost.Areas.Deployment.Services;
using Serilog;
using Serilog.Core;

namespace Milou.Deployer.Web.IisHost.Areas.Settings.Controllers
{
    [UsedImplicitly]
    public class DiagnosticsViewHandler : IRequestHandler<SettingsViewRequest, SettingsViewModel>
    {
        private readonly IConfiguration _aspNetConfiguration;
        private readonly MultiSourceKeyValueConfiguration _configuration;
        private readonly ConfigurationInstanceHolder _configurationInstanceHolder;
        private readonly IDeploymentTargetReadService _deploymentTargetReadService;

        [NotNull]
        private readonly EnvironmentConfiguration _environmentConfiguration;

        private readonly ILogger _logger;

        private readonly LoggingLevelSwitch _loggingLevelSwitch;
        private readonly ServiceDiagnostics _serviceDiagnostics;

        private readonly IServiceProvider _serviceProvider;

        private readonly IApplicationSettingsStore _settingsStore;

        public DiagnosticsViewHandler(
            [NotNull] IDeploymentTargetReadService deploymentTargetReadService,
            [NotNull] MultiSourceKeyValueConfiguration configuration,
            [NotNull] IConfiguration aspNetConfiguration,
            [NotNull] LoggingLevelSwitch loggingLevelSwitch,
            [NotNull] EnvironmentConfiguration environmentConfiguration,
            IServiceProvider serviceProvider,
            ServiceDiagnostics serviceDiagnostics,
            ConfigurationInstanceHolder configurationInstanceHolder,
            ILogger logger,
            IApplicationSettingsStore settingsStore)
        {
            _deploymentTargetReadService = deploymentTargetReadService ??
                                           throw new ArgumentNullException(nameof(deploymentTargetReadService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _aspNetConfiguration = aspNetConfiguration ?? throw new ArgumentNullException(nameof(aspNetConfiguration));
            _loggingLevelSwitch = loggingLevelSwitch ?? throw new ArgumentNullException(nameof(loggingLevelSwitch));
            _environmentConfiguration = environmentConfiguration;
            _serviceProvider = serviceProvider;
            _serviceDiagnostics = serviceDiagnostics;
            _configurationInstanceHolder = configurationInstanceHolder;
            _logger = logger;
            _settingsStore = settingsStore;
        }

        public async Task<SettingsViewModel> Handle(SettingsViewRequest request, CancellationToken cancellationToken)
        {
            var routesWithController =
                RouteList.GetRoutesWithController(ApplicationAssemblies.FilteredAssemblies());

            var configurationValues = new ConfigurationInfo(_configuration.SourceChain,
                _configuration.AllKeys
                    .OrderBy(key => key)
                    .Select(key =>
                        new ConfigurationKeyInfo(key,
                            _configuration[key].MakeAnonymous(key,
                                ArborStringExtensions.DefaultAnonymousKeyWords.ToArray()),
                            _configuration.ConfiguratorFor(key).GetType().Name))
                    .ToImmutableArray());

            IEnumerable<KeyValuePair<string, string>> aspNetConfigurationValues = _aspNetConfiguration
                .AsEnumerable()
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair =>
                    new KeyValuePair<string, string>(pair.Key,
                        pair.Value.MakeAnonymous(pair.Key,
                            ArborStringExtensions.DefaultAnonymousKeyWords.ToArray())));

            ApplicationVersionInfo applicationVersionInfo = ApplicationVersionHelper.GetAppVersion();

            var serviceDiagnosticsRegistrations = _serviceDiagnostics.Registrations;

            IKeyValueConfiguration applicationMetadata = await GetApplicationMetadataAsync(cancellationToken);

            ServiceInstance? GetInstance(ServiceRegistrationInfo serviceRegistrationInfo)
            {
                Type registrationType = serviceRegistrationInfo.ServiceDescriptorServiceType;

                if (serviceRegistrationInfo.ServiceDescriptorImplementationInstance is {})
                {
                    return new ServiceInstance(registrationType,
                        serviceRegistrationInfo.ServiceDescriptorImplementationInstance,
                        serviceRegistrationInfo.Module);
                }

                if (serviceRegistrationInfo.Factory is {})
                {
                    try
                    {
                        return new ServiceInstance(
                            registrationType,
                            serviceRegistrationInfo.Factory(_serviceProvider),
                            serviceRegistrationInfo.Module);
                    }
                    catch (Exception ex)
                    {
                        return new ServiceInstance(registrationType, ex, serviceRegistrationInfo.Module);
                    }
                }

                if (serviceRegistrationInfo.ServiceDescriptorImplementationType.IsGenericType)
                {
                    return new ServiceInstance(registrationType, "Generic type", serviceRegistrationInfo.Module);
                }

                if (serviceRegistrationInfo.ServiceDescriptorImplementationType?.Namespace?.StartsWith(
                    "Microsoft.AspNetCore.Mvc.ViewFeatures.RazorComponents") == true)
                {
                    return new ServiceInstance(registrationType, "Razor", serviceRegistrationInfo.Module);
                }

                try
                {
                    object instance =
                        _serviceProvider.GetService(serviceRegistrationInfo.ServiceDescriptorImplementationType);

                    return new ServiceInstance(registrationType, instance, serviceRegistrationInfo.Module);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    _logger.Error(ex,
                        "Could not get instance form registration type {Type}",
                        serviceRegistrationInfo.ServiceDescriptorImplementationType?.FullName);
                    return default;
                }
            }

            ImmutableArray<DeploymentTargetWorker> deploymentTargetWorkers = _configurationInstanceHolder
                .GetInstances<DeploymentTargetWorker>().Values
                .Where(item => item is {})
                .SafeToImmutableArray()!;

            ApplicationSettings applicationSettings = await _settingsStore.GetApplicationSettings(cancellationToken);

            var settingsViewModel = new SettingsViewModel(
                _deploymentTargetReadService.GetType().Name,
                routesWithController,
                configurationValues,
                serviceDiagnosticsRegistrations,
                aspNetConfigurationValues,
                serviceDiagnosticsRegistrations
                    .Select(GetInstance)
                    .Where(item => item is {})
                    .ToImmutableArray(),
                _loggingLevelSwitch.MinimumLevel,
                applicationVersionInfo,
                applicationMetadata,
                deploymentTargetWorkers,
                applicationSettings);

            return settingsViewModel;
        }

        private async Task<IKeyValueConfiguration> GetApplicationMetadataAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_environmentConfiguration.ContentBasePath))
            {
                return NoConfiguration.Empty;
            }

            string applicationMetadataJsonFilePath = Path.Combine(_environmentConfiguration.ContentBasePath,
                "wwwroot",
                "applicationmetadata.json");

            if (!File.Exists(applicationMetadataJsonFilePath))
            {
                return NoConfiguration.Empty;
            }

            string json =
                await File.ReadAllTextAsync(applicationMetadataJsonFilePath, Encoding.UTF8, cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
            {
                return NoConfiguration.Empty;
            }

            ConfigurationItems configurationItems = JsonConfigurationSerializer.Deserialize(json);

            if (configurationItems is null)
            {
                return NoConfiguration.Empty;
            }

            if (configurationItems.Keys.IsDefaultOrEmpty)
            {
                return NoConfiguration.Empty;
            }

            var values = new NameValueCollection();

            foreach (KeyValue configurationItem in configurationItems.Keys)
            {
                values.Add(configurationItem.Key, configurationItem.Value);
            }

            return new InMemoryKeyValueConfiguration(values);
        }
    }
}