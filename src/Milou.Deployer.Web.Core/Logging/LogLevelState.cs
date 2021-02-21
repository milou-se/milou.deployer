using System;
using Arbor.App.Extensions.Time;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Milou.Deployer.Web.Core.Logging
{
    public class LogLevelState
    {
        private readonly ILogger _logger;
        private readonly ICustomClock _customClock;

        public LoggingLevelSwitch LevelSwitch { get; }

        public LogLevelState(LoggingLevelSwitch levelSwitch, ILogger logger, ICustomClock customClock)
        {
            LevelSwitch = levelSwitch;
            _logger = logger;
            _customClock = customClock;
        }

        public DateTimeOffset? ValidToUtc { get; set; }

        public void SetLevel(LogEventLevel newLevel, TimeSpan timeSpan)
        {
            var oldLevel = LevelSwitch.MinimumLevel;

            if (oldLevel != newLevel)
            {
                _logger.Information("Switching log level from {OldLogLevel} to {NewLogLevel}",
                    oldLevel,
                    newLevel);

                LevelSwitch.MinimumLevel = newLevel;

                if (timeSpan < TimeSpan.Zero || timeSpan.TotalHours > 1)
                {
                    const int minutes = 5;
                    timeSpan = TimeSpan.FromMinutes(minutes);
                    _logger.Warning("Invalid timespan for log level, setting time to {Minutes} minutes", minutes);
                }

                _logger.Information("Switched log level from {OldLogLevel} to {NewLogLevel} for {Minutes}",
                    oldLevel,
                    newLevel,
                    timeSpan.TotalMinutes.ToString("F1"));

                ValidToUtc = _customClock.UtcNow().Add(timeSpan);
            }
        }
    }
}