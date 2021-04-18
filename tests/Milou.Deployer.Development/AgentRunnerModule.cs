using System.Linq;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Agent.Host.Configuration;

namespace Milou.Deployer.Development
{
    [UsedImplicitly]
    public class AgentRunnerModule : IModule
    {
        private readonly IApplicationAssemblyResolver _applicationAssemblyResolver;
        private readonly DevConfiguration? _devConfiguration;

        public AgentRunnerModule(IApplicationAssemblyResolver applicationAssemblyResolver, DevConfiguration? devConfiguration = null)
        {
            _applicationAssemblyResolver = applicationAssemblyResolver;
            _devConfiguration = devConfiguration;
        }

        public IServiceCollection Register(IServiceCollection builder)
        {
            if (_devConfiguration is null)
            {
                return builder;
            }

            if (_applicationAssemblyResolver.GetAssemblies()
                .Any(assembly => !assembly.GetName().Name!.Contains(typeof(AgentConfiguration).Namespace!)))
            {
                return builder;
            }

            return builder;
        }
    }
}