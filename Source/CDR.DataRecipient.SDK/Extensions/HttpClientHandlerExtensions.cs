using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace CDR.DataRecipient.SDK.Extensions
{
    public static class HttpClientHandlerExtensions
    {
        public static void SetServerCertificateValidation(this HttpClientHandler httpClientHandler, bool acceptAnyServerCertificate)
        {
            httpClientHandler.ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback(acceptAnyServerCertificate);
        }

        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback(
            bool acceptAnyServerCertificate)
        {
            return (message, serverCert, chain, errors) =>
            {
                if (acceptAnyServerCertificate)
                {
                    return true;
                }

                return errors == SslPolicyErrors.None;
            };
        }
    }
}
