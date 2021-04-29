using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Configuration;
using Arbor.App.Extensions.ExtensionMethods;
using Arbor.AspNetCore.Host;
using Arbor.KVConfiguration.Core;
using Arbor.Primitives;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Primitives;
using Milou.Deployer.Web.Agent;
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
        private readonly IKeyValueConfiguration _keyValueConfiguration;

        public TestDataSeeder(IMediator mediator, EnvironmentVariables environmentVariables, IKeyValueConfiguration keyValueConfiguration)
        {
            _mediator = mediator;
            _environmentVariables = environmentVariables;
            _keyValueConfiguration = keyValueConfiguration;
        }

        public async Task SeedAsync(CancellationToken cancellationToken)
        {
            if (!_environmentVariables.Variables.ContainsKey("TestDeploymentTargetPath"))
            {
                return;
            }

            var testTarget = new DeploymentTarget(
                new DeploymentTargetId("TestTarget"),
                "Test target",
                "MilouDeployerWebTest",
                allowExplicitPreRelease: false,
                autoDeployEnabled: true,
                targetDirectory: _environmentVariables.Variables["TestDeploymentTargetPath"],
                url: _environmentVariables.Variables["TestDeploymentUri"].ParseUriOrDefault(),
                emailNotificationAddresses: new StringValues("noreply@localhost.local"),
                enabled: true);

            var createTarget = new CreateTarget(testTarget.Id.TargetId, testTarget.Name);
            await _mediator.Send(createTarget, cancellationToken);

            string nugetConfigFile = _keyValueConfiguration[ConfigurationConstants.NugetConfigFile];

            var updateDeploymentTarget = new UpdateDeploymentTarget(
                testTarget.Id,
                testTarget.AllowPreRelease,
                testTarget.Url?.ToString(),
                testTarget.PackageId,
                autoDeployEnabled: testTarget.AutoDeployEnabled,
                targetDirectory: testTarget.TargetDirectory,
                nugetConfigFile: nugetConfigFile);

            await _mediator.Send(updateDeploymentTarget, cancellationToken);

            var enableTarget = new EnableTarget(testTarget.Id.TargetId);

            await _mediator.Send(enableTarget, cancellationToken);
        }

        public int Order => 100;

        public Task RunAsync(CancellationToken cancellationToken) => SeedAsync(cancellationToken);
    }
}