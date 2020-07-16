using System;
using System.Linq;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.DependencyInjection;
using Arbor.App.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Milou.Deployer.Web.Tests.Integration.TestData;

namespace Milou.Deployer.Web.Tests.Integration.Agents
{
    public class AgentsModule : IModule
    {
        private readonly IApplicationAssemblyResolver _applicationAssemblyResolver;
        private readonly TestHttpPort _testHttpPort;
        private readonly TestConfiguration _testConfiguration;

        public AgentsModule(IApplicationAssemblyResolver applicationAssemblyResolver, TestHttpPort testHttpPort, TestConfiguration testConfiguration)
        {
            _applicationAssemblyResolver = applicationAssemblyResolver;
            _testHttpPort = testHttpPort;
            _testConfiguration = testConfiguration;
        }

        public IServiceCollection Register(IServiceCollection builder)
        {
            if (_applicationAssemblyResolver.GetAssemblies().Any(assembly =>
                assembly.FullName is { } fullName && fullName.Contains("Web.Agent.Host", StringComparison.Ordinal)))
            {
                builder.AddSingleton(new SerilogConfiguration("http://localhost:5341", null, seqEnabled: true, consoleEnabled: true));
                string accessToken = _testConfiguration.AgentToken;
                builder.AddSingleton(new AgentConfiguration(accessToken, "http://localhost:" + _testHttpPort.Port));
            }

            return builder;
        }
    }
}