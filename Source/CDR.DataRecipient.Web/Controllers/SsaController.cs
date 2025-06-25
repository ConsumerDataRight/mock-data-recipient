using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.Register;
using CDR.DataRecipient.Web.Caching;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    [Route("ssa")]
    public class SsaController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IInfosecService _infosecService;
        private readonly ISsaService _ssaService;
        private readonly ICacheManager _cacheManager;

        public SsaController(
            IConfiguration config,
            IInfosecService infosecService,
            ICacheManager cacheManager,
            ISsaService ssaService)
        {
            this._config = config;
            this._infosecService = infosecService;
            this._cacheManager = cacheManager;
            this._ssaService = ssaService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new SsaModel();
            this.PopulateModel(model);
            return this.View(model);
        }

        [HttpPost]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index(SsaModel model)
        {
            await this.GetSSA(model);
            return this.View(model);
        }

        private static void SetDefaults(SsaModel model, SoftwareProduct sp)
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
                model.Version = "3";
            }

            if (string.IsNullOrEmpty(model.Messages))
            {
                model.Messages = "Waiting...";
            }
        }

        private async Task GetSSA(SsaModel model)
        {
            var reg = this._config.GetRegisterConfig();
            var sp = this._config.GetSoftwareProductConfig();
            var tokenEndpoint = await this._cacheManager.GetRegisterTokenEndpoint(reg.OidcDiscoveryUri);

            // Get the access token from the Register.
            var tokenResponse = await this._infosecService.GetAccessToken(
                tokenEndpoint,
                model.SoftwareProductId,
                sp.ClientCertificate.X509Certificate,
                sp.SigningCertificate.X509Certificate);

            if (!tokenResponse.IsSuccessful)
            {
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                this.PopulateModel(model);
                return;
            }

            var ssaResponse = await this._ssaService.GetSoftwareStatementAssertion(
                reg.MtlsBaseUri,
                model.Version,
                tokenResponse.Data.AccessToken,
                sp.ClientCertificate.X509Certificate,
                model.BrandId,
                model.SoftwareProductId,
                model.Industry);

            model.StatusCode = ssaResponse.StatusCode;
            model.Messages = $"{ssaResponse.StatusCode} - {ssaResponse.Message}";
            model.SSA = ssaResponse.Data;
            this.PopulateModel(model);
        }

        private void PopulateModel(SsaModel model)
        {
            var reg = this._config.GetRegisterConfig();
            var sp = this._config.GetSoftwareProductConfig();

            SetDefaults(model, sp);

            // Populate the view
            model.SSARequest = new HttpRequestModel()
            {
                Method = "GET",
                RequiresAccessToken = true,
                RequiresClientCertificate = true,
                SupportsVersion = true,
                Url = reg.GetSsaEndpoint,
            };
        }
    }
}
