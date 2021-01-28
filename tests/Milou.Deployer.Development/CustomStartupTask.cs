using System.Threading;
using System.Threading.Tasks;
using DotNext.Collections.Generic;
using Milou.Deployer.Web.Core.Deployment.Targets;

namespace Milou.Deployer.Development
{
    public class CustomStartupTask : IDataSeeder
    {
        public int Order { get; }

        public Task SeedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}