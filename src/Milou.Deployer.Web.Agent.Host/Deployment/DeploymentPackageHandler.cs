using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.IO;
using Arbor.Processing;
using Milou.Deployer.DeployerApp;
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

            await File.WriteAllTextAsync(manifestFile.File!.FullName, deploymentTaskPackage.ManifestJson, Encoding.UTF8,
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