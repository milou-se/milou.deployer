using System.Threading;
using System.Threading.Tasks;

namespace Milou.Deployer.Web.Core.Deployment.Targets
{
    public interface IDataSeeder
    {
        int Order { get; }
        Task SeedAsync(CancellationToken cancellationToken);
    }
}