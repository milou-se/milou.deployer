using System.Collections.Generic;
using System.Collections.Immutable;
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
        public WhenSerializingManifest(ITestOutputHelper output) => _output = output;

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
                DeploymentExecutionDefinitionParser.Deserialize(serialized);

            Assert.Single(deserializeObject);
            Assert.Equal(2, deserializeObject[0].ExcludedFilePatterns.Length);
        }

        [Fact]
        public void DefinitionCreatedWithPublicCtorShouldBeEqualToDeserializedDefinition()
        {
           var definition = new DeploymentExecutionDefinition("aPackageId",
                @"C:\Temp", new SemanticVersion(1, 2, 3),
                nuGetConfigFile: "@C:\\Nuget.Config", "aNuGetSource",
                "aSiteName",
                isPreRelease: true,
                force: false,
                environmentConfig: "production",
                publishSettingsFile: null,
                parameters: null,
                excludedFilePatterns: null,
                requireEnvironmentConfig: false,
                webConfigTransformFile: "C:\\Xdt.Config",
                publishType: PublishType.WebDeploy.Name,
                ftpPath: null,
                nugetExePath: "C:\\NuGet.exe",
                packageListPrefix: "packageid:",
                packageListPrefixEnabled: true);


           DeploymentExecutionDefinition[] deploymentExecutionDefinitions = {definition};

            string serialized = JsonConvert.SerializeObject(
                new { definitions = deploymentExecutionDefinitions },
                Formatting.Indented);

            _output.WriteLine(serialized);

            ImmutableArray<DeploymentExecutionDefinition> deserializedObject =
                DeploymentExecutionDefinitionParser.Deserialize(serialized);

            Assert.Single(deserializedObject);

            string serializedDeserialized = JsonConvert.SerializeObject(
                new { definitions = deploymentExecutionDefinitions },
                Formatting.Indented);

            DeploymentExecutionDefinition deserializedDefinition = deserializedObject[0];

            Assert.Equal(serialized, serializedDeserialized);

            Assert.Equal(definition.PackageId, deserializedDefinition.PackageId);
        }
    }
}
