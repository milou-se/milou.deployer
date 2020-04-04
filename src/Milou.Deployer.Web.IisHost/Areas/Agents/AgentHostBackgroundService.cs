using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Arbor.Tooler;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Core.Settings;
using NuGet.Versioning;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [UsedImplicitly]
    public class AgentHostBackgroundService : BackgroundService
    {
        private readonly IApplicationSettingsStore _applicationSettingsStore;
        private readonly ILogger _logger;
        private readonly NuGetPackageInstaller _packageInstaller;

        public AgentHostBackgroundService(IApplicationSettingsStore applicationSettingsStore,
            ILogger logger,
            NuGetPackageInstaller packageInstaller)
        {
            _applicationSettingsStore = applicationSettingsStore;
            _logger = logger;
            _packageInstaller = packageInstaller;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            ApplicationSettings applicationSettings = await _applicationSettingsStore.GetApplicationSettings(stoppingToken);

            if (!applicationSettings.HostAgentEnabled)
            {
                _logger.Information("Host agent is disabled");
                return;
            }

            string? exePath = applicationSettings.AgentExe;
            if (string.IsNullOrWhiteSpace(applicationSettings.AgentExe))
            {
                _logger.Debug("No agent exe has been specified");

                SemanticVersion? currentVersion = await GetCurrentVersionAsync();
                NuGetPackageVersion nuGetPackageVersion = currentVersion is {}
                    ? new NuGetPackageVersion(currentVersion)
                    : NuGetPackageVersion.LatestAvailable;
                NuGetPackage nugetPackage = new NuGetPackage(new NuGetPackageId("Milou.Deployer.Web.Agent.Host"),
                    nuGetPackageVersion);
                NugetPackageSettings nugetPackageSettings = NugetPackageSettings.Default;
                string fileName = Assembly.GetExecutingAssembly().Location;

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return;
                }

                var fileInfo = new FileInfo(fileName);

                if (fileInfo.Directory is null)
                {
                    return;
                }

                DirectoryInfo targetDirectory = fileInfo.Directory.CreateSubdirectory("agent");
                NuGetPackageInstallResult result = await _packageInstaller.InstallPackageAsync(nugetPackage, nugetPackageSettings,
                    installBaseDirectory: targetDirectory, cancellationToken: stoppingToken);

                if (result?.SemanticVersion is {})
                {
                    exePath = Path.Combine(targetDirectory.FullName, "Milou.Deployer.Web.Agent.Host.exe");
                }
            }

            if (!File.Exists(exePath))
            {
                _logger.Debug("The specified agent exe '{AgentExe}' does not exist", applicationSettings.AgentExe);
                return;
            }

            _logger.Information("Starting agent as sub-process {Path}", applicationSettings.AgentExe);
            ExitCode exitCode = await ProcessRunner.ExecuteProcessAsync(
                applicationSettings.AgentExe,
                workingDirectory: new FileInfo(applicationSettings.AgentExe).Directory,
                cancellationToken: stoppingToken);

            if (!stoppingToken.IsCancellationRequested && !exitCode.IsSuccess)
            {
                _logger.Error("Failed to start agent from process {Process}", applicationSettings.AgentExe);
            }
        }

        private async Task<SemanticVersion?> GetCurrentVersionAsync() => default;
    }
}