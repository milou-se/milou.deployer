using System.Collections.Immutable;
using System.Threading.Tasks;
using Arbor.Tooler;
using Milou.Deployer.Bootstrapper.Common;
using Serilog;
using Serilog.Core;
using Xunit;
using Constants = Milou.Deployer.Bootstrapper.Common.Constants;

namespace Milou.Deployer.Bootstrapper.Tests.Integration
{
    public class WhenDownloadingMilouDeployer
    {
        [Fact(Skip = "Issue with all packages list by NuGet")]
        public async Task DownloadAsync()
        {
            string[] args = {Constants.AllowPreRelease, Constants.DownloadOnly};

            using Logger logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .MinimumLevel.Verbose()
                .CreateLogger();
            using BootstrapperApp bootstrapperApp = await BootstrapperApp.CreateAsync(args, logger);
            NuGetPackageInstallResult nuGetPackageInstallResult =
                await bootstrapperApp.ExecuteAsync(args.ToImmutableArray());

            Assert.NotNull(nuGetPackageInstallResult);
            Assert.NotNull(nuGetPackageInstallResult.NuGetPackageId);
            Assert.NotNull(nuGetPackageInstallResult.PackageDirectory);
            Assert.NotNull(nuGetPackageInstallResult.SemanticVersion);
        }
    }
}