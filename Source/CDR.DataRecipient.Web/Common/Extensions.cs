using CDR.DataRecipient.SDK.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace CDR.DataRecipient.Web.Common
{
    public static class Extensions
    {
        public static IDictionary<string, X509EncryptingCredentials> GetEncryptionCredentials(this X509Certificate2 cert)
        {
            Dictionary<string, X509EncryptingCredentials> credentials = new Dictionary<string, X509EncryptingCredentials>();
            var encryptingCredentials_oaep = new X509EncryptingCredentials(cert, SecurityAlgorithms.RsaOaepKeyWrap, SecurityAlgorithms.RsaOAEP);
            credentials.Add(encryptingCredentials_oaep.Key.KeyId.Sha256(), encryptingCredentials_oaep);

            var encryptingCredentials_oaep256 = new X509EncryptingCredentials(cert, SecurityAlgorithms.RsaOaepKeyWrap, "RSA-OAEP-256");
            credentials.Add(encryptingCredentials_oaep256.Key.KeyId.Sha1(), encryptingCredentials_oaep256);

            return credentials;
        }
    }
}
