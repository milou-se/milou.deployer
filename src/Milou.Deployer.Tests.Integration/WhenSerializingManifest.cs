using System.Collections.Generic;
using System.Collections.Immutable;
using Milou.Deployer.Core.Configuration;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.Core.Deployment.Configuration;

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
                [WebDeployRules.AppDataSkipDirectiveEnabled] = new[] { "false" },
                [WebDeployRules.AppOfflineEnabled] = new[] { "true" },
                [WebDeployRules.ApplicationInsightsProfiler2SkipDirectiveEnabled] = new[] { "true" },
                [WebDeployRules.DoNotDeleteEnabled] = new[] { "false" },
                [WebDeployRules.UseChecksumEnabled] = new[] { "true" },
                [WebDeployRules.WhatIfEnabled] = new[] { "false" }
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
