using System.Threading.Tasks;
using Milou.Deployer.DeployerApp;

namespace Milou.Deployer.ConsoleClient
{
    public static class Program
    {
        public static async Task<int> Main(string[] args) => await AppBootstrapper.RunAsync(args);
    }
}