using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Controllers
{
    public class JwksController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwksController> _logger;
        private readonly IMemoryCache _cache;

        public JwksController(
            IConfiguration config,
            ILogger<JwksController> logger,
            IMemoryCache cache)
        {
            _config = config;
            _logger = logger;
            _cache = cache;
        }

        [HttpGet]
        [Route("jwks/{id:int?}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetJwks(int? id = 1)
        {
            return Ok(await GenerateJwks(id, false));
        }

        [HttpGet]
        [Route("jwks-with-private-keys/{id:int?}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> GetJwksPrivateKeys(int? id = 1)
        {
            return Ok(await GenerateJwks(id, true));
        }

        /// <summary>
        /// Generate a JWKS for the DR.
        /// </summary>
        /// <param name="id">ID to control the certificate to use to generate the JWKS.  Can be 1 or 2.</param>
        /// <param name="includePrivateKeyDetails">Whether private key details should be included in the JWKS.</param>
        /// <returns>JsonWebKeySet</returns>
        /// <remarks>
        /// In a production scenario the private key details would never be included in the output.
        /// However, for FAPI testing the private key is required to be included in the JWKS when configuring the test plan.
        /// Therefore, set this flag to generate the JWKS with the private key that can then be included in the FAPI configuration.
        /// </remarks>
        private async Task<SDK.Models.JsonWebKeySet> GenerateJwks(int? id = 1, bool includePrivateKeyDetails = false)
        {
            _logger.LogInformation($"{nameof(JwksController)}.{nameof(GenerateJwks)}");

            string cacheKey = $"jwks-{id}-{includePrivateKeyDetails}";
            var item = _cache.Get<SDK.Models.JsonWebKeySet>(cacheKey);

            if (item != null)
            {
                _logger.LogInformation("Cache hit: {cacheKey}", cacheKey);
                return item;
            }

            var cert = GetCertificate(id.Value);

            // Get credentials from certificate
            var securityKey = new X509SecurityKey(cert);
            var signingCredentials = new X509SigningCredentials(cert, SecurityAlgorithms.RsaSsaPssSha256);
            var encryptingCredentials = new X509EncryptingCredentials(cert, SecurityAlgorithms.RsaOaepKeyWrap, SecurityAlgorithms.RsaOAEP);

            var rsaParams = signingCredentials.Certificate.GetRSAPublicKey().ExportParameters(false);
            var e = Base64UrlEncoder.Encode(rsaParams.Exponent);
            var n = Base64UrlEncoder.Encode(rsaParams.Modulus);

            var jwkSign = new SDK.Models.JsonWebKey()
            {
                alg = signingCredentials.Algorithm,
                kid = signingCredentials.Kid,
                kty = securityKey.PublicKey.KeyExchangeAlgorithm,
                n = n,
                e = e,
                use = "sig"
            };

            var jwkEnc = new SDK.Models.JsonWebKey()
            {
                alg = encryptingCredentials.Enc,
                kid = encryptingCredentials.Key.KeyId.Sha256(), // FAPI 1.0 - kid needs to be unique id within the keyset.
                kty = securityKey.PublicKey.KeyExchangeAlgorithm,
                n = n,
                e = e,
                use = "enc"
            };

            if (includePrivateKeyDetails)
            {
                var privateKey = new RsaSecurityKey(cert.GetRSAPrivateKey());
                var jwkPrivate = JsonWebKeyConverter.ConvertFromRSASecurityKey(privateKey);

                jwkSign.d = jwkPrivate.D;
                jwkEnc.d = jwkPrivate.D;
            }

            var jwks = new SDK.Models.JsonWebKeySet()
            {
                keys = new SDK.Models.JsonWebKey[] { jwkSign, jwkEnc }
            };

            // Add the jwks to the cache.
            _cache.Set<SDK.Models.JsonWebKeySet>(cacheKey, jwks);
            _logger.LogInformation("JWKS added to cache");

            return jwks;
        }

        /// <summary>
        /// Retrieve the certificate to use to generate the jwks.
        /// </summary>
        /// <param name="id">Provides the ability to switch to an alternative certificate</param>
        /// <returns>X509Certificate2</returns>
        /// <remarks>
        /// Providing the ability to switch to a different certificate allows 2 jwks to be generated from the one data recipient.
        /// This is not what would be used in a production scenario, however this is useful to be able to perform FAPI testing using the DR.
        /// For FAPI testing, 2 clients are required.  The first client can be configured to use the /jwks endpoint, whilst the second client
        /// can be configured to use the secondary /jwks/2 endpoint.
        /// In this way a data holder can test their FAPI conformance using the Mock Data Recipient.
        /// </remarks>
        private X509Certificate2 GetCertificate(int id)
        {
            var sp = _config.GetSoftwareProductConfig();

            if (id == 2)
            {
                return sp.ClientCertificate.X509Certificate;
            }

            return sp.SigningCertificate.X509Certificate;
        }
    }
}
