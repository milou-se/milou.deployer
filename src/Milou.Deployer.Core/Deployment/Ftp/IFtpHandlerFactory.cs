using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Milou.Deployer.Core.Deployment.Ftp
{
    public interface IFtpHandlerFactory
    {
        Task<IFtpHandler> CreateWithPublishSettings(string publishSettingsFile,
            FtpSettings ftpSettings,
            ILogger logger,
            CancellationToken cancellationToken = default);
    }
}