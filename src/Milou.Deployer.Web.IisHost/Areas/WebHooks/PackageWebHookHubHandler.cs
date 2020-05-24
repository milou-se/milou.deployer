using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Sources;
using Milou.Deployer.Web.Core.NuGet;
using Milou.Deployer.Web.IisHost.Areas.Deployment.Signaling;

namespace Milou.Deployer.Web.IisHost.Areas.WebHooks
{
    [UsedImplicitly]
    public class PackageWebHookHubHandler : INotificationHandler<PackageUpdatedEvent>
    {
        private readonly IDeploymentTargetReadService _readService;
        private readonly IHubContext<TargetHub> _targetHubContext;

        public PackageWebHookHubHandler(IHubContext<TargetHub> targetHubContext,
            IDeploymentTargetReadService readService)
        {
            _targetHubContext = targetHubContext;
            _readService = readService;
        }

        public async Task Handle(PackageUpdatedEvent notification, CancellationToken cancellationToken)
        {
            var deploymentTargets = await _readService.GetDeploymentTargetsAsync(stoppingToken: cancellationToken);

            DeploymentTarget[] targetsMatchingPackage = deploymentTargets
                .Where(
                    target => target.PackageId.Equals(
                        notification.PackageVersion.PackageId,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (targetsMatchingPackage.Length == 0)
            {
                return;
            }

            IClientProxy clientProxy = _targetHubContext.Clients.All;

            await clientProxy.SendAsync(TargetHub.TargetsWithUpdates, notification.PackageVersion.PackageId,
                notification.PackageVersion.Version.ToNormalizedString(),
                targetsMatchingPackage.Select(target => target.Id).ToArray(), cancellationToken);
        }
    }
}