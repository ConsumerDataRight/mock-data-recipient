using System.Security.Cryptography.X509Certificates;
using CDR.DataRecipient.Web.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataRecipient.Web.Controllers
{
    public class JwksController : Controller
    {
        private readonly ILogger<JwksController> _logger;
        private readonly IConfiguration _config;

        public JwksController(
            IConfiguration config,
            ILogger<JwksController> logger)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        [Route("jwks")]
        public IActionResult GetJwks()
        {
            _logger.LogInformation($"Request received to {nameof(JwksController)}.{nameof(GetJwks)}");
            return Ok(GenerateJwks());
        }

        private Models.JsonWebKeySet GenerateJwks()
        {
            var sp = _config.GetSoftwareProductConfig();

            //Get credentials from certificate
            var securityKey = new X509SecurityKey(sp.SigningCertificate.X509Certificate);
            var signingCredentials = new X509SigningCredentials(sp.SigningCertificate.X509Certificate, SecurityAlgorithms.RsaSsaPssSha256);
            var encryptingCredentials = new X509EncryptingCredentials(sp.SigningCertificate.X509Certificate, SecurityAlgorithms.RsaOaepKeyWrap, SecurityAlgorithms.RsaOAEP);

            var rsaParams = signingCredentials.Certificate.GetRSAPublicKey().ExportParameters(false);
            var e = Base64UrlEncoder.Encode(rsaParams.Exponent);
            var n = Base64UrlEncoder.Encode(rsaParams.Modulus);

            var jwkSign = new Models.JsonWebKey()
            {
                alg = signingCredentials.Algorithm,
                kid = signingCredentials.Kid,
                kty = securityKey.PublicKey.KeyExchangeAlgorithm,
                n = n,
                e = e,
                use = "sig"
            };

            var jwkEnc = new Models.JsonWebKey()
            {
                alg = encryptingCredentials.Enc,
                kid = "B548C914A02787A3B5F15583C8EB030D94BC2425",
                kty = securityKey.PublicKey.KeyExchangeAlgorithm,
                n = n,
                e = e,
                use = "enc"
            };

            return new Models.JsonWebKeySet()
            {
                keys = new Models.JsonWebKey[] { jwkSign, jwkEnc }
            };
        }
    }
}
