using CDR.DataRecipient.SDK;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.SDK.Services.Register;
using CDR.DataRecipient.Web.Caching;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

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
            _config = config;
            _infosecService = infosecService;
            _cacheManager = cacheManager;
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
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index(SsaModel model)
        {
            await GetSSA(model);
            return View(model);
        }

        private async Task GetSSA(SsaModel model)
        {
            var reg = _config.GetRegisterConfig();
            var sp = _config.GetSoftwareProductConfig();
            var tokenEndpoint = await _cacheManager.GetRegisterTokenEndpoint(reg.OidcDiscoveryUri);

            // Get the access token from the Register.
            var tokenResponse = await _infosecService.GetAccessToken(
                tokenEndpoint, 
                model.SoftwareProductId, 
                sp.ClientCertificate.X509Certificate, 
                sp.SigningCertificate.X509Certificate,
                scope: ScopeExtensions.GetRegisterScope(model.Version, 3));

            if (!tokenResponse.IsSuccessful)
            {
                model.Messages = $"{tokenResponse.StatusCode} - {tokenResponse.Message}";
                PopulateModel(model);
                return;
            }

            var ssaResponse = await _ssaService.GetSoftwareStatementAssertion(
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
            PopulateModel(model);
        }

        private void PopulateModel(SsaModel model)
        {
            var reg = _config.GetRegisterConfig();
            var sp = _config.GetSoftwareProductConfig();

            SetDefaults(model, sp);

            // Populate the view
            model.SSARequest = new HttpRequestModel()
            {
                Method = "GET",
                RequiresAccessToken = true,
                RequiresClientCertificate = true,
                SupportsVersion = true,
                Url = reg.GetSsaEndpoint
            };
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
    }
}
