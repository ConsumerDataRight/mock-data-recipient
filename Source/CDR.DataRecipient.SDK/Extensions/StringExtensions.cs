using System;
using System.Security.Cryptography;
using System.Text;

namespace CDR.DataRecipient.SDK.Extensions
{
    public static class StringExtensions
    {
        public static string Sha256(this string value)
        {
            // ComputeHash - returns byte array
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));

            // Convert byte array to a string
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        // SHA-1 is used here solely to derive a non-secret lookup identifier (JWK "kid") that matches the
        // SHA-1-based X.509 certificate thumbprint convention ("kid") used by Data Holders/Register in the
        // CDR ecosystem for the "RSA-OAEP-256" JWE encryption key (see CDR.DataRecipient.Web.Common.Extensions.
        // GetEncryptionCredentials). It is not used for any cryptographic integrity/confidentiality guarantee,
        // so the collision weaknesses of SHA-1 are not a security concern in this context. Do not change the
        // algorithm without confirming compatibility with Data Holder "kid" matching.
#pragma warning disable CA5350, S4790 // Do Not Use Weak Cryptographic Algorithms / Use a stronger hashing algorithm
        public static string Sha1(this string value)
        {
            // ComputeHash - returns byte array
            byte[] bytes = SHA1.HashData(Encoding.UTF8.GetBytes(value));
#pragma warning restore CA5350, S4790

            // Convert byte array to a string
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        public static (string ErrorCode, string ErrorTitle, string ErrorDescription) ParseErrorString(this string value, string defaultErrorTitle = "error", string defaultErrorCode = "error", string defaultErrorDescription = "An error has occured")
        {
            int charLocation = value.IndexOf(':', StringComparison.Ordinal);
            var errorCode = charLocation > 0 ? value.Substring(0, charLocation) : defaultErrorCode;
            var errorTitle = defaultErrorTitle;
            var errorDescription = charLocation > 0 ? value.Substring(charLocation + 1) : defaultErrorDescription;

            return (errorCode, errorTitle, errorDescription);
        }
    }
}
