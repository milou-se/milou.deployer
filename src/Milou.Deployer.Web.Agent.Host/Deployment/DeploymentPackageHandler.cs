using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.IO;
using Arbor.Processing;
using Milou.Deployer.Core.Deployment;
using Milou.Deployer.DeployerApp;
using Newtonsoft.Json;
using Serilog;

namespace Milou.Deployer.Web.Agent.Host.Deployment
{
    public class DeploymentPackageHandler : IDeploymentPackageHandler
    {
        public async Task<ExitCode> RunAsync(
            DeploymentTaskPackage deploymentTaskPackage,
            ILogger jobLogger,
            CancellationToken cancellationToken)
        {
            using var manifestFile = TempFile.CreateTempFile("manifest", ".json");
            using var nugetConfig = TempFile.CreateTempFile("nuget", ".config");

            // Deserialize manifest, add NuGet config, serialize

            if (string.IsNullOrWhiteSpace(deploymentTaskPackage.ManifestJson))
            {
                throw new InvalidOperationException("JSON manifest is missing");
            }

            var definitions = JsonConvert.DeserializeAnonymousType(deploymentTaskPackage.ManifestJson,
                new {definitions = Array.Empty<DeploymentExecutionDefinition>()});

            if (definitions?.definitions.Length != 1)
            {
                throw new InvalidOperationException($"Expected exactly 1 {nameof(DeploymentExecutionDefinition)}");
            }

            var deploymentExecutionDefinition = definitions.definitions[0];

            if (!string.IsNullOrWhiteSpace(deploymentTaskPackage.NuGetSource))
            {
                deploymentExecutionDefinition = deploymentExecutionDefinition with
                {
                    NuGetPackageSource = deploymentTaskPackage.NuGetSource
                };
            }

            if (!string.IsNullOrWhiteSpace(deploymentTaskPackage.NuGetConfigXml))
            {
                string? nuGetConfigFile = nugetConfig.File?.FullName;

                if (nuGetConfigFile is { })
                {
                    await File.WriteAllTextAsync(nuGetConfigFile, deploymentTaskPackage.NuGetConfigXml, cancellationToken);

                    deploymentExecutionDefinition = deploymentExecutionDefinition with
                    {
                        NuGetConfigFile = nuGetConfigFile
                    };
                }
            }

            string manifest =
                JsonConvert.SerializeObject(new {definitions = new[] {deploymentExecutionDefinition}});

            await File.WriteAllTextAsync(manifestFile.File!.FullName, manifest, Encoding.UTF8,
                cancellationToken);

            using var publishSettings = string.IsNullOrWhiteSpace(deploymentTaskPackage.PublishSettingsXml)
                ? null
                : TempFile.CreateTempFile(deploymentTaskPackage.DeploymentTargetId.TargetId, ".publishSettings");

            DirectoryInfo? currentDir = manifestFile.File!.Directory;

            if (string.IsNullOrWhiteSpace(currentDir?.FullName))
            {
                throw new InvalidOperationException("Current directory is not set");
            }

            if (publishSettings?.File?.Exists ?? false)
            {
                await File.WriteAllTextAsync(publishSettings.File.FullName, deploymentTaskPackage.PublishSettingsXml,
                    Encoding.UTF8, cancellationToken);

                publishSettings.File.CopyTo(Path.Combine(currentDir.FullName, publishSettings.File.Name));
            }

            Directory.SetCurrentDirectory(currentDir.FullName);

            string[] inputArgs = Array.Empty<string>();

            using DeployerApp.DeployerApp deployerApp =
                await AppBuilder.BuildAppAsync(inputArgs,
                    jobLogger,
                    cancellationToken);

            int result = await deployerApp.ExecuteAsync(
                inputArgs,
                cancellationToken);

            if (result != 0)
            {
                jobLogger.Warning("Milou.Deployer failed");
                return ExitCode.Failure;
            }

            return ExitCode.Success;
        }
    }
}