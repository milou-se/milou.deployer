using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Milou.Deployer.Bootstrapper
{
    internal class Program
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
