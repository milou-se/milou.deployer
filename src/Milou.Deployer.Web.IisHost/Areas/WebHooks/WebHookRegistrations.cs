using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Core;

namespace Milou.Deployer.Web.IisHost.Areas.WebHooks
{
    [UsedImplicitly]
    public class WebHookRegistrations : IModule
    {
        private readonly IApplicationAssemblyResolver _applicationAssemblyResolver;

        public WebHookRegistrations(IApplicationAssemblyResolver applicationAssemblyResolver) =>
            _applicationAssemblyResolver = applicationAssemblyResolver;

        public IServiceCollection Register(IServiceCollection builder) =>
            builder.AddSingleton<PackageWebHookHandler>()
                .RegisterAssemblyTypes<IPackageWebHook>(_applicationAssemblyResolver.GetAssemblies(),
                    ServiceLifetime.Singleton);
    }
}