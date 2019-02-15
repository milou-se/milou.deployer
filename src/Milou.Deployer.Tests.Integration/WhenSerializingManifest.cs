using System.Collections.Generic;
using System.Collections.Immutable;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Newtonsoft.Json;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration
{
    public class WhenSerializingManifest
    {
        public WhenSerializingManifest(ITestOutputHelper output)
        {
            _output = output;
        }

        private readonly ITestOutputHelper _output;

        [Fact]
        public void ItShouldFind1DeploymentExecutionDefinition()
        {
            var parameters = new Dictionary<string, string[]>
            {
                [ConfigurationKeys.WebDeploy.Rules.AppDataSkipDirectiveEnabled] = new[] { "false" },
                [ConfigurationKeys.WebDeploy.Rules.AppOfflineEnabled] = new[] { "true" },
                [ConfigurationKeys.WebDeploy.Rules.ApplicationInsightsProfiler2SkipDirectiveEnabled] = new[] { "true" },
                [ConfigurationKeys.WebDeploy.Rules.DoNotDeleteEnabled] = new[] { "false" },
                [ConfigurationKeys.WebDeploy.Rules.UseChecksumEnabled] = new[] { "true" },
                [ConfigurationKeys.WebDeploy.Rules.WhatIfEnabled] = new[] { "false" }
            };

            DeploymentExecutionDefinition[] deploymentExecutionDefinitions =
            {
                new DeploymentExecutionDefinition("MySamplePackageId",
                    @"C:\Sites\Sample",
                    SemanticVersion.Parse("1.0.0"),
                    excludedFilePatterns: "*.user;*.cache",
                    parameters: parameters)
            };

            string serialized = JsonConvert.SerializeObject(
                new { definitions = deploymentExecutionDefinitions },
                Formatting.Indented);

            _output.WriteLine(serialized);

            ImmutableArray<DeploymentExecutionDefinition> deserializeObject =
                new DeploymentExecutionDefinitionParser().Deserialize(serialized);

            Assert.Single(deserializeObject);
            Assert.Equal(2, deserializeObject[0].ExcludedFilePatterns.Length);
        }
    }
}
