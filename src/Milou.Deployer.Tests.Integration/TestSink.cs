using System;
using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;

namespace Milou.Deployer.Tests.Integration
{
    public class TestSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;
        private readonly ITestOutputHelper _helper;

        public TestSink(IFormatProvider formatProvider, ITestOutputHelper helper)
        {
            _formatProvider = formatProvider;
            _helper = helper;
        }

        public void Emit(LogEvent logEvent)
        {
            string message = logEvent.RenderMessage(_formatProvider);

            string actualMessage = message ?? logEvent.MessageTemplate.Render(logEvent.Properties);

            string line = $"[{logEvent.Level}] {actualMessage}";
            Debug.WriteLine(line);

            _helper.WriteLine(line);
            if (logEvent.Exception != null)
            {
                string format = logEvent.Exception.ToString();
                _helper.WriteLine(format);
                Debug.WriteLine(format);
            }

        }
    }
}
