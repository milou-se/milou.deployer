using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Milou.Deployer.Web.Core.Deployment.Packages;

namespace Milou.Deployer.Web.IisHost.Areas.NuGet
{
    public interface IPackageService
    {
        Task<IReadOnlyCollection<PackageVersion>> GetPackageVersionsAsync(
            [NotNull] string packageId,
            bool useCache = true,
            bool includePreReleased = false,
            string? nugetPackageSource = null,
            string? nugetConfigFile = null,
            CancellationToken cancellationToken = default);
    }
}