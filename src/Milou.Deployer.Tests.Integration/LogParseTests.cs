using Milou.Deployer.Core.Logging;
using Serilog.Events;
using Xunit;

namespace Milou.Deployer.Tests.Integration
{
    public class LogParseTests
    {
        [Fact]
        public void ParseMessageStartsWithInformation()
        {
            (string Message, LogEventLevel Level) logItem = LogMessageExtensions.Parse("[Information] My message");

            Assert.Equal("My message", logItem.Message);
            Assert.Equal(LogEventLevel.Information, logItem.Level);
        }

        [Fact]
        public void ParseMessageStartsWithFatal()
        {
            (string Message, LogEventLevel Level) logItem = LogMessageExtensions.Parse("[Fatal] My message");

            Assert.Equal("My message", logItem.Message);
            Assert.Equal(LogEventLevel.Fatal, logItem.Level);
        }

        [Fact]
        public void ParseMessageStartsWithError()
        {
            (string Message, LogEventLevel Level) logItem = LogMessageExtensions.Parse("[Error] My message");

            Assert.Equal("My message", logItem.Message);
            Assert.Equal(LogEventLevel.Error, logItem.Level);
        }

        [Fact]
        public void ParseMessageStartsWithDebug()
        {
            (string Message, LogEventLevel Level) logItem = LogMessageExtensions.Parse("[Debug] My message");

            Assert.Equal("My message", logItem.Message);
            Assert.Equal(LogEventLevel.Debug, logItem.Level);
        }

        [Fact]
        public void ParseMessageStartsWithVerbose()
        {
            (string Message, LogEventLevel Level) logItem = LogMessageExtensions.Parse("[Verbose] My message");

            Assert.Equal("My message", logItem.Message);
            Assert.Equal(LogEventLevel.Verbose, logItem.Level);
        }
    }
}