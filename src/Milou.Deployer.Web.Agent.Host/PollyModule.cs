using System;
using System.Net;
using System.Net.Http;
using Arbor.App.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Agent.Host.Deployment;
using Polly;
using Polly.Extensions.Http;

namespace Milou.Deployer.Web.Agent.Host
{
    public class PollyModule : IModule
    {
        public IServiceCollection Register(IServiceCollection builder)
        {
            builder.AddHttpClient();
            builder.AddHttpClient<DeploymentTaskPackageService>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy());

            static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
            {
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            }

            return builder;
        }
    }
}