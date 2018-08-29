using System.Threading.Tasks;

namespace Milou.Deployer.ConsoleClient
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            int exitCode;

            using (DeployerApp deployerApp = AppBuilder.BuildApp(args))
            {
                exitCode = await deployerApp.ExecuteAsync(args);
            }

            return exitCode;
        }
    }
}
