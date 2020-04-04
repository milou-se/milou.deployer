using System;
using Arbor.App.Extensions.Logging;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Integration
{
    [UsedImplicitly]
    public class XunitAppLoggingConfiguration : ILoggerConfigurationHandler
    {
        private readonly ITestOutputHelper? _testOutputHelper;

        public XunitAppLoggingConfiguration([NotNull] LoggingLevelSwitch levelSwitch, ITestOutputHelper? testOutputHelper = null)
        {
            if (levelSwitch is null)
            {
                throw new ArgumentNullException(nameof(levelSwitch));
            }

            levelSwitch.MinimumLevel = LogEventLevel.Verbose;
            _testOutputHelper = testOutputHelper;
        }

        public LoggerConfiguration Handle([NotNull] LoggerConfiguration loggerConfiguration)
        {
            if (loggerConfiguration is null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }

            if (_testOutputHelper is null)
            {
                return loggerConfiguration;
            }

            return loggerConfiguration
                .WriteTo.TestOutput(_testOutputHelper)
                .WriteTo.Debug()
                .WriteTo.Console();
        }
    }
}
