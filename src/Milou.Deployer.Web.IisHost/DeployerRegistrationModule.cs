using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.Http;
using Arbor.AspNetCore.Host;
using Arbor.AspNetCore.Host.Hosting;
using Arbor.KVConfiguration.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.IisHost.Areas.Security;
using Milou.Deployer.Web.IisHost.AspNetCore.Startup;
using Serilog;

namespace Milou.Deployer.Web.IisHost
{
    [UsedImplicitly]
    public class DeployerRegistrationModule : IServiceProviderModule
    {
        public void Register(ServiceProviderHolder serviceProviderHolder)
        {
            IServiceCollection services = serviceProviderHolder.ServiceCollection;

            CustomOpenIdConnectConfiguration? openIdConnectConfiguration =
                serviceProviderHolder.ServiceProvider.GetService<CustomOpenIdConnectConfiguration>();

            var applicationAssemblyResolver = serviceProviderHolder.ServiceProvider.GetRequiredService<IApplicationAssemblyResolver>();

            HttpLoggingConfiguration httpLoggingConfiguration =
                serviceProviderHolder.ServiceProvider.GetRequiredService<HttpLoggingConfiguration>();

            MilouAuthenticationConfiguration milouAuthenticationConfiguration =
                serviceProviderHolder.ServiceProvider.GetRequiredService<MilouAuthenticationConfiguration>();

            ILogger logger = serviceProviderHolder.ServiceProvider.GetRequiredService<ILogger>();

            EnvironmentConfiguration environmentConfiguration =
                serviceProviderHolder.ServiceProvider.GetRequiredService<EnvironmentConfiguration>();

            IKeyValueConfiguration configuration =
                serviceProviderHolder.ServiceProvider.GetRequiredService<IKeyValueConfiguration>();

            services.AddDeploymentAuthentication(openIdConnectConfiguration, milouAuthenticationConfiguration, logger,
                    environmentConfiguration)
                .AddDeploymentAuthorization(environmentConfiguration)
                .AddHttpClientsWithConfiguration(httpLoggingConfiguration)
                .AddDeploymentSignalR()
                .AddServerFeatures()
                .AddDeploymentMvc(environmentConfiguration, configuration, logger, applicationAssemblyResolver);
        }
    }
}