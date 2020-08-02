using Arbor.App.Extensions.Configuration;
using Arbor.App.Extensions.DependencyInjection;
using Arbor.App.Extensions.Time;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Agent.Host.Deployment;
using Milou.Deployer.Web.Agent.Host.Logging;

namespace Milou.Deployer.Web.Agent.Host.Configuration
{
    [UsedImplicitly]
    public class AgentModule : IModule
    {
        public IServiceCollection Register(IServiceCollection builder)
        {
            builder.AddSingleton(
                new TimeoutHelper(new TimeoutConfiguration {CancellationEnabled = false}), this);

            builder.AddSingleton<DeploymentTaskPackageService>(this);
            builder.AddSingleton<LogHttpClientFactory>(this);
            builder.AddSingleton<IDeploymentPackageAgent, DeploymentPackageAgent>(this);
            builder.AddSingleton<IDeploymentPackageHandler, DeploymentPackageHandler>(this);
            builder.AddSingleton<IConfigureEnvironment, AgentConfigureEnvironment>(this);

            return builder;
        }
    }
}