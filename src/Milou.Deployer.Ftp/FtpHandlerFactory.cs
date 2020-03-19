using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Core.Deployment.Ftp;
using Serilog;

namespace Milou.Deployer.Ftp
{
    public class FtpHandlerFactory : IFtpHandlerFactory
    {
        public async Task<IFtpHandler> CreateWithPublishSettings(string publishSettingsFile,
            FtpSettings ftpSettings,
            ILogger logger,
            CancellationToken cancellationToken = default) =>
            await FtpHandler.CreateWithPublishSettings(publishSettingsFile, ftpSettings, logger);
    }
}