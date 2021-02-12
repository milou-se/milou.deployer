using System.Linq;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Agent.Host.Configuration;

namespace Milou.Deployer.Development
{
    public class AgentRunnerModule : IModule
    {
        private readonly IApplicationAssemblyResolver _applicationAssemblyResolver;
        private readonly DevConfiguration? _devConfiguration;
        private readonly EnvironmentConfiguration _environmentConfiguration;

        public AgentRunnerModule(IApplicationAssemblyResolver applicationAssemblyResolver, EnvironmentConfiguration environmentConfiguration, DevConfiguration? devConfiguration = null)
        {
            _applicationAssemblyResolver = applicationAssemblyResolver;
            _devConfiguration = devConfiguration;
            _environmentConfiguration = environmentConfiguration;
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