using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Milou.Deployer.Web.Core.NuGet;

namespace Milou.Deployer.Web.IisHost.Areas.WebHooks
{
    public interface IPackageWebHook
    {
        Task<PackageUpdatedEvent?> TryGetWebHookNotification(
            HttpRequest request,
            string content,
            CancellationToken cancellationToken);
    }
}