using System;
using Serilog.Events;

namespace Milou.Deployer.Core.Logging
{
    public static class LogEventLevelExtensions
    {
        public static LogEventLevel TryParseOrDefault(this string value, LogEventLevel defaultLevel)
        {
            if (int.TryParse(value, out int _))
            {
                return defaultLevel;
            }

            if (!Enum.TryParse(value, out LogEventLevel level))
            {
                return defaultLevel;
            }

            return level;
        }
    }
}
