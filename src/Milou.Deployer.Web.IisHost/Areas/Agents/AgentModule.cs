using Arbor.App.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Core.Deployment;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    public class AgentModule : IModule
    {
        public IServiceCollection Register(IServiceCollection builder)
        {
            return builder.AddSingleton<IAgentService, RemoteAgentService>()
                .AddSingleton<AgentHub>();
        }
    }
}