using CDR.DataRecipient.SDK.Enumerations;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CDR.DataRecipient.SDK.Services.Register
{
    public class MetadataService : BaseService, IMetadataService
    {
        public MetadataService(
            IConfiguration config, 
            ILogger<MetadataService> logger,
            IServiceConfiguration serviceConfiguration) : base(config, logger, serviceConfiguration)
        {

        }

        public async Task<(string, System.Net.HttpStatusCode, string)> GetDataHolderBrands(
            string registerMtlsBaseUri,
            string version,
            string accessToken,
            X509Certificate2 clientCertificate,
            string softwareProductId,
            Industry industry,
            int? page = null,
            int? pageSize = null)
        {
            _logger.LogDebug($"Request received to {nameof(MetadataService)}.{nameof(GetDataHolderBrands)}.");

            // Setup the request to the get data holder brands endpoint.
            var endpoint = $"{registerMtlsBaseUri.TrimEnd('/')}/cdr-register/v1/{industry.ToPath()}/data-holders/brands";

            // Setup the http client.
            var client = GetHttpClient(clientCertificate, accessToken, version);

            _logger.LogDebug("Requesting data holder brands from Register: {endpoint}.  Client Certificate: {thumbprint}", endpoint, clientCertificate.Thumbprint);

            // Add the query parameters.
            if (page.HasValue)
            {
                endpoint = endpoint.AppendQueryString("page", page.ToString());
            }

            if (pageSize.HasValue)
            {
                endpoint = endpoint.AppendQueryString("page-size", pageSize.ToString());
            }

            // Make the request to the get data holder brands endpoint.
            var response = await client.GetAsync(EnsureValidEndpoint(endpoint));
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Get Data Holder Brands Response: {statusCode}.  Body: {body}", response.StatusCode, body);

            return (body, response.StatusCode, response.ReasonPhrase.ToString());
        }

        public async Task<(string, System.Net.HttpStatusCode, string)> GetDataRecipients(
            string registerTlsBaseUri,
            string version,
            Industry industry)
        {
            _logger.LogDebug($"Request received to {nameof(MetadataService)}.{nameof(GetDataRecipients)}.");

            // Setup the request to the get data recipients endpoint.
            var endpoint = $"{registerTlsBaseUri.TrimEnd('/')}/cdr-register/v1/{industry.ToPath()}/data-recipients";

            // Setup the http client.
            var client = GetHttpClient(version: version);

            _logger.LogDebug("Requesting data recipients from Register: {endpoint}.", endpoint);

            // Make the request to the get data recipients endpoint.
            var response = await client.GetAsync(EnsureValidEndpoint(endpoint));
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Get Data Recipients Response: {statusCode}.  Body: {body}", response.StatusCode, body);

            return (body, response.StatusCode, response.ReasonPhrase.ToString());
        }
    }
}
