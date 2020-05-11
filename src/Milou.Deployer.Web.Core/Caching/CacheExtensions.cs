using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Milou.Deployer.Web.Core.Caching
{
    public static class CacheExtensions
    {
        public static async Task Set<T>(this IDistributedCache cache,
            string key,
            T item,
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
            }
            catch (OperationCanceledException ex)
            {
            }
            catch (Exception ex) { }
        }

        public static async Task<T?> Get<T>(this IDistributedCache cache,
            string key,
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
                return default;
            }
            catch (OperationCanceledException ex)
            {
                return default;
            }
            catch (Exception ex)
            {
                return default;
            }
        }
    }
}