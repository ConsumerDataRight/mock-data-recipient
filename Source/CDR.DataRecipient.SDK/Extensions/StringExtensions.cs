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

        public static string Sha1(this string value)
        {
            // ComputeHash - returns byte array  
            byte[] bytes = SHA1.HashData(Encoding.UTF8.GetBytes(value));

            // Convert byte array to a string   
            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }

        public static (string errorCode, string errorTitle, string errorDescription) ParseErrorString(this string value, string defaultErrorTitle = "error", string defaultErrorCode = "error", string defaultErrorDescription = "An error has occured")
        {
            int charLocation = value.IndexOf(':', StringComparison.Ordinal);
            var errorCode = charLocation > 0 ? value.Substring(0, charLocation) : defaultErrorCode;
            var errorTitle = defaultErrorTitle;
            var errorDescription= charLocation > 0 ? value.Substring(charLocation + 1) : defaultErrorDescription;

            return (errorCode, errorTitle, errorDescription);
        }
    }
}
