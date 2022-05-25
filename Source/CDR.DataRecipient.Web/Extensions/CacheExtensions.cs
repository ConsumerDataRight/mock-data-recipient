using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Extensions
{
    public static class CacheExtensions
    {
        public async static Task<T> GetAsync<T>(this IDistributedCache cache, string key) where T : class
        {
            var bytes = await cache.GetAsync(key);
            if (bytes == null)
            {
                return null;
            }

            return bytes.FromByteArray<T>();
        }

        public async static Task SetAsync<T>(this IDistributedCache cache, string key, T data, DateTimeOffset absoluteExpiry) where T : class
        {
            await cache.SetAsync(key, data.ToByteArray(), new DistributedCacheEntryOptions() { AbsoluteExpiration = absoluteExpiry });
        }
    }
}
