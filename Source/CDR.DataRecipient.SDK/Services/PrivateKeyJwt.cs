using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataRecipient.SDK.Register
{
    /// <summary>
    /// Class used to generate a private_key_jwt client assertion based on a provided private key.
    /// </summary>
    public class PrivateKeyJwt
    {
        public SigningCredentials SigningCredentials { get; set; }

        /// <summary>
        /// Provide the Pkcs8 private key from X509 certificate.
        /// </summary>
        /// <param name="certFilePath">The path to the certificate.</param>
        /// <param name="pwd">The password of the certificate.</param>
        public PrivateKeyJwt(string certFilePath, string pwd) : this(new X509Certificate2(certFilePath, pwd, X509KeyStorageFlags.Exportable))
        {
        }

        /// <summary>
        /// Provide the Pkcs8 private key from X509 certificate.
        /// </summary>
        /// <param name="signingCertificate">The certificate used to sign the private key jwt.</param>
        public PrivateKeyJwt(X509Certificate2 signingCertificate)
        {
            this.SigningCredentials = new X509SigningCredentials(signingCertificate, SecurityAlgorithms.RsaSsaPssSha256);
        }

        /// <summary>
        /// Provide the private key directly.
        /// </summary>
        /// <param name="privateKey">The path to the certificate.</param>
        public PrivateKeyJwt(string privateKey)
        {
            var privateKeyBytes = Convert.FromBase64String(privateKey);
            RSA rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

            var securityKey = new RsaSecurityKey(rsa);
            this.SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSsaPssSha256);
        }

        /// <summary>
        /// Generate the private_key_jwt using the provided private key.
        /// </summary>
        /// <param name="issuer">The issuer of the JWT, usually set to the softwareProductId</param>
        /// <param name="audience">The audience of the JWT, usually set to the target token endpoint</param>
        /// <returns>A base64 encoded JWT</returns>
        public string Generate(
            string issuer, 
            string audience,
            string jti = null,
            int expiryMinutes = 10,
            string kid = null)
        {
            if (string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentException("issuer must be provided");
            }

            if (string.IsNullOrEmpty(audience))
            {
                throw new ArgumentException("audience must be provided");
            }

            if (!string.IsNullOrEmpty(kid))
            {
                this.SigningCredentials.Key.KeyId = kid;
            }

            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
            var claims = new List<Claim> 
            { 
                new Claim("sub", issuer), 
                new Claim("jti", jti ?? Guid.NewGuid().ToString()), 
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer) 
            };
            var jwt = new JwtSecurityToken(issuer, audience, claims, expires: expiry, signingCredentials: this.SigningCredentials);
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            return jwtSecurityTokenHandler.WriteToken(jwt);
        }
    }
}
