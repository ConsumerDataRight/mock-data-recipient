using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Register;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CDR.DataRecipient.Web.Configuration;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Services.Register;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("data-holders")]
    public class DataHoldersController : Controller
    {
        private readonly ILogger<DataHoldersController> _logger;
        private readonly IConfiguration _config;
        private readonly IInfosecService _infosecService;
        private readonly IMetadataService _metadataService;
        private readonly IDataHoldersRepository _repository;
        
        public DataHoldersController(
            IConfiguration config, 
            ILogger<DataHoldersController> logger,
            IDataHoldersRepository repository,
            IInfosecService infosecService,
            IMetadataService metadataService)
        {
            _logger = logger;
            _config = config;
            _repository = repository;
            _infosecService = infosecService;
            _metadataService = metadataService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation($"GET request: {nameof(DataHoldersController)}.{nameof(Index)}");

            var model = new DataHoldersModel();
            await PopulateModel(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(DataHoldersModel model)
        {
            _logger.LogInformation($"POST request: {nameof(DataHoldersController)}.{nameof(Index)}");

            await GetDataHolderBrands(model);
            return View(model);
        }

        private async Task PopulateModel(DataHoldersModel model)
        {
            var reg = _config.GetRegisterConfig();

            model.DataHolders = await _repository.GetDataHolderBrands();

            model.RefreshRequest = new HttpRequestModel()
            {
                Method = "GET",
                Url = reg.GetDataHolderBrandsEndpoint,
                RequiresClientCertificate = true,
                RequiresAccessToken = true,
                SupportsVersion = true,
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
                model.Version = "1";
            }

            if (string.IsNullOrEmpty(model.Messages))
            {
                model.Messages = "Waiting...";
            }
        }

        private async Task<DataHoldersModel> GetDataHolderBrands(DataHoldersModel model)
        {
            var reg = _config.GetRegisterConfig();
            var sp = _config.GetSoftwareProductConfig();

            // Get the access token from the Register.
            var tokenResponse = await _infosecService.GetAccessToken(reg.TokenEndpoint, sp.SoftwareProductId, sp.ClientCertificate.X509Certificate, sp.SigningCertificate.X509Certificate);

            if (!tokenResponse.IsSuccessful)
            {
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                await PopulateModel(model);
                return model;
            }

            // Using the access token, make a request to Get Data Holder Brands.
            var dhResponse = await _metadataService.GetDataHolderBrands(reg.MtlsBaseUri, model.Version, tokenResponse.Data.AccessToken, sp.ClientCertificate.X509Certificate, sp.SoftwareProductId, pageSize: _config.GetDefaultPageSize());

            if (dhResponse.IsSuccessful)
            {
                model.Messages = $"{dhResponse.StatusCode} - {dhResponse.Data.Count} data holder brands loaded.";

                // Save the data holder brands.
                await _repository.PersistDataHolderBrands(dhResponse.Data);
            }
            else
            {
                model.Messages = $"{dhResponse.StatusCode} - {dhResponse.Message}";
            }

            await PopulateModel(model);
            return model;
        }
    }
}
