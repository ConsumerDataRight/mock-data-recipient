using System;
using System.Threading.Tasks;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Services.DataHolder;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CDR.DataRecipient.Web.Common
{
	public class DataHolderDiscoveryCache : IDataHolderDiscoveryCache
	{
		protected readonly IMemoryCache _cache;
		protected readonly IInfosecService _dhInfosecService;
		private readonly IDataHoldersRepository _dhRepository;

		public DataHolderDiscoveryCache(
			IMemoryCache cache,
			IInfosecService dhInfosecService,
			IDataHoldersRepository dhRepository)
		{
			_cache = cache;
			_dhInfosecService = dhInfosecService;
			_dhRepository = dhRepository;
		}

		public async Task<OidcDiscovery> GetOidcDiscoveryByBrandId(string dataHolderBrandId)
		{
			var key = "dh:oidc:discovery:" + dataHolderBrandId;
			var oidcDiscovery = _cache.Get<OidcDiscovery>(key);
			if (oidcDiscovery == null)
			{
				var dataHolder = await _dhRepository.GetDataHolderBrand(dataHolderBrandId);
				if (dataHolder == null)
				{
					return null;
				}

				string infosecBaseUri = dataHolder.EndpointDetail.InfoSecBaseUri;
				oidcDiscovery = (await _dhInfosecService.GetOidcDiscovery(infosecBaseUri)).Data;

				if (oidcDiscovery != null)
				{
					_cache.Set(key, oidcDiscovery, DateTimeOffset.UtcNow.AddMinutes(5));
				}
			}

			return oidcDiscovery;
		}

		public async Task<OidcDiscovery> GetOidcDiscoveryByInfoSecBaseUri(string infosecBaseUri)
		{
			var key = "dh:oidc:discovery:" + infosecBaseUri;
			var oidcDiscovery = _cache.Get<OidcDiscovery>(key);
			if (oidcDiscovery == null)
			{
				oidcDiscovery = (await _dhInfosecService.GetOidcDiscovery(infosecBaseUri)).Data;

				if (oidcDiscovery != null)
				{
					_cache.Set(key, oidcDiscovery, DateTimeOffset.UtcNow.AddMinutes(5));
				}
			}

			return oidcDiscovery;
		}
	}
}
