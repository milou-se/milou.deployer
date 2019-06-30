namespace Milou.Deployer.Core.Deployment
{
    public class FtpSettings
    {
        public FtpSettings(FtpPath basePath = default, bool isSecure = true, int batchSize = 10, int maxAttempts = 3)
        {
            BasePath = basePath;
            IsSecure = isSecure;
            BatchSize = batchSize;
            MaxAttempts = maxAttempts;
        }

        public FtpPath BasePath { get; }

        public bool IsSecure { get; }

        public int BatchSize { get; }

        public int MaxAttempts { get; }
    }
}
