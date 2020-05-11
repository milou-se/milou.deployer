using Arbor.App.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Milou.Deployer.Web.Core.Caching;

namespace Milou.Deployer.Web.IisHost.Areas.Caching
{
    [UsedImplicitly]
    public class DistributedCacheModule : IModule
    {
        private readonly CacheSettings _cacheSettings;

        public DistributedCacheModule(CacheSettings cacheSettings)
        {
            _cacheSettings = cacheSettings;
        }

        public IServiceCollection Register(IServiceCollection builder)
        {
            if (!string.IsNullOrWhiteSpace(_cacheSettings.Host))
            {
                builder.AddStackExchangeRedisCache(options => options.Configuration = _cacheSettings.Host);
            }
            else
            {
                builder.AddDistributedMemoryCache();
            }

            return builder;
        }
    }
}