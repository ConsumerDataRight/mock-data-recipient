using CDR.DataRecipient.SDK.Register;
using CDR.DataRecipient.Web.Caching;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ICacheManager _cacheManager;

        public ApiController(
            IConfiguration config,
            ICacheManager cacheManager)
        {
            _config = config;
            _cacheManager = cacheManager;
        }

        [Route("generate-client-assertion")]
        public async Task<string> GenerateClientAssertion(
            [FromQuery] string iss = null,
            [FromQuery] string aud = null,
            [FromQuery] string jti = null,
            [FromQuery] string kid = null,
            [FromQuery] int? exp = null)
        {
            var sp = _config.GetSoftwareProductConfig();
            var reg = _config.GetRegisterConfig();
            var privateKeyFormatted = Constants.DEFAULT_PRIVATE_KEY.FormatPrivateKey();
            var privateKeyJwt = new PrivateKeyJwt(privateKeyFormatted);

            return privateKeyJwt.Generate(
                iss ?? sp.SoftwareProductId, 
                aud ?? await _cacheManager.GetRegisterTokenEndpoint(reg.OidcDiscoveryUri), 
                jti ?? System.Guid.NewGuid().ToString(),
                exp ?? 10, 
                kid ?? Constants.DEFAULT_KEY_ID);
        }

    }
}
