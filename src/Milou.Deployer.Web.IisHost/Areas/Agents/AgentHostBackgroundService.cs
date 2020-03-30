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
        private readonly ApplicationSettings _applicationSettings;
        private ILogger _logger;

        public AgentHostBackgroundService(ApplicationSettings applicationSettings, ILogger logger)
        {
            _applicationSettings = applicationSettings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            if (string.IsNullOrWhiteSpace(_applicationSettings.AgentExe))
            {
                _logger.Debug("No agent exe has been specified");
                return;
            }

            if (!File.Exists(_applicationSettings.AgentExe))
            {
                _logger.Debug("The specified agent exe '{AgentExe}' does not exist", _applicationSettings.AgentExe);
                return;
            }

            _logger.Information("Starting agent as sub-process {Path}", _applicationSettings.AgentExe);
            var exitCode = await ProcessRunner.ExecuteProcessAsync(_applicationSettings.AgentExe, cancellationToken: stoppingToken);

            if (!stoppingToken.IsCancellationRequested && !exitCode.IsSuccess)
            {
                _logger.Error("Failed to start agent from process {Process}", _applicationSettings.AgentExe);
            }
        }
    }
}