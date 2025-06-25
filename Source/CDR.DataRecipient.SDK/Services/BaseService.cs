using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.SDK.Services
{
    public abstract class BaseService
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly IServiceConfiguration _serviceConfiguration;

        protected BaseService(
            IConfiguration config,
            ILogger logger,
            IServiceConfiguration serviceConfiguration)
        {
            this._config = config;
            this._logger = logger;
            this._serviceConfiguration = serviceConfiguration;
        }

        // Expose protected read-only properties for derived classes
        protected IConfiguration Config => this._config;

        protected ILogger Logger => this._logger;

        protected IServiceConfiguration ServiceConfiguration => this._serviceConfiguration;

        protected virtual HttpClient GetHttpClient(
            X509Certificate2 clientCertificate = null,
            string accessToken = null,
            string version = null)
        {
            var clientHandler = new HttpClientHandler();

            // If accepting any TLS server certificate, then ignore certificate validation.
            if (this._serviceConfiguration.AcceptAnyServerCertificate)
            {
                clientHandler.SetServerCertificateValidation(this._serviceConfiguration.AcceptAnyServerCertificate);
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

        protected virtual string EnsureValidEndpoint(string uri)
        {
            return uri.ValidateEndpoint(this._serviceConfiguration.EnforceHttpsEndpoints);
        }

        protected virtual Uri EnsureValidEndpoint(Uri uri)
        {
            return uri.ValidateEndpoint(this._serviceConfiguration.EnforceHttpsEndpoints);
        }

        protected virtual HttpRequestMessage EnsureValidEndpoint(HttpRequestMessage request)
        {
            request.RequestUri.ValidateEndpoint(this._serviceConfiguration.EnforceHttpsEndpoints);
            return request;
        }
    }
}
