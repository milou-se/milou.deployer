using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.Processing;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Core.Settings;
using Serilog;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentHostBackgroundService : BackgroundService
    {
        private readonly IApplicationSettingsStore _applicationSettingsStore;
        private ILogger _logger;

        public AgentHostBackgroundService(IApplicationSettingsStore applicationSettingsStore, ILogger logger)
        {
            _applicationSettingsStore = applicationSettingsStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            var applicationSettings = await _applicationSettingsStore.GetApplicationSettings(stoppingToken);

            if (string.IsNullOrWhiteSpace(applicationSettings.AgentExe))
            {
                _logger.Debug("No agent exe has been specified");
                return;
            }
            if (!File.Exists(applicationSettings.AgentExe))
            {
                _logger.Debug("The specified agent exe '{AgentExe}' does not exist", applicationSettings.AgentExe);
                return;
            }

            _logger.Information("Starting agent as sub-process {Path}", applicationSettings.AgentExe);
            var exitCode = await ProcessRunner.ExecuteProcessAsync(
                applicationSettings.AgentExe,
                workingDirectory: new FileInfo(applicationSettings.AgentExe).Directory,
                cancellationToken: stoppingToken);

            if (!stoppingToken.IsCancellationRequested && !exitCode.IsSuccess)
            {
                _logger.Error("Failed to start agent from process {Process}", applicationSettings.AgentExe);
            }
        }
    }
}