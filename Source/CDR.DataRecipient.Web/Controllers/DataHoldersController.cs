using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.Register;
using CDR.DataRecipient.Web.Caching;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Features;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    [Route("data-holders")]
    public class DataHoldersController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IInfosecService _infosecService;
        private readonly ICacheManager _cacheManager;
        private readonly IMetadataService _metadataService;
        private readonly IDataHoldersRepository _repository;
        private readonly IFeatureManager _featureManager;
        
        public DataHoldersController(
            IConfiguration config,
            IInfosecService infosecService,
            ICacheManager cacheManager,
            IMetadataService metadataService,
            IDataHoldersRepository repository,
            IFeatureManager featureManager)
        {
            _config = config;
            _infosecService = infosecService;
            _cacheManager = cacheManager;
            _metadataService = metadataService;
            _repository = repository;
            _featureManager = featureManager;
        }

        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index()
        {
            var model = new DataHoldersModel();
            await PopulateModel(model);
            return View(model);
        }

        [FeatureGate(nameof(FeatureFlags.AllowDataHolderRefresh))]
        [HttpPost]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index(DataHoldersModel model)
        {
            await GetDataHolderBrands(model);
            await PopulateModel(model);
            return View(model);
        }

        private async Task GetDataHolderBrands(DataHoldersModel model)
        {
            var reg = _config.GetRegisterConfig();
            var sp = _config.GetSoftwareProductConfig();
            var tokenEndpoint = await _cacheManager.GetRegisterTokenEndpoint(reg.OidcDiscoveryUri);

            // Get the access token from the Register.
            var tokenResponse = await _infosecService.GetAccessToken(
                tokenEndpoint, 
                sp.SoftwareProductId, 
                sp.ClientCertificate.X509Certificate, 
                sp.SigningCertificate.X509Certificate,
                scope: ScopeExtensions.GetRegisterScope(model.Version, 2));

            if (!tokenResponse.IsSuccessful)
            {
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                return;
            }

            // Using the access token, make a request to Get Data Holder Brands.
            (string respBody, System.Net.HttpStatusCode statusCode, string reason) = await _metadataService.GetDataHolderBrands(
                reg.MtlsBaseUri, 
                model.Version, 
                tokenResponse.Data.AccessToken, 
                sp.ClientCertificate.X509Certificate, 
                sp.SoftwareProductId, 
                model.Industry, 
                pageSize: _config.GetDefaultPageSize());

            if (statusCode != System.Net.HttpStatusCode.OK)
            {
                model.Messages = $"{statusCode} - {reason}";
                return;
            }

            // Populate the view model
            var dhViewResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Response<IList<DataHolderBrand>>>(respBody);
            if (!dhViewResponse.IsSuccessful)
            {
                model.Messages = $"{dhViewResponse.StatusCode} - {dhViewResponse.Message}";
                return;
            }

            // Save the data holder brands
            Response<IList<DataHolderBrand>> dhResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Response<IList<DataHolderBrand>>>(respBody);
            (int inserted, int updated) = await _repository.AggregateDataHolderBrands(dhResponse.Data);

            model.Messages = $"{statusCode}: {inserted} data holder brands added.  {updated} data holder brands updated.";
        }

        [FeatureGate(nameof(FeatureFlags.AllowDataHolderRefresh))]
        [HttpPost]
        [Route("reset/dataholderbrands")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> ResetDataHolderBrands()
        {
            await _repository.DataHolderBrandsDelete();
            return Json(Url.Action("Index"));
        }

        private async Task PopulateModel(DataHoldersModel model)
        {
            var reg = _config.GetRegisterConfig();
            var allowDataHolderRefresh = await _featureManager.IsEnabledAsync(nameof(FeatureFlags.AllowDataHolderRefresh));

            model.DataHolders = (await _repository.GetDataHolderBrands()).OrderByMockDataHolders(allowDataHolderRefresh);

            // Populate the view
            model.RefreshRequest = new HttpRequestModel()
            {
                Method = "GET",
                Url = reg.GetDataHolderBrandsEndpoint,
                RequiresClientCertificate = true,
                RequiresAccessToken = true,
                SupportsVersion = true
            };
            SetDefaults(model);
        }

        private void SetDefaults(DataHoldersModel model)
        {
            var defaultPageSize = _config.GetDefaultPageSize();
            if (defaultPageSize.HasValue)
            {
                model.RefreshRequest.QueryParameters.Add("page-size", defaultPageSize.ToString());
            }

            if (string.IsNullOrEmpty(model.Version))
            {
                model.Version = "2";
            }

            if (string.IsNullOrEmpty(model.Messages))
            {
                model.Messages = "Waiting...";
            }
        }
    }
}