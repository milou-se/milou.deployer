using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class TestBackgroundService : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}