using Arbor.App.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Core.Agents;

namespace Milou.Deployer.Web.IisHost.Areas.Agents
{
    [UsedImplicitly]
    public class AgentsModule : IModule
    {
        public IServiceCollection Register(IServiceCollection builder) => builder.AddSingleton<AgentsData>(this);
    }
}