using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Jose;

#nullable enable

namespace CDR.DataRecipient.IntegrationTests.Infrastructure
{
    static public class JWT2
    {
        static public string CreateJWT(string certificateFilename, string certificatePassword, Dictionary<string, object> subject, string kid)
        {
            var payload = JsonConvert.SerializeObject(subject);

            return CreateJWT(certificateFilename, certificatePassword, payload, kid);
        }

        static public string CreateJWT(string certificateFilename, string certificatePassword, string payload, string kid)
        {
            var jwtHeader = new Dictionary<string, object>()
            {
                { JwtHeaderParameterNames.Alg, "PS256" },
                { JwtHeaderParameterNames.Typ, "JWT" },
                { JwtHeaderParameterNames.Kid, kid },
            };

            var cert = new X509Certificate2(certificateFilename, certificatePassword);

            var jwt = Jose.JWT.Encode(payload, cert.GetRSAPrivateKey(), JwsAlgorithm.PS256, jwtHeader);

            return jwt;
        }
    }

    static public class JWT
    {
        public enum SecurityAlgorithm { HmacSha256, RsaSsaPssSha256 };

        /// <summary>
        /// Create a JWT
        /// </summary>
        /// <param name="certificateFilename">Filename of certificate used to sign the token</param>
        /// <param name="certificatePassword">Password of certificate used to sign the token</param>
        /// <param name="issuer">iss</param>
        /// <param name="audience">aud</param>
        /// <param name="Expires">exp</param>
        /// <param name="subject">Claims to assert in the token</param>
        /// <returns></returns>
        static public string CreateJWT(
            string certificateFilename,
            string certificatePassword,
            string? issuer,
            string? audience,
            bool expired,
            Dictionary<string, object> subject,
            SecurityAlgorithm securityAlgorithm
        )
        {
            var keyBytes = GetRSAPrivateKeyBytes(certificateFilename, certificatePassword);

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(GetSecurityTokenDescriptor(keyBytes, issuer, audience, expired, subject, securityAlgorithm));

            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Extract key bytes from certificate
        /// </summary>
        static private byte[] GetRSAPrivateKeyBytes(string certificateFilename, string certificatePassword)
        {
            var certificate = new X509Certificate2(certificateFilename, certificatePassword, X509KeyStorageFlags.Exportable);

            var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new Exception($"{nameof(JWT)}.{nameof(CreateJWT)}.{nameof(GetRSAPrivateKeyBytes)} - Certificate has no private key");
            }

            var keyBytes = rsa.ExportPkcs8PrivateKey();

            return keyBytes;
        }

        /// <summary>
        /// Get a security token descriptor
        /// </summary>
        static private SecurityTokenDescriptor GetSecurityTokenDescriptor(
            byte[] keyBytes, 
            string? issuer, string? audience, 
            bool expired,
            Dictionary<string, object> claims,
            SecurityAlgorithm securityAlgorithm)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                Audience = audience
            };

            if (expired)
            {
                // Can't set back-dated tokenDescriptor.Expires, so lets just issue the token as at yesterday, with a 1 second expiry
                var utcNow = DateTime.UtcNow.AddDays(-1);
                tokenDescriptor.IssuedAt = utcNow;
                tokenDescriptor.NotBefore = utcNow;
                tokenDescriptor.Expires = utcNow.AddSeconds(1);
            }

            tokenDescriptor.Claims = claims;

            // Signing credentials
            tokenDescriptor.SigningCredentials = securityAlgorithm switch
            {
                // SecurityAlgorithm.HmacSha256 => new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256),
                SecurityAlgorithm.HmacSha256 => new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature),

                // SecurityAlgorithm.RsaSsaPssSha256 => new SigningCredentials(GetRsaSecurityKey(keyBytes), SecurityAlgorithms.RsaSsaPssSha256),
                SecurityAlgorithm.RsaSsaPssSha256 => new SigningCredentials(GetRsaSecurityKey(keyBytes), SecurityAlgorithms.RsaSsaPssSha256Signature),

                _ => throw new ArgumentOutOfRangeException(nameof(securityAlgorithm))
            };

            return tokenDescriptor;
        }

        /// <summary>
        /// Get a RSA security key for key bytes
        /// </summary>
        static private RsaSecurityKey GetRsaSecurityKey(byte[] keyBytes)
        {
            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            //rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            //var rsaSecurityKey = new RsaSecurityKey(rsa);
            //var key1 = CngKey.Import(keyBytes, CngKeyBlobFormat.Pkcs8PrivateBlob);
            //var rsa = new RSACng(key1);

            var kid = GetKeyId(rsa);
            var rsaSecurityKey = new RsaSecurityKey(rsa)
            {
                KeyId = kid,
                CryptoProviderFactory = new CryptoProviderFactory()
                {
                    CacheSignatureProviders = false
                }
            };

            return rsaSecurityKey;
        }

        /// <summary>
        /// Get key id for RSA
        /// </summary>
        static private string GetKeyId(RSA rsa)
        {
            var rsaParameters = rsa.ExportParameters(false);

            var e = Base64UrlEncoder.Encode(rsaParameters.Exponent);
            var n = Base64UrlEncoder.Encode(rsaParameters.Modulus);
            var dict = new Dictionary<string, string>()
            {
                { "e", e },
                { "kty", "RSA" },
                { "n", n}
            };
            var hash = SHA256.Create();
            var hashBytes = hash.ComputeHash(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dict)));
            return Base64UrlEncoder.Encode(hashBytes);
        }
    }
}