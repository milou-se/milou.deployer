using System.Collections.Immutable;
using System.Threading.Tasks;
using Arbor.Tooler;
using Milou.Deployer.Bootstrapper.Common;
using Serilog;
using Serilog.Core;
using Xunit;

namespace Milou.Deployer.Bootstrapper.Tests.Integration
{
    public class WhenDownloadingMilouDeployer
    {
        [Fact]
        public async Task DownloadAsync()
        {
            string[] args = { "--prerelease", "--download-only" };

            using (Logger logger = new LoggerConfiguration().WriteTo.Debug().MinimumLevel.Verbose().CreateLogger())
            {
                using (App app = await App.CreateAsync(args, logger))
                {
                    NuGetPackageInstallResult nuGetPackageInstallResult =
                        await app.ExecuteAsync(args.ToImmutableArray());

                    Assert.NotNull(nuGetPackageInstallResult);
                    Assert.NotNull(nuGetPackageInstallResult.NuGetPackageId);
                    Assert.NotNull(nuGetPackageInstallResult.PackageDirectory);
                    Assert.NotNull(nuGetPackageInstallResult.SemanticVersion);
                }
            }
        }
    }
}
