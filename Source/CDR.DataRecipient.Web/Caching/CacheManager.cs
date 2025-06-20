using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Services.Register;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.Web.Caching
{
    public class CacheManager : ICacheManager
    {
        private readonly IMemoryCache _memCache;
        private readonly ILogger<CacheManager> _logger;
        private readonly IInfosecService _infosecService;

        public CacheManager(
            IMemoryCache memCache,
            ILogger<CacheManager> logger,
            IInfosecService infosecService)
        {
            this._memCache = memCache;
            this._logger = logger;
            this._infosecService = infosecService;
        }

        public async Task<string> GetRegisterTokenEndpoint(string oidcDiscoveryUri)
        {
            var key = $"RegisterTokenEndpoint:{oidcDiscoveryUri}";
            var item = this._memCache.Get<string>(key);
            if (!string.IsNullOrEmpty(item))
            {
                this._logger.LogInformation("Cache hit: {Key}", key);
                return item;
            }

            var tokenEndpoint = await this._infosecService.GetTokenEndpoint(oidcDiscoveryUri);

            this._memCache.Set<string>(key, tokenEndpoint);
            this._logger.LogInformation("Adding item to cache: {Key}", key);

            return tokenEndpoint;
        }
    }
}
