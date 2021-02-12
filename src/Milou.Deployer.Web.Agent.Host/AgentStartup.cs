using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Milou.Deployer.Web.Agent.Host
{
    [PublicAPI]
    public sealed class AgentStartup
    {
        [PublicAPI]
        public void ConfigureServices(IServiceCollection services)
        {
            // convention method
        }

        [PublicAPI]
        public void Configure(IApplicationBuilder app)
        {
            // convention method
        }
    }
}