using System;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Services.Register;
using CDR.DataRecipient.Web.Configuration;
using CDR.DataRecipient.Web.Configuration.Models;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("ssa")]
    public class SsaController : Controller
    {
        private readonly ILogger<SsaController> _logger;
        private readonly IConfiguration _config;
        private readonly IInfosecService _infosecService;
        private readonly ISsaService _ssaService;

        public SsaController(
            IConfiguration config,
            ILogger<SsaController> logger,
            IInfosecService infosecService,
            ISsaService ssaService)
        {
            _logger = logger;
            _config = config;
            _infosecService = infosecService;
            _ssaService = ssaService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new SsaModel();
            PopulateModel(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(SsaModel model)
        {
            await GetSSA(model);
            return View(model);
        }

        private async Task GetSSA(SsaModel model)
        {
            var reg = _config.GetRegisterConfig();
            var sp = _config.GetSoftwareProductConfig();

            // Get the access token from the Register.
            var tokenResponse = await _infosecService.GetAccessToken(reg.TokenEndpoint, model.SoftwareProductId, sp.ClientCertificate.X509Certificate, sp.SigningCertificate.X509Certificate);

            if (!tokenResponse.IsSuccessful)
            {
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                PopulateModel(model);
                return;
            }

            var ssaResponse = await _ssaService.GetSoftwareStatementAssertion(reg.MtlsBaseUri, model.Version, tokenResponse.Data.AccessToken, sp.ClientCertificate.X509Certificate, model.BrandId, model.SoftwareProductId);

            model.StatusCode = ssaResponse.StatusCode;
            model.Messages = $"{ssaResponse.StatusCode} - {ssaResponse.Message}";
            model.SSA = ssaResponse.Data;
            PopulateModel(model);
        }

        private void PopulateModel(SsaModel model)
        {
            var reg = _config.GetRegisterConfig();
            var sp = _config.GetSoftwareProductConfig();

            SetDefaults(model, sp);

            model.SSARequest = new HttpRequestModel()
            {
                Method = "GET",
                RequiresAccessToken = true,
                RequiresClientCertificate = true,
                SupportsVersion = true,
                Url = string.Concat(reg.MtlsBaseUri.TrimEnd('/'), "/cdr-register/v1/banking/data-recipients/brands/{BrandId}/software-products/{SoftwareProductId}/ssa"),
            };
        }

        private void SetDefaults(SsaModel model, SoftwareProduct sp)
        {
            if (string.IsNullOrEmpty(model.BrandId))
            {
                model.BrandId = sp.BrandId;
            }

            if (string.IsNullOrEmpty(model.SoftwareProductId))
            {
                model.SoftwareProductId = sp.SoftwareProductId;
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
