using Serilog.Events;

namespace Milou.Deployer.Core.Deployment.Ftp
{
    public class FtpSettings
    {
        public FtpSettings(
            FtpPath? basePath = default,
            bool isSecure = true,
            int batchSize = 10,
            int maxAttempts = 3,
            LogEventLevel logLevel = LogEventLevel.Information,
            FtpPath? publicRootPath = null)
        {
            BasePath = basePath;
            IsSecure = isSecure;
            BatchSize = batchSize;
            MaxAttempts = maxAttempts;
            LogLevel = logLevel;
            PublicRootPath = publicRootPath;
        }

        public FtpPath? PublicRootPath { get; }

        public FtpPath? BasePath { get; }

        public bool IsSecure { get; }

        public int BatchSize { get; }

        public int MaxAttempts { get; }

        public LogEventLevel LogLevel { get; }
    }
}