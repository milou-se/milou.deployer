using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Milou.Deployer.Web.Core.Startup;

namespace Milou.Deployer.Development
{
    [UsedImplicitly]
    public class AgentStartTask : IStartupTask
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public bool IsCompleted { get; private set; }
    }
}