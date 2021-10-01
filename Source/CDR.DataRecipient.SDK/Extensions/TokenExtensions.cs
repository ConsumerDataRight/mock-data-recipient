using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Jose;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataRecipient.SDK.Extensions
{
    public static class TokenExtensions
    {
        public static JwtSecurityToken GetJwt(this string token)
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            if (!string.IsNullOrEmpty(token) && tokenHandler.CanReadToken(token))
            {
                return tokenHandler.ReadJwtToken(token);
            }

            return null;
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
                notBefore: null
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
