using Jose;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CDR.DataRecipient.SDK.Extensions
{
    public static class TokenExtensions
    {
        public static async Task<(bool IsValid, JwtSecurityToken ValidatedToken, ClaimsPrincipal ClaimsPrincipal)> ValidateToken(
            this string jwt,
            string jwksUri,
            ILogger logger,
            string validIssuer = null,
            string[] validAudiences = null,
            int clockSkewMins = 2,
            bool validateLifetime = true,
            bool acceptAnyServerCertificate = false,
            bool enforceHttpsEndpoint = false)
        {
            var jwks = await jwksUri.GetJwks(acceptAnyServerCertificate, enforceHttpsEndpoint);
            if (jwks == null || jwks.Keys.Count == 0)
            {
                logger.LogDebug("Keys not found in JWKS: {jwksUri}", jwksUri);
                return (false, null, null);
            }

            logger.LogDebug("Keys found in JWKS: {keys}", string.Join(',', jwks.Keys.Select(k => k.Kid).ToArray()));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.FromMinutes(clockSkewMins),
                IssuerSigningKeys = jwks.Keys,
                ValidateIssuerSigningKey = true,
                ValidIssuer = validIssuer,
                ValidateIssuer = !string.IsNullOrEmpty(validIssuer),
                ValidAudiences = validAudiences,
                ValidateAudience = validAudiences != null && validAudiences.Any(),
                RequireSignedTokens = true,
                ValidateLifetime = validateLifetime,
            };

            try
            {
                logger.LogDebug("Validating token: {jwt}", jwt);
                var handler = new JwtSecurityTokenHandler();
                var claimsPrincipal = handler.ValidateToken(jwt, tokenValidationParameters, out var token);
                return (true, token as JwtSecurityToken, claimsPrincipal);
            }
            catch (Exception ex)
            {
                // implement logging.
                logger.LogError(ex, "An error occurred validating the JWT");
            }

            return (false, null, null);
        }

        public static JwtSecurityToken GetJwt(this string token)
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (!string.IsNullOrEmpty(token) && tokenHandler.CanReadToken(token))
            {
                return tokenHandler.ReadJwtToken(token);
            }

            return null;
        }

        public static async Task<JsonWebKeySet> GetJwks(
            this string jwksEndpoint,
            bool acceptAnyServerCertificate = false,
            bool enforceHttpsEndpoint = false)
        {
            var clientHandler = new HttpClientHandler();
            if (acceptAnyServerCertificate)
            {
                clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            var jwksClient = new HttpClient(clientHandler);
            var jwksResponse = await jwksClient.GetAsync(jwksEndpoint.ValidateEndpoint(enforceHttpsEndpoint));
            return new JsonWebKeySet(await jwksResponse.Content.ReadAsStringAsync());
        }

        public static string GenerateJwt(
            this IDictionary<string, object> claims,
            string issuer,
            string audience,
            X509Certificate2 signingCertificate,
            string signingAlgorithm = SecurityAlgorithms.RsaSsaPssSha256,
            int expirySeconds = 300)
        {
            var jwtHeader = new JwtHeader(new X509SigningCredentials(signingCertificate, signingAlgorithm));

            var jwtPayload = new JwtPayload(
                issuer: issuer,
                audience: audience,
                claims: null,
                claimsCollection: claims,
                expires: DateTime.UtcNow.AddSeconds(expirySeconds),
                issuedAt: DateTime.UtcNow,
                notBefore: DateTime.UtcNow
                );

            var jwt = new JwtSecurityToken(jwtHeader, jwtPayload);
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(jwt);
        }

        public static string DecryptIdToken(this string idToken, X509Certificate2 privateKeyCertificate)
        {
            var privateKey = privateKeyCertificate.GetRSAPrivateKey();
            JweToken token = JWE.Decrypt(idToken, privateKey);
            return token.Plaintext;
        }

        public static IEnumerable<Claim> GetTokenClaims(this string token)
        {
            var jwt = GetJwt(token);
            return jwt.Payload.Claims;
        }
    }
}