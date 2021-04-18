using System;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Time;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Milou.Deployer.Web.Core.Logging
{
    [UsedImplicitly]
    public class LogLevelBackgroundService : BackgroundService
    {
        private readonly LogLevelState _logLevelState;
        private readonly ICustomClock _customClock;
        private readonly ILogger _logger;

        public LogLevelBackgroundService(LogLevelState logLevelState, ICustomClock customClock, ILogger logger)
        {
            _logLevelState = logLevelState;
            _customClock = customClock;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                var defaultLevel = LogEventLevel.Information;

                if (_logLevelState.ValidToUtc is null)
                {
                    continue;
                }

                if (_customClock.UtcNow() > _logLevelState.ValidToUtc
                    && _logLevelState.LevelSwitch.MinimumLevel != defaultLevel)
                {
                    _logLevelState.LevelSwitch.MinimumLevel = defaultLevel;
                    _logLevelState.ValidToUtc = null;

                    _logger.Information("Log level reset to {DefaultLevel}", defaultLevel);
                }
            }
        }
    }
}