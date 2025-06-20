using System;
using System.Threading.Tasks;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Features;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CDR.DataRecipient.Web.Common
{
    public class DataHolderDiscoveryCache : IDataHolderDiscoveryCache
    {
        private readonly IDistributedCache _cache;
        private readonly IInfosecService _dhInfosecService;
        private readonly ILogger<DataHolderDiscoveryCache> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public DataHolderDiscoveryCache(
            IDistributedCache cache,
            IInfosecService dhInfosecService,
            ILogger<DataHolderDiscoveryCache> logger,
            IServiceScopeFactory scopeFactory)
        {
            this._cache = cache;
            this._dhInfosecService = dhInfosecService;
            this._scopeFactory = scopeFactory;
            this._logger = logger;
        }

        public async Task<OidcDiscovery> GetOidcDiscoveryByBrandId(string dataHolderBrandId)
        {
            var key = "dh:oidc:discovery:" + dataHolderBrandId;
            var oidcDiscovery = await this._cache.GetAsync<OidcDiscovery>(key);
            if (oidcDiscovery == null)
            {
                this._logger.LogDebug("Discovery document for {DataHolderBrandId} not found in cache.  Retrieving...", dataHolderBrandId);

                using var scope = this._scopeFactory.CreateScope();
                var dhRepository = scope.ServiceProvider.GetRequiredService<IDataHoldersRepository>();

                DataHolderBrand dataHolder;
                dataHolder = await dhRepository.GetDataHolderBrand(dataHolderBrandId);

                if (dataHolder == null)
                {
                    this._logger.LogError("Data Holder {DataHolderBrandId} not found.", dataHolderBrandId);
                    return null;
                }

                string infosecBaseUri = dataHolder.EndpointDetail.InfoSecBaseUri;

                oidcDiscovery = (await this._dhInfosecService.GetOidcDiscovery(infosecBaseUri)).Data;
                if (oidcDiscovery != null)
                {
                    this._logger.LogDebug("Data Holder {DataHolderBrandId} discovery document added to cache.", dataHolderBrandId);
                    await this._cache.SetAsync(key, oidcDiscovery, DateTimeOffset.UtcNow.AddMinutes(5));
                }
            }

            return oidcDiscovery;
        }

        public async Task<OidcDiscovery> GetOidcDiscoveryByInfoSecBaseUri(string infosecBaseUri)
        {
            var key = "dh:oidc:discovery:" + infosecBaseUri;
            var oidcDiscovery = await this._cache.GetAsync<OidcDiscovery>(key);
            if (oidcDiscovery == null)
            {
                this._logger.LogDebug("Discovery document for {InfosecBaseUri} not found in cache.  Retrieving...", infosecBaseUri);

                oidcDiscovery = (await this._dhInfosecService.GetOidcDiscovery(infosecBaseUri)).Data;
                if (oidcDiscovery != null)
                {
                    this._logger.LogDebug("Discovery document for {InfosecBaseUri} added to cache.", infosecBaseUri);
                    await this._cache.SetAsync(key, oidcDiscovery, DateTimeOffset.UtcNow.AddMinutes(5));
                }
            }

            return oidcDiscovery;
        }
    }
}
