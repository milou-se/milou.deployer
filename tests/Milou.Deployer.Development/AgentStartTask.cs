using System.Threading;
using System.Threading.Tasks;
using Milou.Deployer.Web.Core.Startup;

namespace Milou.Deployer.Development
{
    public class AgentStartTask : IStartupTask
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        public bool IsCompleted { get; private set; }
    }
}