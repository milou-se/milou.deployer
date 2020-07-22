using System;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.AspNetCore.Host;
using Arbor.Primitives;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Primitives;
using Milou.Deployer.Web.Core.Deployment;
using Milou.Deployer.Web.Core.Deployment.Messages;
using Milou.Deployer.Web.Core.Deployment.Targets;

namespace Milou.Deployer.Web.Tests.Integration
{
    [UsedImplicitly]
    public class TestDataSeeder : IPreStartModule
    {
        private readonly IMediator _mediator;
        private readonly EnvironmentVariables _environmentVariables;

        public TestDataSeeder(IMediator mediator, EnvironmentVariables environmentVariables)
        {
            _mediator = mediator;
            _environmentVariables = environmentVariables;
        }

        public async Task SeedAsync(CancellationToken cancellationToken)
        {
            var testTarget = new DeploymentTarget(
                "TestTarget",
                "Test target",
                "MilouDeployerWebTest",
                allowExplicitPreRelease: false,
                autoDeployEnabled: true,
                targetDirectory: _environmentVariables.Variables["TestDeploymentTargetPath"],
                url: _environmentVariables.Variables["TestDeploymentUri"].ParseUriOrDefault(),
                emailNotificationAddresses: new StringValues("noreply@localhost.local"),
                enabled: true);

            var createTarget = new CreateTarget(testTarget.Id, testTarget.Name);
            await _mediator.Send(createTarget, cancellationToken);

            var updateDeploymentTarget = new UpdateDeploymentTarget(
                testTarget.Id,
                testTarget.AllowPreRelease,
                testTarget.Url?.ToString(),
                testTarget.PackageId,
                autoDeployEnabled: testTarget.AutoDeployEnabled,
                targetDirectory: testTarget.TargetDirectory);

            await _mediator.Send(updateDeploymentTarget, cancellationToken);

            var enableTarget = new EnableTarget(testTarget.Id);

            await _mediator.Send(enableTarget, cancellationToken);
        }

        public int Order => int.MaxValue;

        public Task RunAsync(CancellationToken cancellationToken) => SeedAsync(cancellationToken);
    }
}