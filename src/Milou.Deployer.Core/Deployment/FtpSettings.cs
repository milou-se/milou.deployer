namespace Milou.Deployer.Core.Deployment
{
    public class FtpSettings
    {
        public FtpSettings(FtpPath basePath = default, bool isSecure = true)
        {
            BasePath = basePath;
            IsSecure = isSecure;
        }

        public FtpPath BasePath { get; }

        public bool IsSecure { get; }
    }
}
