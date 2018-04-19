using System.Threading.Tasks;

namespace Milou.Deployer.ConsoleClient
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            int exitCode = await AppBuilder.BuildApp().ExecuteAsync(args);

            return exitCode;
        }
    }
}
