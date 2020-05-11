using System;
using System.Threading.Tasks;
using Milou.Deployer.Core.Extensions;

namespace Milou.Deployer.DeployerApp
{
    public static class AppBootstrapper
    {
        public static async Task<int> RunAsync(string[] args)
        {
            int exitCode;
            try
            {
                using DeployerApp deployerApp = await AppBuilder.BuildAppAsync(args).ConfigureAwait(false);
                try
                {
                    exitCode = await deployerApp.ExecuteAsync(args).ConfigureAwait(false);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    deployerApp.Logger.Fatal(ex, "Could not execute deployment");
                    exitCode = 2;
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                Console.Error.WriteLine(ex);
                exitCode = 4;
            }

            return exitCode;
        }
    }
}