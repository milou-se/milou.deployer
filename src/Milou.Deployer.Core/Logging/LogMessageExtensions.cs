using System;
using Serilog;
using Serilog.Events;

namespace Milou.Deployer.Core.Logging
{
    public static class LogMessageExtensions
    {
        private const string Verbose = "[Verbose]";
        private const string Debug = "[Debug]";
        private const string Error = "[Error]";
        private const string Fatal = "[Fatal]";
        private const string Information = "[Information]";
        private const string MessageTemplate = "{Message}";
        private const string MessageTemplateWithCategory = "{Category} {Message}";

        public static void ParseAndLog(this ILogger logger, string message, string category = null)
        {
            if (logger is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            (string Message, LogEventLevel Level) parsed = Parse(message);

            switch (parsed.Level)
            {
                case LogEventLevel.Fatal when string.IsNullOrWhiteSpace(category):
                    logger.Fatal(MessageTemplate, parsed.Message);
                    break;
                case LogEventLevel.Fatal:
                    logger.Fatal(MessageTemplateWithCategory, category, parsed.Message);
                    break;
                case LogEventLevel.Error when string.IsNullOrWhiteSpace(category):
                    logger.Error(MessageTemplate, parsed.Message);
                    break;
                case LogEventLevel.Error:
                    logger.Error(MessageTemplateWithCategory, category, parsed.Message);
                    break;
                case LogEventLevel.Information when string.IsNullOrWhiteSpace(category):
                    logger.Information(MessageTemplate, parsed.Message);
                    break;
                case LogEventLevel.Information:
                    logger.Information(MessageTemplateWithCategory, category, parsed.Message);
                    break;
                case LogEventLevel.Debug when string.IsNullOrWhiteSpace(category):
                    logger.Debug(MessageTemplate, parsed.Message);
                    break;
                case LogEventLevel.Debug:
                    logger.Debug(MessageTemplateWithCategory, category, parsed.Message);
                    break;
                case LogEventLevel.Verbose when string.IsNullOrWhiteSpace(category):
                    logger.Verbose(MessageTemplate, parsed.Message);
                    break;
                case LogEventLevel.Verbose:
                    logger.Verbose(MessageTemplateWithCategory, category, parsed.Message);
                    break;
            }
        }

        public static (string, LogEventLevel) Parse(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return (null, LogEventLevel.Information);
            }

            if (message.StartsWith(Information, StringComparison.OrdinalIgnoreCase))
            {
                return (message.Substring(Information.Length + 1).Trim(), LogEventLevel.Information);
            }

            if (message.StartsWith(Fatal, StringComparison.OrdinalIgnoreCase))
            {
                return (message.Substring(Fatal.Length + 1).Trim(), LogEventLevel.Fatal);
            }

            if (message.StartsWith(Error, StringComparison.OrdinalIgnoreCase))
            {
                return (message.Substring(Error.Length + 1).Trim(), LogEventLevel.Error);
            }

            if (message.StartsWith(Debug, StringComparison.OrdinalIgnoreCase))
            {
                return (message.Substring(Debug.Length + 1).Trim(), LogEventLevel.Debug);
            }

            if (message.StartsWith(Verbose, StringComparison.OrdinalIgnoreCase))
            {
                return (message.Substring(Verbose.Length + 1).Trim(), LogEventLevel.Verbose);
            }

            return (message, LogEventLevel.Information);
        }
    }
}