using System.Collections.Immutable;
using System.Threading.Tasks;
using Milou.Deployer.Bootstrapper.Common;

namespace Milou.Deployer.Bootstrapper.ConsoleClient
{
    public static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            int exitCode;

            using (App app = await App.CreateAsync(args))
            {
                exitCode = await app.ExecuteAsync(args.ToImmutableArray());
            }

            return exitCode;
        }
    }
}
