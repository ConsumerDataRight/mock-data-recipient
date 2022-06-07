using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.SDK.Services
{
    public abstract class BaseService
    {
        protected readonly IConfiguration _config;
        protected readonly ILogger _logger;

        public BaseService(
            IConfiguration config,
            ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        protected HttpClient GetHttpClient(
            X509Certificate2 clientCertificate = null,
            string accessToken = null,
            string version = null)
        {
            var acceptAnyServerCertificate = _config.GetValue<bool>("AcceptAnyServerCertificate");
            return GetHttpClient(acceptAnyServerCertificate, clientCertificate, accessToken, version);
        }

        protected HttpClient GetHttpClient(
            bool acceptAnyServerCertificate,
            X509Certificate2 clientCertificate = null,
            string accessToken = null,
            string version = null)
        {
            var clientHandler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip
            };

            // If accepting any TLS server certificate, then ignore certificate validation.
            if (acceptAnyServerCertificate)
            {
                clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            // Set the client certificate for the connection if supplied.
            if (clientCertificate != null)
            {
                clientHandler.ClientCertificates.Add(clientCertificate);
            }

            var client = new HttpClient(clientHandler);

            // If an access token has been provided then add to the Authorization header of the client.
            if (!string.IsNullOrEmpty(accessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            // Add the x-v header to the request if provided.
            if (!string.IsNullOrEmpty(version))
            {
                client.DefaultRequestHeaders.Add("x-v", version);
            }

            return client;
        }
    }
}
