using Arbor.App.Extensions.Application;
using Arbor.AspNetCore.Host;
using Arbor.AspNetCore.Host.Hosting;
using Arbor.KVConfiguration.Core;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Core.Logging;
using Milou.Deployer.Web.IisHost.Areas.Security;
using Milou.Deployer.Web.IisHost.AspNetCore.Startup;
using Serilog;

namespace Milou.Deployer.Web.IisHost
{
    public class DeployerRegistrationModule : IServiceProviderModule
    {
        public void Register(ServiceProviderHolder serviceProviderHolder)
        {
            IServiceCollection services = serviceProviderHolder.ServiceCollection;

            CustomOpenIdConnectConfiguration openIdConnectConfiguration =
                serviceProviderHolder.ServiceProvider.GetService<CustomOpenIdConnectConfiguration>();

            HttpLoggingConfiguration httpLoggingConfiguration =
                serviceProviderHolder.ServiceProvider.GetService<HttpLoggingConfiguration>();

            MilouAuthenticationConfiguration milouAuthenticationConfiguration =
                serviceProviderHolder.ServiceProvider.GetService<MilouAuthenticationConfiguration>();

            ILogger logger = serviceProviderHolder.ServiceProvider.GetRequiredService<ILogger>();
            EnvironmentConfiguration environmentConfiguration =
                serviceProviderHolder.ServiceProvider.GetRequiredService<EnvironmentConfiguration>();
            IKeyValueConfiguration configuration =
                serviceProviderHolder.ServiceProvider.GetRequiredService<IKeyValueConfiguration>();

            services.AddDeploymentAuthentication(openIdConnectConfiguration, milouAuthenticationConfiguration, logger,
                    environmentConfiguration)
                .AddDeploymentAuthorization(environmentConfiguration)
                .AddDeploymentHttpClients(httpLoggingConfiguration)
                .AddDeploymentSignalR()
                .AddServerFeatures()
                .AddDeploymentMvc(environmentConfiguration, configuration, logger);
        }
    }
}