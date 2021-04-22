using System;
using System.Threading;
using System.Threading.Tasks;
using Arbor.KVConfiguration.Core;
using Microsoft.Extensions.Hosting;
using Serilog.Core;
using Serilog.Events;

namespace Milou.Deployer.Web.Agent
{
    public class LogLevelStartup : BackgroundService
    {
        private readonly IKeyValueConfiguration _keyValueConfiguration;
        private readonly LoggingLevelSwitch _loggingLevelSwitch;

        public LogLevelStartup(IKeyValueConfiguration keyValueConfiguration, LoggingLevelSwitch loggingLevelSwitch)
        {
            _keyValueConfiguration = keyValueConfiguration;
            _loggingLevelSwitch = loggingLevelSwitch;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!TimeSpan.TryParse(_keyValueConfiguration["urn:milou:deployer:log-level-start-reset-time"],
                    out var timeStamp)
                || Math.Abs(timeStamp.TotalSeconds) < 1)
            {
                return;
            }

            await Task.Yield();

            try
            {
                await Task.Delay(timeStamp, stoppingToken);

                _loggingLevelSwitch.MinimumLevel = LogEventLevel.Information;
            }
            catch (TaskCanceledException)
            {
                //
            }
        }
    }
}