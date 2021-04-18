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

        public static void ParseAndLog(this ILogger? logger, string? message, string? category = null)
        {
            if (logger is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            (string? parsedMessage, var level) = Parse(message!);

            switch (level)
            {
                case LogEventLevel.Fatal when string.IsNullOrWhiteSpace(category):
                    logger.Fatal(MessageTemplate, parsedMessage);
                    break;
                case LogEventLevel.Fatal:
                    logger.Fatal(MessageTemplateWithCategory, category, parsedMessage);
                    break;
                case LogEventLevel.Error when string.IsNullOrWhiteSpace(category):
                    logger.Error(MessageTemplate, parsedMessage);
                    break;
                case LogEventLevel.Error:
                    logger.Error(MessageTemplateWithCategory, category, parsedMessage);
                    break;
                case LogEventLevel.Information when string.IsNullOrWhiteSpace(category):
                    logger.Information(MessageTemplate, parsedMessage);
                    break;
                case LogEventLevel.Information:
                    logger.Information(MessageTemplateWithCategory, category, parsedMessage);
                    break;
                case LogEventLevel.Debug when string.IsNullOrWhiteSpace(category):
                    logger.Debug(MessageTemplate, parsedMessage);
                    break;
                case LogEventLevel.Debug:
                    logger.Debug(MessageTemplateWithCategory, category, parsedMessage);
                    break;
                case LogEventLevel.Verbose when string.IsNullOrWhiteSpace(category):
                    logger.Verbose(MessageTemplate, parsedMessage);
                    break;
                case LogEventLevel.Verbose:
                    logger.Verbose(MessageTemplateWithCategory, category, parsedMessage);
                    break;
            }
        }

        internal static (string?, LogEventLevel) Parse(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return (null, LogEventLevel.Information);
            }

            if (message.StartsWith(Information, StringComparison.OrdinalIgnoreCase))
            {
                return (message[(Information.Length + 1)..].Trim(), LogEventLevel.Information);
            }

            if (message.StartsWith(Fatal, StringComparison.OrdinalIgnoreCase))
            {
                return (message[(Fatal.Length + 1)..].Trim(), LogEventLevel.Fatal);
            }

            if (message.StartsWith(Error, StringComparison.OrdinalIgnoreCase))
            {
                return (message[(Error.Length + 1)..].Trim(), LogEventLevel.Error);
            }

            if (message.StartsWith(Debug, StringComparison.OrdinalIgnoreCase))
            {
                return (message[(Debug.Length + 1)..].Trim(), LogEventLevel.Debug);
            }

            if (message.StartsWith(Verbose, StringComparison.OrdinalIgnoreCase))
            {
                return (message[(Verbose.Length + 1)..].Trim(), LogEventLevel.Verbose);
            }

            return (message, LogEventLevel.Information);
        }
    }
}