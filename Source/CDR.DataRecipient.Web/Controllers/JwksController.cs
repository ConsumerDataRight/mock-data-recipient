using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("")]
    public class JwksController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwksController> _logger;
        private readonly IMemoryCache _cache;

        public JwksController(
            IConfiguration config,
            ILogger<JwksController> logger,
            IMemoryCache cache)
        {
            this._config = config;
            this._logger = logger;
            this._cache = cache;
        }

        [HttpGet]
        [Route("jwks/{id:int?}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult GetJwks(int? id = 1)
        {
            return this.Ok(this.GenerateJwks(id, false));
        }

        [HttpGet]
        [Route("jwks-with-private-keys/{id:int?}")]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult GetJwksPrivateKeys(int? id = 1)
        {
            return this.Ok(this.GenerateJwks(id, true));
        }

        /// <summary>
        /// Generate a JWKS for the DR.
        /// </summary>
        /// <param name="id">ID to control the certificate to use to generate the JWKS.  Can be 1 or 2.</param>
        /// <param name="includePrivateKeyDetails">Whether private key details should be included in the JWKS.</param>
        /// <returns>JsonWebKeySet.</returns>
        /// <remarks>
        /// In a production scenario the private key details would never be included in the output.
        /// However, for FAPI testing the private key is required to be included in the JWKS when configuring the test plan.
        /// Therefore, set this flag to generate the JWKS with the private key that can then be included in the FAPI configuration.
        /// </remarks>
        private SDK.Models.JsonWebKeySet GenerateJwks(int? id = 1, bool includePrivateKeyDetails = false)
        {
            this._logger.LogInformation($"{nameof(JwksController)}.{nameof(this.GenerateJwks)}");

            string cacheKey = $"jwks-{id}-{includePrivateKeyDetails}";
            var item = this._cache.Get<SDK.Models.JsonWebKeySet>(cacheKey);

            if (item != null)
            {
                this._logger.LogInformation("Cache hit: {CacheKey}", cacheKey);
                return item;
            }

            var cert = this.GetCertificate(id.Value);

            // Get credentials from certificate
            var securityKey = new X509SecurityKey(cert);
            var signingCredentials = new X509SigningCredentials(cert, SecurityAlgorithms.RsaSsaPssSha256);
            var encryptionCredentials = cert.GetEncryptionCredentials();

            var rsaParams = signingCredentials.Certificate.GetRSAPublicKey().ExportParameters(false);
            var e = Base64UrlEncoder.Encode(rsaParams.Exponent);
            var n = Base64UrlEncoder.Encode(rsaParams.Modulus);

            // Create JWKs for sig and enc purposes.
            // Make sure the kid is different for each key. FAPI 1.0 - kid needs to be unique id within the keyset.
            var jwkSign = new SDK.Models.JsonWebKey()
            {
                Alg = signingCredentials.Algorithm,
                Kid = signingCredentials.Kid,
                Kty = securityKey.PublicKey.KeyExchangeAlgorithm,
                N = n,
                E = e,
                Use = "sig",
            };
            var jwkEncList = encryptionCredentials.Keys.Select(key =>
            {
                var credential = encryptionCredentials[key];
                return new SDK.Models.JsonWebKey()
                {
                    Alg = credential.Enc,
                    Kid = key,
                    Kty = securityKey.PublicKey.KeyExchangeAlgorithm,
                    N = n,
                    E = e,
                    Use = "enc",
                };
            });

            if (includePrivateKeyDetails)
            {
                var privateKey = new RsaSecurityKey(cert.GetRSAPrivateKey());
                var jwkPrivate = JsonWebKeyConverter.ConvertFromRSASecurityKey(privateKey);

                jwkSign.D = jwkPrivate.D;
                foreach (var jwk in jwkEncList)
                {
                    jwk.D = jwkPrivate.D;
                }
            }

            var jwks = new List<SDK.Models.JsonWebKey>();
            jwks.Add(jwkSign);
            jwks.AddRange(jwkEncList);
            var keySet = new SDK.Models.JsonWebKeySet() { Keys = jwks.ToArray() };

            // Add the key Set to the cache.
            this._cache.Set<SDK.Models.JsonWebKeySet>(cacheKey, keySet);
            this._logger.LogInformation("JWKS added to cache");

            return keySet;
        }

        /// <summary>
        /// Retrieve the certificate to use to generate the jwks.
        /// </summary>
        /// <param name="id">Provides the ability to switch to an alternative certificate.</param>
        /// <returns>X509Certificate2.</returns>
        /// <remarks>
        /// Providing the ability to switch to a different certificate allows 2 jwks to be generated from the one data recipient.
        /// This is not what would be used in a production scenario, however this is useful to be able to perform FAPI testing using the DR.
        /// For FAPI testing, 2 clients are required.  The first client can be configured to use the /jwks endpoint, whilst the second client
        /// can be configured to use the secondary /jwks/2 endpoint.
        /// In this way a data holder can test their FAPI conformance using the Mock Data Recipient.
        /// </remarks>
        private X509Certificate2 GetCertificate(int id)
        {
            var sp = this._config.GetSoftwareProductConfig();

            if (id == 2)
            {
                return sp.ClientCertificate.X509Certificate;
            }

            return sp.SigningCertificate.X509Certificate;
        }
    }
}
