using CDR.DataRecipient.SDK.Services.Register;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Caching
{
    public class CacheManager : ICacheManager
    {
        private readonly IDistributedCache _cache;
        private readonly IMemoryCache _memCache;
        private readonly ILogger<CacheManager> _logger;
        private readonly IInfosecService _infosecService;

        public CacheManager(
            IDistributedCache cache,
            IMemoryCache memCache,
            ILogger<CacheManager> logger,
            IInfosecService infosecService)
        {
            _cache = cache;
            _memCache = memCache;
            _logger = logger;
            _infosecService = infosecService;
        }

        public async Task<string> GetRegisterTokenEndpoint(string oidcDiscoveryUri)
        {
            var key = $"RegisterTokenEndpoint:{oidcDiscoveryUri}";
            var item = _memCache.Get<string>(key);
            if (!string.IsNullOrEmpty(item))
            {
                _logger.LogInformation("Cache hit: {key}", key);
                return item;
            }

            var tokenEndpoint = await _infosecService.GetTokenEndpoint(oidcDiscoveryUri);

            _memCache.Set<string>(key, tokenEndpoint);
            _logger.LogInformation("Adding item to cache: {key}", key);

            return tokenEndpoint;
        }
    }
}
