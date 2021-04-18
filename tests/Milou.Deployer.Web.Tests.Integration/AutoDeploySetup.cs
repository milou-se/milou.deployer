using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Configuration;
using Arbor.KVConfiguration.Schema.Json;
using JetBrains.Annotations;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Web.Core;
using Milou.Deployer.Web.Core.Configuration;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    [UsedImplicitly]
    public class AutoDeploySetup : WebFixtureBase, IAppHost
    {
        public AutoDeploySetup(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
            // TODO run entire test in temp dir
        }

        public override async Task DisposeAsync()
        {
            if (TestConfiguration?.BaseDirectory is {})
            {
                DirectoriesToClean.Add(TestConfiguration.BaseDirectory);
            }

            await base.DisposeAsync();
        }

        protected override Task RunAsync() => Task.CompletedTask;

        protected override async Task BeforeInitialize(CancellationToken cancellationToken)
        {
            Variables.Add("TestDeploymentTargetPath", TestConfiguration.SiteAppRoot.FullName);
            Variables.Add("TestDeploymentUri",
                $"http://localhost:{ServerEnvironmentTestSiteConfiguration.Port.Port + 1}");

            string deployerDir = Path.Combine(VcsTestPathHelper.GetRootDirectory(), "tools", "milou.deployer");

            const string milouDeployerWebTestsIntegration = "Milou.Deployer.Web.Tests.Integration";

            var keys = new List<KeyValue>
            {
                new(ConfigurationConstants.NugetConfigFile,
                    TestConfiguration.NugetConfigFile.FullName,
                    null),
                new(ConfigurationKeys.NuGetConfig, TestConfiguration.NugetConfigFile.FullName, null),
                new(ConfigurationKeys.LogLevel, "Verbose", null)
            }.ToImmutableArray();

            string serializedConfigurationItems =
                JsonConfigurationSerializer.Serialize(new ConfigurationItems("1.0", keys));

            string settingsFile = Path.Combine(deployerDir, $"{Environment.MachineName}.settings.json");

            FilesToClean.Add(new FileInfo(settingsFile));

            await File.WriteAllTextAsync(settingsFile, serializedConfigurationItems, Encoding.UTF8, cancellationToken);

            var integrationTestProjectDirectory = new DirectoryInfo(Path.Combine(VcsTestPathHelper.GetRootDirectory(),
                "tests",
                milouDeployerWebTestsIntegration, "TestData", "Packages"));

            FileInfo[] nugetPackages = integrationTestProjectDirectory.GetFiles("*.nupkg");

            if (nugetPackages.Length == 0)
            {
                throw new DeployerAppException(
                    $"Could not find nuget test packages located in {integrationTestProjectDirectory.FullName}");
            }

            Variables.Add(ConfigurationKeys.KeyValueConfigurationFile, settingsFile);

            Variables.Add(ConfigurationConstants.NugetConfigFile,
                TestConfiguration.NugetConfigFile.FullName);

            Variables.Add(ConfigurationConstants.NuGetPackageSourceName,
                milouDeployerWebTestsIntegration);

            Variables.Add(
                $"{DeployerAppConstants.AutoDeployConfiguration}:default:StartupDelayInSeconds",
                "0");

            Variables.Add(
                $"{DeployerAppConstants.AutoDeployConfiguration}:default:afterDeployDelayInSeconds",
                "1");

            Variables.Add(
                $"{DeployerAppConstants.AutoDeployConfiguration}:default:MetadataTimeoutInSeconds",
                "10");

            Variables.Add(
                $"{DeployerAppConstants.AutoDeployConfiguration}:default:enabled",
                "true");

            DirectoriesToClean.Add(TestConfiguration.BaseDirectory);
        }

        protected override void OnException(Exception exception)
        {
        }

        protected override Task BeforeStartAsync(IReadOnlyCollection<string> args) => Task.CompletedTask;
    }
}