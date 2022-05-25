using System;
using System.Threading.Tasks;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.Web.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.Web.Common
{
    public class DataHolderDiscoveryCache : IDataHolderDiscoveryCache
    {
        protected readonly IDistributedCache _cache;
        protected readonly IInfosecService _dhInfosecService;
        private readonly IDataHoldersRepository _dhRepository;
        private readonly ILogger<DataHolderDiscoveryCache> _logger;

        public DataHolderDiscoveryCache(
            IDistributedCache cache,
            IInfosecService dhInfosecService,
            IDataHoldersRepository dhRepository,
            ILogger<DataHolderDiscoveryCache> logger)
        {
            _cache = cache;
            _dhInfosecService = dhInfosecService;
            _dhRepository = dhRepository;
            _logger = logger;
        }

        public async Task<OidcDiscovery> GetOidcDiscoveryByBrandId(string dataHolderBrandId)
        {
            var key = "dh:oidc:discovery:" + dataHolderBrandId;
            var oidcDiscovery = await _cache.GetAsync<OidcDiscovery>(key);
            if (oidcDiscovery == null)
            {
                _logger.LogDebug("Discovery document for {dataHolderBrandId} not found in cache.  Retrieving...", dataHolderBrandId);

                var dataHolder = await _dhRepository.GetDataHolderBrand(dataHolderBrandId);
                if (dataHolder == null)
                {
                    _logger.LogError("Data Holder {dataHolderBrandId} not found.", dataHolderBrandId);
                    return null;
                }

                string infosecBaseUri = dataHolder.EndpointDetail.InfoSecBaseUri;
                oidcDiscovery = (await _dhInfosecService.GetOidcDiscovery(infosecBaseUri)).Data;
                if (oidcDiscovery != null)
                {
                    _logger.LogDebug("Data Holder {dataHolderBrandId} discovery document added to cache.", dataHolderBrandId);
                    await _cache.SetAsync(key, oidcDiscovery, DateTimeOffset.UtcNow.AddMinutes(5));
                }
            }
            return oidcDiscovery;
        }

        public async Task<OidcDiscovery> GetOidcDiscoveryByInfoSecBaseUri(string infosecBaseUri)
        {
            var key = "dh:oidc:discovery:" + infosecBaseUri;
            var oidcDiscovery = await _cache.GetAsync<OidcDiscovery>(key);
            if (oidcDiscovery == null)
            {
                _logger.LogDebug("Discovery document for {infosecBaseUri} not found in cache.  Retrieving...", infosecBaseUri);

                oidcDiscovery = (await _dhInfosecService.GetOidcDiscovery(infosecBaseUri)).Data;
                if (oidcDiscovery != null)
                {
                    _logger.LogDebug("Discovery document for {infosecBaseUri} added to cache.", infosecBaseUri);
                    await _cache.SetAsync(key, oidcDiscovery, DateTimeOffset.UtcNow.AddMinutes(5));
                }
            }
            return oidcDiscovery;
        }
    }
}