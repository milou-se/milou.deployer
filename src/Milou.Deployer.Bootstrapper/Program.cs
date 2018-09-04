using System.Collections.Immutable;
using System.Threading.Tasks;
using Arbor.Tooler;
using Milou.Deployer.Bootstrapper.Common;

namespace Milou.Deployer.Bootstrapper
{
    public static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            int exitCode;

            using (App app = await App.CreateAsync(args))
            {
                NuGetPackageInstallResult nuGetPackageInstallResult = await app.ExecuteAsync(args.ToImmutableArray());

                exitCode = nuGetPackageInstallResult.SemanticVersion != null && nuGetPackageInstallResult.PackageDirectory != null ? 0 : 1;
            }

            return exitCode;
        }
    }
}
