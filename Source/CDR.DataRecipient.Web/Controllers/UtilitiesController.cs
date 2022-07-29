using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Register;
using CDR.DataRecipient.SDK.Services.Register;
using CDR.DataRecipient.Web.Caching;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    [Route("utilities")]
    public class UtilitiesController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ICacheManager _cacheManager;

        public UtilitiesController(
            IConfiguration config,
            ICacheManager cacheManager)
        {
            _config = config;
            _cacheManager = cacheManager;
        }

        [HttpGet]
        [Route("id-token")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult IdToken()
        {
            var model = new IdTokenModel();
            return View(model);
        }

        [HttpPost]
        [Route("id-token")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult IdToken(IdTokenModel model)
        {
            var sp = _config.GetSoftwareProductConfig();

            if (string.IsNullOrEmpty(model.IdTokenEncrypted))
            {
                return View(model);
            }

            try
            {
                model.IdTokenDecrypted = model.IdTokenEncrypted.DecryptIdToken(sp.SigningCertificate.X509Certificate);
                model.IdTokenClaims = model.IdTokenDecrypted.GetTokenClaims();
            }
            catch (Exception)
            {
                model.Messages = "Invalid ID Token";
            }

            return View(model);
        }

        [HttpGet]
        [Route("private-key-jwt")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> PrivateKeyJwt()
        {
            var model = new PrivateKeyJwtModel();
            await SetDefaults(model);
            return View(model);
        }

        [HttpPost]
        [Route("private-key-jwt")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult PrivateKeyJwt(PrivateKeyJwtModel model)
        {
            var privateKeyFormatted = model.PrivateKey.FormatPrivateKey();
            var privateKeyJwt = new PrivateKeyJwt(privateKeyFormatted);
            model.ClientAssertion = privateKeyJwt.Generate(model.Issuer, model.Audience, model.Jti, model.ExpiryMinutes, model.Kid);
            model.ClientAssertionClaims = model.ClientAssertion.GetTokenClaims();

            return View(model);
        }

        private async Task SetDefaults(PrivateKeyJwtModel model)
        {
            var sp = _config.GetSoftwareProductConfig();
            var reg = _config.GetRegisterConfig();
            var tokenEndpoint = await _cacheManager.GetRegisterTokenEndpoint(reg.OidcDiscoveryUri);

            model.Issuer = sp.SoftwareProductId;
            model.Audience = tokenEndpoint;
            model.PrivateKey = Constants.DEFAULT_PRIVATE_KEY;
            model.Kid = Constants.DEFAULT_KEY_ID;
            model.ExpiryMinutes = 10;
        }
    }
}
