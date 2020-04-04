﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions;
using JetBrains.Annotations;

using MediatR;

using Milou.Deployer.Web.Core.Deployment.Sources;
using Milou.Deployer.Web.Core.Deployment.WorkTasks;
using Milou.Deployer.Web.Core.NuGet;
using Milou.Deployer.Web.Core.Settings;
using Milou.Deployer.Web.IisHost.Areas.Deployment.Services;

using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.WebHooks
{
    [UsedImplicitly]
    public class PackageWebHookAutoDeployHandler : INotificationHandler<PackageEventNotification>
    {
        private readonly DeploymentWorkerService _deploymentService;

        private readonly ILogger _logger;

        private readonly MonitoringService _monitoringService;

        private readonly IApplicationSettingsStore _applicationSettingsStore;

        private readonly IDeploymentTargetReadService _targetSource;

        public PackageWebHookAutoDeployHandler(
            IDeploymentTargetReadService targetSource,
            DeploymentWorkerService deploymentService,
            ILogger logger,
            MonitoringService monitoringService,
            IApplicationSettingsStore applicationSettingsStore)
        {
            _targetSource = targetSource;
            _deploymentService = deploymentService;
            _logger = logger;
            _monitoringService = monitoringService;
            _applicationSettingsStore = applicationSettingsStore;
        }

        public async Task Handle(PackageEventNotification notification, CancellationToken cancellationToken)
        {
            if (!(await _applicationSettingsStore.GetApplicationSettings(cancellationToken)).AutoDeploy.Enabled)
            {
                _logger.Information("Auto deploy is disabled, skipping package web hook notification");
                return;
            }

            Core.Deployment.Packages.PackageVersion packageIdentifier = notification.PackageVersion;

            if (packageIdentifier is null)
            {
                throw new ArgumentNullException(nameof(packageIdentifier));
            }

            _logger.Information("Received hook for package {Package}", packageIdentifier);

            System.Collections.Generic.IReadOnlyCollection<Core.Deployment.DeploymentTarget> deploymentTargets =
                (await _targetSource.GetDeploymentTargetsAsync(stoppingToken: cancellationToken))
                .SafeToReadOnlyCollection();

            Core.Deployment.DeploymentTarget[] withAutoDeploy = deploymentTargets.Where(target => target.AutoDeployEnabled).ToArray();

            if (!withAutoDeploy.Any())
            {
                _logger.Information("No target has auto deploy enabled");
            }
            else
            {
                foreach (Core.Deployment.DeploymentTarget deploymentTarget in withAutoDeploy)
                {
                    if (deploymentTarget.PackageId.Equals(
                        packageIdentifier.PackageId,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        if (deploymentTarget.NuGet.NuGetConfigFile is {}
                            && !deploymentTarget.NuGet.NuGetConfigFile.Equals(notification.NugetConfig))
                        {
                            _logger.Information("Target {Target} does not match NuGet config", deploymentTarget.Id);
                            continue;
                        }

                        if (deploymentTarget.NuGet.NuGetPackageSource is {}
                            && !deploymentTarget.NuGet.NuGetPackageSource.Equals(notification.NugetSource))
                        {
                            _logger.Information("Target {Target} does not match NuGet source", deploymentTarget.Id);
                            continue;
                        }

                        bool allowDeployment =
                            !packageIdentifier.Version.IsPrerelease || deploymentTarget.AllowPreRelease;

                        if (allowDeployment)
                        {
                            Core.Application.Metadata.AppVersion metadata = await _monitoringService.GetAppMetadataAsync(
                                               deploymentTarget,
                                               cancellationToken);

                            if (metadata.SemanticVersion is {})
                            {
                                if (packageIdentifier.Version > metadata.SemanticVersion)
                                {
                                    _logger.Information(
                                        "Auto deploying package {PackageIdentifier} to target {Name} from web hook",
                                        packageIdentifier,
                                        deploymentTarget.Name);

                                            _deploymentService.Enqueue(
                                                new DeploymentTask(
                                                    packageIdentifier,
                                                    deploymentTarget.Id,
                                                    Guid.NewGuid(),
                                                    "Web hook auto deploy"));

                                    _logger.Information(
                                        "Successfully enqueued package {PackageIdentifier} to target {Name} from web hook",
                                        packageIdentifier,
                                        deploymentTarget.Name);
                                }
                                else
                                {
                                    _logger.Information(
                                        "Auto deployment skipped for {PackageIdentifier} since deployed version is higher {MetadataVersion}",
                                        packageIdentifier,
                                        metadata.SemanticVersion.ToNormalizedString());
                                }
                            }
                            else
                            {
                                _logger.Information(
                                    "Auto deployment skipped for {PackageIdentifier} since the target version could not be determined",
                                    packageIdentifier);
                            }
                        }
                        else
                        {
                            _logger.Information(
                                "Auto deployment skipped for {PackageIdentifier} since the target does not allow pre-release",
                                packageIdentifier);
                        }
                    }
                    else
                    {
                        _logger.Information(
                            "No package id matched {PackageIdentifier} for target {Name}",
                            packageIdentifier,
                            deploymentTarget.Name);
                    }
                }
            }
        }
    }
}