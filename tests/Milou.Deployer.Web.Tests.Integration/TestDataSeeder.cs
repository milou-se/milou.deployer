using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Configuration;
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
    [RegistrationOrder(100)]
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
            if (!_environmentVariables.Variables.ContainsKey("TestDeploymentTargetPath"))
            {
                return;
            }

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

            string nugetConfigFile = Path.Combine(VcsTestPathHelper.GetRootDirectory(), "tests", "Milou.Deployer.Web.Tests.Integration", "TestData", "nuget.config");

            var updateDeploymentTarget = new UpdateDeploymentTarget(
                testTarget.Id,
                testTarget.AllowPreRelease,
                testTarget.Url?.ToString(),
                testTarget.PackageId,
                autoDeployEnabled: testTarget.AutoDeployEnabled,
                targetDirectory: testTarget.TargetDirectory,
                nugetConfigFile: nugetConfigFile);

            await _mediator.Send(updateDeploymentTarget, cancellationToken);

            var enableTarget = new EnableTarget(testTarget.Id);

            await _mediator.Send(enableTarget, cancellationToken);
        }

        public int Order => 100;

        public Task RunAsync(CancellationToken cancellationToken) => SeedAsync(cancellationToken);
    }
}