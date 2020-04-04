using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Arbor.App.Extensions;
using Arbor.App.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Milou.Deployer.Web.Core
{
    public static class BuilderExtensions
    {
        public static IServiceCollection RegisterAssemblyTypes<T>(
            [NotNull] this IServiceCollection serviceCollection,
            [NotNull] IEnumerable<Assembly> assemblies,
            ServiceLifetime lifetime,
            IModule? module = null) where T : class
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (assemblies is null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            if (!Enum.IsDefined(typeof(ServiceLifetime), lifetime))
            {
                throw new InvalidEnumArgumentException(nameof(lifetime), (int)lifetime, typeof(ServiceLifetime));
            }

            IEnumerable<Type> types = assemblies
                .SelectMany(assembly => assembly.GetLoadableTypes())
                .Where(type => type.IsPublicConcreteTypeImplementing<T>());

            foreach (Type type in types)
            {
                serviceCollection.Add(new ExtendedServiceDescriptor(typeof(T), type, lifetime, module?.GetType()));
            }

            return serviceCollection;
        }

        public static IServiceCollection RegisterAssemblyTypesAsSingletons<T>(
            this IServiceCollection serviceCollection,
            IEnumerable<Assembly> assemblies,
            IModule? module = null) where T : class =>
            RegisterAssemblyTypes<T>(serviceCollection, assemblies, ServiceLifetime.Singleton, module);
    }
}