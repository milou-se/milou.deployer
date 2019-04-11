using System;
using System.Threading.Tasks;

namespace Milou.Deployer.ConsoleClient
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            int exitCode;
            try
            {
                using (DeployerApp deployerApp = await AppBuilder.BuildAppAsync(args).ConfigureAwait(false))
                {
                    try
                    {
                        exitCode = await deployerApp.ExecuteAsync(args).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        deployerApp.Logger.Fatal(ex, "Could not execute deployment");
                        exitCode = 2;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                exitCode = 4;
            }

            return exitCode;
        }
    }
}
