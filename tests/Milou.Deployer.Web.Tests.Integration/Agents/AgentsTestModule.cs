﻿using System;
using System.Linq;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Milou.Deployer.Web.Tests.Integration.TestData;

namespace Milou.Deployer.Web.Tests.Integration.Agents
{
    [UsedImplicitly]
    public class AgentsTestModule : IModule
    {
        private readonly IApplicationAssemblyResolver _applicationAssemblyResolver;
        private readonly ServerEnvironmentTestConfiguration _serverEnvironmentTestConfiguration;
        private readonly TestConfiguration _testConfiguration;

        public AgentsTestModule(
            IApplicationAssemblyResolver applicationAssemblyResolver,
            ServerEnvironmentTestConfiguration serverEnvironmentTestConfiguration,
            TestConfiguration testConfiguration)
        {
            _applicationAssemblyResolver = applicationAssemblyResolver;
            _serverEnvironmentTestConfiguration = serverEnvironmentTestConfiguration;
            _testConfiguration = testConfiguration;
        }

        public IServiceCollection Register(IServiceCollection builder)
        {
            if (_applicationAssemblyResolver.GetAssemblies().Any(assembly =>
                assembly.FullName is { } fullName && fullName.Contains("Web.Agent.Host", StringComparison.Ordinal)))
            {
                string accessToken = _testConfiguration.AgentToken;
                builder.AddSingleton(new AgentConfiguration(accessToken,
                    $"http://localhost:{_serverEnvironmentTestConfiguration.Port}",
                    startupDelay: TimeSpan.FromMilliseconds(50)), this);
            }

            return builder;
        }
    }
}