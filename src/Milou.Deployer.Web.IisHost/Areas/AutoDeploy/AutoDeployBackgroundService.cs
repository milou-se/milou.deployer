using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.App.Extensions.Time;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Core.Application.Metadata;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Packages;
using Milou.Deployer.Web.Core.Deployment.Sources;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;
using Milou.Deployer.Web.Core.Settings;
using Milou.Deployer.Web.IisHost.Areas.Deployment.Services;
using Milou.Deployer.Web.IisHost.Areas.NuGet;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.AutoDeploy
{
    [UsedImplicitly]
    public class AutoDeployBackgroundService : BackgroundService
    {
        private readonly IApplicationSettingsStore _applicationSettingsStore;
        private readonly AutoDeployConfiguration _autoDeployConfiguration;
        private readonly IDeploymentTargetReadService _deploymentTargetReadService;
        private readonly DeploymentWorkerService _deploymentWorkerService;
        private readonly ILogger _logger;
        private readonly MonitoringService _monitoringService;
        private readonly IPackageService _packageService;
        private readonly TimeoutHelper _timeoutHelper;

        public AutoDeployBackgroundService(
            [NotNull] IDeploymentTargetReadService deploymentTargetReadService,
            [NotNull] MonitoringService monitoringService,
            [NotNull] DeploymentWorkerService deploymentWorkerService,
            [NotNull] AutoDeployConfiguration autoDeployConfiguration,
            [NotNull] ILogger logger,
            [NotNull] IPackageService packageService,
            TimeoutHelper timeoutHelper,
            IApplicationSettingsStore applicationSettingsStore)
        {
            _deploymentTargetReadService = deploymentTargetReadService ??
                                           throw new ArgumentNullException(nameof(deploymentTargetReadService));
            _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
            _deploymentWorkerService = deploymentWorkerService ??
                                       throw new ArgumentNullException(nameof(deploymentWorkerService));
            _autoDeployConfiguration = autoDeployConfiguration ??
                                       throw new ArgumentNullException(nameof(autoDeployConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _packageService = packageService ?? throw new ArgumentNullException(nameof(packageService));
            _timeoutHelper = timeoutHelper;
            _applicationSettingsStore = applicationSettingsStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            ApplicationSettings applicationSettings =
                await _applicationSettingsStore.GetApplicationSettings(stoppingToken);

            if (!applicationSettings.AutoDeploy.Enabled)
            {
                _logger.Debug("Auto deploy is disabled");
                return;
            }

            if (!applicationSettings.AutoDeploy.PollingEnabled)
            {
                _logger.Debug("Auto deploy polling is disabled");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(_autoDeployConfiguration.StartupDelayInSeconds), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var deploymentTargets = await GetDeploymentTargetsWithAutoDeployEnabled(stoppingToken);

                if (deploymentTargets.IsDefaultOrEmpty)
                {
                    _logger.Verbose(
                        "Found no deployment targets with auto deployment enabled, waiting {DelayInSeconds} seconds",
                        _autoDeployConfiguration.EmptyTargetsDelayInSeconds);

                    await Task.Delay(
                        TimeSpan.FromSeconds(_autoDeployConfiguration.EmptyTargetsDelayInSeconds),
                        stoppingToken);

                    continue;
                }

                var targetsWithUrl = deploymentTargets.Where(target => target.Url is {}).ToImmutableArray();

                if (targetsWithUrl.IsDefaultOrEmpty)
                {
                    _logger.Verbose(
                        "Found no deployment targets with auto deployment enabled and URL defined, waiting {DelayInSeconds} seconds",
                        _autoDeployConfiguration.EmptyTargetsDelayInSeconds);

                    await Task.Delay(
                        TimeSpan.FromSeconds(_autoDeployConfiguration.EmptyTargetsDelayInSeconds),
                        stoppingToken);

                    continue;
                }

                AppVersion[] appVersions = await GetAppVersions(stoppingToken, targetsWithUrl);

                foreach (DeploymentTarget deploymentTarget in targetsWithUrl)
                {
                    AppVersion? appVersion = appVersions.SingleOrDefault(version =>
                        version.Target.Id == deploymentTarget.Id);

                    if (appVersion?.SemanticVersion is null)
                    {
                        _logger.Verbose("No semantic version was found for target {Target}, {Url}", deploymentTarget.Id, deploymentTarget.Url);
                    }

                    if (string.IsNullOrWhiteSpace(appVersion?.PackageId))
                    {
                        _logger.Verbose("No package id was found for target {Target}, {Url}", deploymentTarget.Id, deploymentTarget.Url);
                    }

                    if (appVersion?.SemanticVersion is null || string.IsNullOrWhiteSpace(appVersion.PackageId))
                    {
                        continue;
                    }

                    ImmutableHashSet<PackageVersion> packageVersions =
                        await GetPackageVersions(stoppingToken, deploymentTarget);

                    if (packageVersions.IsEmpty)
                    {
                        continue;
                    }

                    ImmutableHashSet<PackageVersion> filteredPackages = !deploymentTarget.AllowPreRelease
                        ? packageVersions.Where(packageVersion => !packageVersion.Version.IsPrerelease)
                            .ToImmutableHashSet()
                        : packageVersions;

                    if (filteredPackages.IsEmpty)
                    {
                        _logger.Debug(
                            "Found no auto deploy versions of package {Package} for target {TargetId} allowing pre-release {AllowPreRelease}",
                            deploymentTarget.PackageId,
                            deploymentTarget.Id, deploymentTarget.AllowPreRelease);
                        continue;
                    }

                    var newerPackages = filteredPackages
                        .Where(package =>
                            package.PackageId.Equals(appVersion.PackageId, StringComparison.OrdinalIgnoreCase)
                            && package.Version > appVersion.SemanticVersion)
                        .ToImmutableHashSet();

                    PackageVersion? packageToDeploy = newerPackages
                        .OrderByDescending(package => package.Version)
                        .FirstOrDefault();

                    if (packageToDeploy is {})
                    {
                        var task = new DeploymentTask(packageToDeploy, deploymentTarget.Id, Guid.NewGuid(),
                            nameof(AutoDeployBackgroundService));

                        _logger.Information(
                            "Enqueuing auto deploy package {Package} to target {TargetId}",
                            packageToDeploy,
                            deploymentTarget.Id);

                        await _deploymentWorkerService.Enqueue(task);
                    }
                    else
                    {
                        _logger.Debug(
                            "Found no newer auto deploy versions for target {TargetId} allowing pre-release {AllowPreRelease}",
                            deploymentTarget.Id, deploymentTarget.AllowPreRelease);
                    }
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(_autoDeployConfiguration.AfterDeployDelayInSeconds),
                    stoppingToken);
            }
        }

        private async Task<ImmutableHashSet<PackageVersion>> GetPackageVersions(CancellationToken stoppingToken,
            DeploymentTarget deploymentTarget)
        {
            try
            {
                using CancellationTokenSource packageVersionCancellationTokenSource =
                    _timeoutHelper.CreateCancellationTokenSource(
                        TimeSpan.FromSeconds(_autoDeployConfiguration.DefaultTimeoutInSeconds));

                using var linked =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        stoppingToken,
                        packageVersionCancellationTokenSource.Token);

                var packageVersions = (await _packageService.GetPackageVersionsAsync(
                        deploymentTarget.PackageId,
                        nugetConfigFile: deploymentTarget.NuGet.NuGetConfigFile,
                        nugetPackageSource: deploymentTarget.NuGet.NuGetPackageSource,
                        cancellationToken: linked.Token))
                    .ToImmutableHashSet();

                return packageVersions;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Could not get package versions for auto deploy of target {TargetId}",
                    deploymentTarget.Id);
                return ImmutableHashSet<PackageVersion>.Empty;
            }
        }

        private async Task<AppVersion[]> GetAppVersions(CancellationToken stoppingToken,
            ImmutableArray<DeploymentTarget> targetsWithUrl)
        {
            try
            {
                using CancellationTokenSource cancellationTokenSource =
                    _timeoutHelper.CreateCancellationTokenSource(
                        TimeSpan.FromSeconds(_autoDeployConfiguration.MetadataTimeoutInSeconds));
                using var linkedCancellationTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, stoppingToken);
                var cancellationToken = linkedCancellationTokenSource.Token;

                IEnumerable<Task<AppVersion?>> tasks = targetsWithUrl.Select(
                    target =>
                        _monitoringService.GetAppMetadataAsync(target, cancellationToken));

                AppVersion[] appVersions = (await Task.WhenAll(tasks)).NotNull().ToArray();

                return appVersions;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Could not app versions for auto deploy");

                return Array.Empty<AppVersion>();
            }
        }

        private async Task<ImmutableArray<DeploymentTarget>> GetDeploymentTargetsWithAutoDeployEnabled(
            CancellationToken stoppingToken)
        {
            try
            {
                ImmutableArray<DeploymentTarget> deploymentTargets;
                using (CancellationTokenSource targetsTokenSource =
                    _timeoutHelper.CreateCancellationTokenSource(
                        TimeSpan.FromSeconds(_autoDeployConfiguration.DefaultTimeoutInSeconds)))
                {
                    using var linked =
                        CancellationTokenSource.CreateLinkedTokenSource(
                            stoppingToken,
                            targetsTokenSource.Token);
                    deploymentTargets =
                        (await _deploymentTargetReadService.GetDeploymentTargetsAsync(stoppingToken: linked.Token))
                        .Where(target => target.Enabled && target.AutoDeployEnabled)
                        .ToImmutableArray();
                }

                return deploymentTargets;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Could not get targets with auto deploy enabled");

                return ImmutableArray<DeploymentTarget>.Empty;
            }
        }
    }
}