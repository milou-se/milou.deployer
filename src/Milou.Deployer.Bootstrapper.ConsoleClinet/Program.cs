using System.Collections.Immutable;
using System.Threading.Tasks;
using Arbor.Tooler;
using Milou.Deployer.Bootstrapper.Common;

namespace Milou.Deployer.Bootstrapper.ConsoleClient
{
    public static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            int exitCode;

            using (App app = await App.CreateAsync(args).ConfigureAwait(false))
            {
                NuGetPackageInstallResult nuGetPackageInstallResult = await app.ExecuteAsync(args.ToImmutableArray()).ConfigureAwait(false);

                exitCode = nuGetPackageInstallResult.SemanticVersion != null && nuGetPackageInstallResult.PackageDirectory != null ? 0 : 1;
            }

            return exitCode;
        }
    }
}
