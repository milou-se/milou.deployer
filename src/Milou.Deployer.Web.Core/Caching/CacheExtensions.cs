using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;

namespace Milou.Deployer.Web.Core.Caching
{
    public static class CacheExtensions
    {
        public static async Task Set<T>(this IDistributedCache cache,
            string key,
            T item,
            ILogger? logger = default,
            CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var json = JsonSerializer.Serialize(item);

                var options = new DistributedCacheEntryOptions();

                await cache.SetStringAsync(key, json, options, cancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                logger?.Debug(ex, "Cache timed out");
            }
            catch (OperationCanceledException ex)
            {
                logger?.Debug(ex, "Cache timed out");
            }
            catch (Exception ex)
            {
                logger?.Debug(ex, "Could not set cache");
            }
        }

        public static async Task<T?> Get<T>(this IDistributedCache cache,
            string key,
            ILogger? logger = default,
            CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var json = await cache.GetStringAsync(key, cancellationToken);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return default;
                }

                var item = JsonSerializer.Deserialize<T>(json);

                return item;
            }
            catch (TaskCanceledException ex)
            {
                logger?.Debug(ex, "Cache timed out");
                return default;
            }
            catch (OperationCanceledException ex)
            {
                logger?.Debug(ex, "Cache timed out");
                return default;
            }
            catch (Exception ex)
            {
                logger?.Debug(ex, "Could not get cache");
                return default;
            }
        }
    }
}