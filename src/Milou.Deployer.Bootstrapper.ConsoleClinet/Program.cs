using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
                using (var cts = new CancellationTokenSource(GetTimeout(args)))
                {
                    NuGetPackageInstallResult nuGetPackageInstallResult =
                        await app.ExecuteAsync(args.ToImmutableArray(), cancellationToken: cts.Token).ConfigureAwait(false);

                    exitCode = nuGetPackageInstallResult.SemanticVersion != null &&
                               nuGetPackageInstallResult.PackageDirectory != null
                        ? 0
                        : 1;
                }
            }

            return exitCode;
        }

        static TimeSpan GetTimeout(string[] args)
        {
            if (!int.TryParse(
                    args.SingleOrDefault(arg => arg.StartsWith("timeout-in-seconds=", StringComparison.OrdinalIgnoreCase))?.Split('=').LastOrDefault(),
                    out int timeoutInSeconds) || timeoutInSeconds <= 0)
            {
                return TimeSpan.FromSeconds(60);
            }

            return TimeSpan.FromSeconds(timeoutInSeconds);

        }
    }
}
