using System;
using System.Threading;
using System.Threading.Tasks;

namespace Milou.Deployer.Development
{
    public class AgentRunner
    {
        private readonly Task<int> _appTask;
        public CancellationTokenSource CancellationTokenSource { get; }

        public AgentRunner(CancellationTokenSource cancellationTokenSource, Task<int> appTask)
        {
            _appTask = appTask;
            CancellationTokenSource = cancellationTokenSource;
        }

        public async Task StopAsync()
        {
            try
            {
                CancellationTokenSource.Cancel();

                await _appTask;
            }
            catch (Exception e)
            {
            }
        }
    }
}