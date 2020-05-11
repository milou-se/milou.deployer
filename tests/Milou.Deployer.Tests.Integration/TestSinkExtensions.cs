using System;
using Serilog;
using Serilog.Configuration;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration
{
    public static class TestSinkExtensions
    {
        public static LoggerConfiguration TestSink(
            this LoggerSinkConfiguration loggerConfiguration,
            ITestOutputHelper testOutputHelper,
            IFormatProvider? formatProvider = null) =>
            loggerConfiguration.Sink(new TestSink(formatProvider, testOutputHelper));
    }
}