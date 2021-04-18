using Serilog;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration
{
    public static class LoggerHelper
    {
        public static ILogger FromTestOutput(this ITestOutputHelper output) => new LoggerConfiguration().WriteTo
            .TestOutput(output).MinimumLevel.Verbose().CreateLogger();
    }
}