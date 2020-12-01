using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arbor.App.Extensions.Application;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Milou.Deployer.Web.Agent;
using Milou.Deployer.Web.Agent.Host.Configuration;
using Serilog;

namespace Milou.Deployer.Web.Tests.Integration
{
    public class TestBackgroundService : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }

    public class AgentResolveServices : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EnvironmentConfiguration _environmentConfiguration;

        public AgentResolveServices(IServiceProvider serviceProvider, ILogger logger, EnvironmentConfiguration environmentConfiguration)
        {
            _serviceProvider = serviceProvider;
            _environmentConfiguration = environmentConfiguration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_environmentConfiguration.HttpEnabled)
            {
                return Task.CompletedTask;
            }

            var environment = _serviceProvider.GetRequiredService<EnvironmentConfiguration>();

            var logger = _serviceProvider.GetRequiredService<ILogger>();

            var types = new List<Type>
            {
                typeof(IDeploymentPackageAgent),
                typeof(AgentConfiguration),
                typeof(ILogger),
                typeof(IMediator)
            };

            foreach (var type in types)
            {
                try
                {
                    _ = _serviceProvider.GetRequiredService(type);
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, "Could not get service type {Type} in configuration {@Configuration}", type.Name, environment);
                }
            }

            return Task.CompletedTask;
        }
    }
}