using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions;
using Arbor.App.Extensions.Time;
using Microsoft.AspNetCore.Mvc;
using Milou.Deployer.Web.Core;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Sources;
using Milou.Deployer.Web.IisHost.Areas.Deployment.Services;
using Milou.Deployer.Web.IisHost.Areas.Deployment.ViewOutputModels;
using Milou.Deployer.Web.IisHost.Areas.Targets.Controllers;
using Milou.Deployer.Web.IisHost.Controllers;

namespace Milou.Deployer.Web.IisHost.Areas.Deployment.Controllers
{
    [Area(DeploymentConstants.AreaName)]
    [Route(BaseRoute)]
    public class MonitoringController : BaseApiController
    {
        public const string BaseRoute = "monitoring";

        private readonly MonitoringService _monitoringService;

        private readonly IDeploymentTargetReadService _targetSource;

        public MonitoringController(
            MonitoringService monitoringService,
            IDeploymentTargetReadService targetSource)
        {
            _monitoringService = monitoringService;
            _targetSource = targetSource;
        }

        [HttpGet]
        [Route("~/status")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            System.Collections.Generic.IReadOnlyCollection<DeploymentTarget> targets = (await _targetSource.GetOrganizationsAsync(cancellationToken))
                .SelectMany(
                    organization => organization.Projects.SelectMany(project => project.DeploymentTargets))
                .SafeToReadOnlyCollection();

            System.Collections.Generic.IReadOnlyCollection<Core.Application.Metadata.AppVersion> appVersions =
                await _monitoringService.GetAppMetadataAsync(targets, cancellationToken);

            return View(new MonitoringViewOutputModel(appVersions));
        }

        [HttpGet]
        [Route(MonitorConstants.MonitorRoute, Name = MonitorConstants.MonitorRouteName)]
        [Route("")]
        public IActionResult Status() => View();

        [HttpGet]
        [Route("~/api/targets")]
        public async Task<IActionResult> Targets(CancellationToken cancellationToken,
            [FromServices] IEnvironmentTypeService environmentTypeService)
        {
            System.Collections.Immutable.ImmutableArray<EnvironmentType> environmentTypes = await environmentTypeService.GetEnvironmentTypes(cancellationToken);

            var targets = (await _targetSource.GetOrganizationsAsync(cancellationToken))
                .SelectMany(
                    organization => organization.Projects.SelectMany(project => project.DeploymentTargets))
                .Select(deploymentTarget =>
                {
                    string editUrl = Url.RouteUrl(TargetConstants.EditTargetRouteName,
                        new { deploymentTargetId = deploymentTarget.Id });
                    string historyUrl = Url.RouteUrl(DeploymentConstants.HistoryRouteName,
                        new { deploymentTargetId = deploymentTarget.Id });
                    string statusUrl = Url.RouteUrl(TargetConstants.TargetStatusApiRouteName,
                        new { deploymentTargetId = deploymentTarget.Id });

                    EnvironmentType environmentType =
                        environmentTypes.SingleOrDefault(type => type.Id.Equals(deploymentTarget.EnvironmentTypeId)) ?? EnvironmentType.Unknown;

                    return new
                    {
                        targetId = deploymentTarget.Id,
                        name = deploymentTarget.Name,
                        url = deploymentTarget.Url,
                        editUrl,
                        historyUrl,
                        statusKey = DeployStatus.Unknown.Key,
                        statusDisplayName = DeployStatus.Unknown.DisplayName,
                        statusUrl,
                        isPreReleaseVersion = false,
                        semanticVersion = "",
                        preReleaseClass = "",
                        intervalAgo = "",
                        intervalAgoName = "",
                        deployedAtLocalTime = "",
                        environmentType = environmentType.Name,
                        metadataUrl = deploymentTarget.Url is null ? null : $"{deploymentTarget.Url.AbsoluteUri.TrimEnd('/')}/applicationmetadata.json",
                        statusMessage = "",
                        latestNewerAvailabe = "",
                        deployEnabled = deploymentTarget.Enabled && !deploymentTarget.IsReadOnly,
                        packages = Array.Empty<object>(),
                        packageId = deploymentTarget.PackageId
                    };
                })
                .OrderBy(target => target.name);

            return Json(new { targets });
        }

        [HttpGet]
        [Route(TargetConstants.TargetStatusApiRoute, Name = TargetConstants.TargetStatusApiRouteName)]
        public async Task<IActionResult> Status(
            string deploymentTargetId,
            [FromServices] IDeploymentTargetReadService deploymentTargetReadService,
            [FromServices] MonitoringService monitoringService,
            [FromServices] ICustomClock clock)
        {
            DeploymentTarget deploymentTarget = await deploymentTargetReadService.GetDeploymentTargetAsync(deploymentTargetId);

            if (deploymentTarget is null)
            {
                return new NotFoundResult();
            }

            if (deploymentTarget.Url is null)
            {
                return Json(DeployStatus.Unavailable);
            }

            if (!deploymentTarget.Enabled)
            {
                return Json(DeployStatus.Unavailable);
            }

            Core.Application.Metadata.AppVersion appVersion = await monitoringService.GetAppMetadataAsync(deploymentTarget, default);

            DeploymentInterval deploymentInterval = appVersion.DeployedAtUtc.IntervalAgo(clock);

            int selectedPackageIndex = appVersion.AvailablePackageVersions
                        .Select((item, index) => new
                        {
                            Selected = item.PackageId == deploymentTarget.PackageId &&
                                       item.Version == appVersion.SemanticVersion,
                            Index = index
                        }).SingleOrDefault(t => t.Selected)?.Index ?? -1;
            return Json(new
            {
                displayName = appVersion.Status.DisplayName,
                key = appVersion.Status.Key,
                semanticVersion =
                    appVersion.SemanticVersion?.ToNormalizedString().WithDefault(Constants.NotAvailable),
                isPreReleaseVersion = appVersion.SemanticVersion?.IsPrerelease ?? false,
                preReleaseClass = appVersion.PreReleaseClass,
                intervalAgo = appVersion.DeployedAtUtc.RelativeUtcToLocalTime(clock),
                intervalAgoName = deploymentInterval.Name,
                deployedAtLocalTime = appVersion.DeployedAtUtc.ToLocalTimeFormatted(clock),
                statusMessage = appVersion.Message,
                latestNewerAvailable = appVersion.LatestNewerAvailable?.ToNormalizedString() ?? "",
                deployEnabled =
                    deploymentTarget.Enabled && !deploymentTarget.IsReadOnly,
                packageId = deploymentTarget.PackageId,
                packages = appVersion.AvailablePackageVersions.Select(availableVersion => new
                {
                    packageId = availableVersion.PackageId,
                    version = availableVersion.Version.ToNormalizedString(),
                    combinedName = availableVersion.Key,
                    isNewer = availableVersion.Version > appVersion.SemanticVersion,
                    isCurrent = availableVersion.Version == appVersion.SemanticVersion,
                    preReleaseWarning = availableVersion.Version.IsPrerelease && appVersion.SemanticVersion?.IsPrerelease == false
                }).ToArray(),
                selectedPackageIndex
            });
        }
    }
}
