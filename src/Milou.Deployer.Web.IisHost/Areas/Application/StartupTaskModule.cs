using System;
using System.Collections.Generic;
using System.Linq;
using Arbor.App.Extensions.Application;
using Arbor.App.Extensions.DependencyInjection;
using Arbor.App.Extensions.ExtensionMethods;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Core.Startup;

namespace Milou.Deployer.Web.IisHost.Areas.Application
{
    [UsedImplicitly]
    public class StartupTaskModule : IModule
    {
        private readonly IApplicationAssemblyResolver _assemblyResolver;

        public StartupTaskModule(IApplicationAssemblyResolver assemblyResolver) => _assemblyResolver = assemblyResolver;

        public IServiceCollection Register(IServiceCollection builder)
        {
            IEnumerable<Type> startupTaskTypes = _assemblyResolver.GetAssemblies()
                .SelectMany(assembly => assembly.GetLoadableTypes())
                .Where(t => t.IsPublicConcreteTypeImplementing<IStartupTask>());

            foreach (Type startupTask in startupTaskTypes)
            {
                builder.AddSingleton<IStartupTask>(context => context.GetRequiredService(startupTask), this);

                if (builder.Any(serviceDescriptor => serviceDescriptor.ImplementationType == startupTask
                                                     && serviceDescriptor.ServiceType == startupTask))
                {
                    continue;
                }

                builder.AddSingleton(startupTask, this);
            }

            return builder;
        }
    }
}