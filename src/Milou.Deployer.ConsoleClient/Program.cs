using System.Threading.Tasks;

namespace Milou.Deployer.ConsoleClient
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            int exitCode;

            using (DeployerApp deployerApp = await AppBuilder.BuildAppAsync(args).ConfigureAwait(false))
            {
                exitCode = await deployerApp.ExecuteAsync(args).ConfigureAwait(false);
            }

            return exitCode;
        }
    }
}
