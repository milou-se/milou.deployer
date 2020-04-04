using System;
using JetBrains.Annotations;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Milou.Deployer.Web.Core.Logging
{
    public static class DelegateSinkExtensions
    {
        public static LoggerConfiguration DelegateSink(
            [NotNull] this LoggerSinkConfiguration loggerConfiguration,
            [NotNull] Action<string, LogEventLevel> action,
            LogEventLevel? minimumLevel = default)
        {
            if (loggerConfiguration is null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return loggerConfiguration.Sink(new DelegateSink(action), minimumLevel ?? LogEventLevel.Verbose);
        }
    }
}
