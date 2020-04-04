using Serilog;
using Xunit.Abstractions;

namespace Milou.Deployer.Web.Tests.Unit
{
    public static class LoggerHelper
    {
        public static ILogger FromTestOutput(this ITestOutputHelper output) => new LoggerConfiguration().WriteTo.TestOutput(output).CreateLogger();
    }
}