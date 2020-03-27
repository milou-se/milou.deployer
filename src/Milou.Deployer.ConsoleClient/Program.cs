using System.Threading.Tasks;

namespace Milou.Deployer.ConsoleClient
{
    public static class Program
    {
        public static async Task<int> Main(string[] args) => await AppBootstrapper.RunAsync(args);
    }
}