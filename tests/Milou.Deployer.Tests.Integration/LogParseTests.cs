using Milou.Deployer.Core.Logging;
using Serilog.Events;
using Xunit;

namespace Milou.Deployer.Tests.Integration
{
    public class LogParseTests
    {
        [Fact]
        public void ParseMessageStartsWithDebug()
        {
            (string? message, var level) = LogMessageExtensions.Parse("[Debug] My message");

            Assert.Equal("My message", message);
            Assert.Equal(LogEventLevel.Debug, level);
        }

        [Fact]
        public void ParseMessageStartsWithError()
        {
            (string? message, var level) = LogMessageExtensions.Parse("[Error] My message");

            Assert.Equal("My message", message);
            Assert.Equal(LogEventLevel.Error, level);
        }

        [Fact]
        public void ParseMessageStartsWithFatal()
        {
            (string? message, var level) = LogMessageExtensions.Parse("[Fatal] My message");

            Assert.Equal("My message", message);
            Assert.Equal(LogEventLevel.Fatal, level);
        }

        [Fact]
        public void ParseMessageStartsWithInformation()
        {
            (string? message, var level) = LogMessageExtensions.Parse("[Information] My message");

            Assert.Equal("My message", message);
            Assert.Equal(LogEventLevel.Information, level);
        }

        [Fact]
        public void ParseMessageStartsWithVerbose()
        {
            (string? message, var level) = LogMessageExtensions.Parse("[Verbose] My message");

            Assert.Equal("My message", message);
            Assert.Equal(LogEventLevel.Verbose, level);
        }
    }
}