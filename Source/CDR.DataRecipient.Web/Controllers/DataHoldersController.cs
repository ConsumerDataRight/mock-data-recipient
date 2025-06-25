using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.Repository;
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
            this._config = config;
            this._infosecService = infosecService;
            this._cacheManager = cacheManager;
            this._metadataService = metadataService;
            this._repository = repository;
            this._featureManager = featureManager;
        }

        [HttpGet]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index()
        {
            var model = new DataHoldersModel();
            await this.PopulateModel(model);
            return this.View(model);
        }

        [FeatureGate(nameof(Feature.AllowDataHolderRefresh))]
        [HttpPost]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index(DataHoldersModel model)
        {
            await this.GetDataHolderBrands(model);
            await this.PopulateModel(model);
            return this.View(model);
        }

        [FeatureGate(nameof(Feature.AllowDataHolderRefresh))]
        [HttpPost]
        [Route("reset/dataholderbrands")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> ResetDataHolderBrands()
        {
            await this._repository.DataHolderBrandsDelete();
            return this.Json(this.Url.Action("Index"));
        }

        private async Task GetDataHolderBrands(DataHoldersModel model)
        {
            var reg = this._config.GetRegisterConfig();
            var sp = this._config.GetSoftwareProductConfig();
            var tokenEndpoint = await this._cacheManager.GetRegisterTokenEndpoint(reg.OidcDiscoveryUri);

            // Get the access token from the Register.
            var tokenResponse = await this._infosecService.GetAccessToken(
                tokenEndpoint,
                sp.SoftwareProductId,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate);

            if (!tokenResponse.IsSuccessful)
            {
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                return;
            }

            // Using the access token, make a request to Get Data Holder Brands.
            (string respBody, System.Net.HttpStatusCode statusCode, string reason) = await this._metadataService.GetDataHolderBrands(
                reg.MtlsBaseUri,
                model.Version,
                tokenResponse.Data.AccessToken,
                sp.ClientCertificate.X509Certificate,
                sp.SoftwareProductId,
                model.Industry,
                pageSize: this._config.GetDefaultPageSize());

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
            (int inserted, int updated) = await this._repository.AggregateDataHolderBrands(dhResponse.Data);

            model.Messages = $"{statusCode}: {inserted} data holder brands added.  {updated} data holder brands updated.";
        }

        private async Task PopulateModel(DataHoldersModel model)
        {
            var reg = this._config.GetRegisterConfig();
            var allowDataHolderRefresh = await this._featureManager.IsEnabledAsync(nameof(Feature.AllowDataHolderRefresh));

            model.DataHolders = (await this._repository.GetDataHolderBrands()).OrderByMockDataHolders(allowDataHolderRefresh);

            // Populate the view
            model.RefreshRequest = new HttpRequestModel()
            {
                Method = "GET",
                Url = reg.GetDataHolderBrandsEndpoint,
                RequiresClientCertificate = true,
                RequiresAccessToken = true,
                SupportsVersion = true,
            };
            this.SetDefaults(model);
        }

        private void SetDefaults(DataHoldersModel model)
        {
            var defaultPageSize = this._config.GetDefaultPageSize();
            if (defaultPageSize.HasValue)
            {
                model.RefreshRequest.QueryParameters.Add("page-size", defaultPageSize.ToString());
            }

            if (string.IsNullOrEmpty(model.Version))
            {
                model.Version = "2";
            }
        }
    }
}
